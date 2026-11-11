// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Stride.Graphics;
using Vortice.Vulkan;

namespace Stride.VirtualReality
{
    public unsafe partial class OpenXRHmd
    {
        // The Vulkan version Stride's instance asks for (GraphicsAdapterFactoryInstance)
        private static readonly Version StrideVulkanVersion = new(1, 3);

        // The runtime made by PrepareGraphicsDevice, before the device existed, so it could take part in creating it
        private static (XR Xr, Instance Instance, ulong SystemId, bool Passthrough, bool VulkanEnable2)? preparedRuntime;

        // XR_KHR_vulkan_enable2: the runtime creates the Vulkan instance and device. Otherwise XR_KHR_vulkan_enable:
        // Stride creates them with the extensions the runtime asks for.
        private bool vulkanEnable2;

        private SwapchainImageVulkanKHR[]? vulkanImages;
        private Texture[]? swapchainTextures;

        private static bool PrepareVulkan(bool requestPassthrough)
        {
            if (preparedRuntime is not null)
                return true;

            if (GraphicsAdapterFactory.IsInitialized)
            {
                Logger.Error("OpenXRHmd.PrepareGraphicsDevice() must be called before the graphics device is created or the graphics adapters are enumerated; VR is not available");
                return false;
            }

            XR xr;
            try
            {
                xr = XR.GetApi();
            }
            catch (Exception e)
            {
                Logger.Warning($"No OpenXR loader found, VR is not available: {e.Message}");
                return false;
            }

            var requestedExtensions = new List<string>(extensions);
            if (requestPassthrough)
                requestedExtensions.Add(OpenXRUtils.XR_FB_PASSTHROUGH_EXTENSION_NAME);

            var instance = OpenXRUtils.CreateRuntime(xr, requestedExtensions, Logger);
            if (instance.Handle == 0)
            {
                xr.Dispose();
                return false;
            }

            var systemId = OpenXRUtils.GetSystem(xr, instance, Logger);
            if (systemId == 0)
            {
                Logger.Warning("The OpenXR runtime has no headset available");
                xr.DestroyInstance(instance);
                xr.Dispose();
                return false;
            }

            // CreateRuntime enabled XR_KHR_vulkan_enable2 when the runtime has it
            var vulkanEnable2 = xr.HasVulkanEnable2();

            if (!CheckVulkanRequirements(xr, instance, systemId, vulkanEnable2))
            {
                xr.DestroyInstance(instance);
                xr.Dispose();
                return false;
            }

            if (vulkanEnable2)
            {
                Logger.Info($"Using {OpenXRUtils.XR_KHR_VULKAN_ENABLE2_EXTENSION_NAME}");
                VulkanCreationHooks.CreateInstance = (VkInstanceCreateInfo* createInfo, out VkInstance vulkanInstance)
                    => CreateVulkanInstance(xr, instance, systemId, createInfo, out vulkanInstance);
                VulkanCreationHooks.CreateDevice = (VkInstance vulkanInstance, VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* createInfo, out VkDevice device)
                    => CreateVulkanDevice(xr, instance, systemId, vulkanInstance, physicalDevice, createInfo, out device);
            }
            else
            {
                Logger.Info($"Using {OpenXRUtils.XR_KHR_VULKAN_ENABLE_EXTENSION_NAME}");
                foreach (var name in GetVulkanExtensions(xr, instance, systemId, "xrGetVulkanInstanceExtensionsKHR"))
                    VulkanCreationHooks.AddInstanceExtension(name);

                foreach (var name in GetVulkanExtensions(xr, instance, systemId, "xrGetVulkanDeviceExtensionsKHR"))
                    VulkanCreationHooks.AddDeviceExtension(name);
            }

            // The runtime's GPU becomes the default adapter, so a laptop with two GPUs renders on the one driving the headset
            VulkanCreationHooks.PreferredPhysicalDevice = vulkanInstance =>
            {
                try
                {
                    return GetVulkanPhysicalDevice(xr, instance, systemId, vulkanEnable2, vulkanInstance);
                }
                catch (OpenXRException e)
                {
                    // Not worth failing the game over: the default adapter order stays, and the session warns about a GPU mismatch
                    Logger.Warning($"Could not get the OpenXR runtime's GPU: {e.Message}");
                    return VkPhysicalDevice.Null;
                }
            };

            preparedRuntime = (xr, instance, systemId, requestPassthrough, vulkanEnable2);
            return true;
        }

        private static OpenXRHmd? TakePreparedRuntime(bool requestPassthrough)
        {
            if (preparedRuntime is not { } runtime)
                return null;

            if (requestPassthrough && !runtime.Passthrough)
                Logger.Warning("Passthrough was not requested in PrepareGraphicsDevice, so it is not available");

            preparedRuntime = null;
            return new OpenXRHmd(runtime.Xr, runtime.Instance, runtime.SystemId, runtime.Passthrough) { vulkanEnable2 = runtime.VulkanEnable2 };
        }

        private static PfnVoidFunction GetFunction(XR xr, Instance instance, string name)
        {
            var function = new PfnVoidFunction();
            xr.GetInstanceProcAddr(instance, name, ref function).CheckResult(name);
            return function;
        }

        /// <summary>
        ///   Gets the Vulkan versions the runtime supports, which the runtime requires before the Vulkan instance or the
        ///   session is created, and checks Stride's version is one of them.
        /// </summary>
        private static bool CheckVulkanRequirements(XR xr, Instance instance, ulong systemId, bool vulkanEnable2)
        {
            // xrGetVulkanGraphicsRequirements2KHR has the signature and structure layout of the version 1 function
            var functionName = vulkanEnable2 ? "xrGetVulkanGraphicsRequirements2KHR" : "xrGetVulkanGraphicsRequirementsKHR";
            var getRequirements = (delegate* unmanaged<Instance, ulong, GraphicsRequirementsVulkanKHR*, Result>)GetFunction(xr, instance, functionName).Handle;
            var requirements = new GraphicsRequirementsVulkanKHR
            {
                Type = vulkanEnable2 ? StructureType.GraphicsRequirementsVulkan2Khr : StructureType.GraphicsRequirementsVulkanKhr,
            };
            getRequirements(instance, systemId, &requirements).CheckResult(functionName);

            var min = (Version)(Version64)requirements.MinApiVersionSupported;
            var max = (Version)(Version64)requirements.MaxApiVersionSupported;
            Logger.Debug($"Vulkan versions supported by the OpenXR runtime: {min.Major}.{min.Minor} to {max.Major}.{max.Minor}");

            // The runtime may support newer minor versions than it was tested with, so only the major version caps
            if (StrideVulkanVersion.Major < min.Major || (StrideVulkanVersion.Major == min.Major && StrideVulkanVersion.Minor < min.Minor) || StrideVulkanVersion.Major > max.Major)
            {
                Logger.Error($"The OpenXR runtime supports Vulkan {min.Major}.{min.Minor} to {max.Major}.{max.Minor}, but Stride uses Vulkan {StrideVulkanVersion.Major}.{StrideVulkanVersion.Minor}; VR is not available");
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Calls xrGetVulkanInstanceExtensionsKHR or xrGetVulkanDeviceExtensionsKHR (XR_KHR_vulkan_enable), which share
        ///   a signature and return the names in one space-separated string.
        /// </summary>
        private static string[] GetVulkanExtensions(XR xr, Instance instance, ulong systemId, string functionName)
        {
            var function = (delegate* unmanaged<Instance, ulong, uint, uint*, byte*, Result>)GetFunction(xr, instance, functionName).Handle;

            uint length = 0;
            function(instance, systemId, 0, &length, null).CheckResult(functionName);
            if (length == 0)
                return [];

            var buffer = new byte[length];
            fixed (byte* bufferPointer = buffer)
                function(instance, systemId, length, &length, bufferPointer).CheckResult(functionName);

            // The length counts the terminating null
            var names = Encoding.UTF8.GetString(buffer, 0, (int)length - 1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Logger.Debug($"{functionName}: {string.Join(", ", names)}");
            return names;
        }

        /// <summary>
        ///   Creates the Vulkan instance through xrCreateVulkanInstanceKHR (XR_KHR_vulkan_enable2), which adds the
        ///   extensions the runtime needs to Stride's create info.
        /// </summary>
        private static VkResult CreateVulkanInstance(XR xr, Instance instance, ulong systemId, VkInstanceCreateInfo* createInfo, out VkInstance vulkanInstance)
        {
            var createVulkanInstance = (delegate* unmanaged<Instance, VulkanInstanceCreateInfoKHR*, nint*, VkResult*, Result>)GetFunction(xr, instance, "xrCreateVulkanInstanceKHR").Handle;
            var xrCreateInfo = new VulkanInstanceCreateInfoKHR
            {
                Type = StructureType.VulkanInstanceCreateInfoKhr,
                SystemId = systemId,
                PfnGetInstanceProcAddr = new PfnVoidFunction((delegate* unmanaged[Cdecl]<void>)Vulkan.vkGetInstanceProcAddr_ptr),
                VulkanCreateInfo = createInfo,
            };

            nint handle = 0;
            var vulkanResult = VkResult.Success;
            var result = createVulkanInstance(instance, &xrCreateInfo, &handle, &vulkanResult);
            vulkanInstance = new VkInstance(handle);
            if (!result.Success())
            {
                Logger.Error($"xrCreateVulkanInstanceKHR failed: {result}");
                return vulkanResult != VkResult.Success ? vulkanResult : VkResult.ErrorInitializationFailed;
            }
            return vulkanResult;
        }

        /// <summary>
        ///   Creates the Vulkan device through xrCreateVulkanDeviceKHR (XR_KHR_vulkan_enable2), which adds the extensions
        ///   the runtime needs to Stride's create info.
        /// </summary>
        private static VkResult CreateVulkanDevice(XR xr, Instance instance, ulong systemId, VkInstance vulkanInstance, VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* createInfo, out VkDevice device)
        {
            // The runtime only creates a device on its own GPU; on another one Stride renders without the headset
            var runtimePhysicalDevice = GetVulkanPhysicalDevice(xr, instance, systemId, vulkanEnable2: true, vulkanInstance);
            if (runtimePhysicalDevice != physicalDevice)
            {
                Logger.Warning("The graphics device uses another GPU than the OpenXR runtime; VR is not available on it");
                return Vulkan.GetApi(vulkanInstance).vkCreateDevice(physicalDevice, createInfo, null, out device);
            }

            var createVulkanDevice = (delegate* unmanaged<Instance, VulkanDeviceCreateInfoKHR*, nint*, VkResult*, Result>)GetFunction(xr, instance, "xrCreateVulkanDeviceKHR").Handle;
            var xrCreateInfo = new VulkanDeviceCreateInfoKHR
            {
                Type = StructureType.VulkanDeviceCreateInfoKhr,
                SystemId = systemId,
                PfnGetInstanceProcAddr = new PfnVoidFunction((delegate* unmanaged[Cdecl]<void>)Vulkan.vkGetInstanceProcAddr_ptr),
                VulkanPhysicalDevice = new VkHandle(physicalDevice.Handle),
                VulkanCreateInfo = createInfo,
            };

            nint handle = 0;
            var vulkanResult = VkResult.Success;
            var result = createVulkanDevice(instance, &xrCreateInfo, &handle, &vulkanResult);
            device = new VkDevice(handle);
            if (!result.Success())
            {
                Logger.Error($"xrCreateVulkanDeviceKHR failed: {result}");
                return vulkanResult != VkResult.Success ? vulkanResult : VkResult.ErrorInitializationFailed;
            }
            return vulkanResult;
        }

        /// <summary>
        ///   Gets the physical device the runtime renders with, through xrGetVulkanGraphicsDevice2KHR or
        ///   xrGetVulkanGraphicsDeviceKHR.
        /// </summary>
        private static VkPhysicalDevice GetVulkanPhysicalDevice(XR xr, Instance instance, ulong systemId, bool vulkanEnable2, VkInstance vulkanInstance)
        {
            nint physicalDevice;
            if (vulkanEnable2)
            {
                var getDevice = (delegate* unmanaged<Instance, VulkanGraphicsDeviceGetInfoKHR*, nint*, Result>)GetFunction(xr, instance, "xrGetVulkanGraphicsDevice2KHR").Handle;
                var getInfo = new VulkanGraphicsDeviceGetInfoKHR
                {
                    Type = StructureType.VulkanGraphicsDeviceGetInfoKhr,
                    SystemId = systemId,
                    VulkanInstance = new VkHandle(vulkanInstance.Handle),
                };
                getDevice(instance, &getInfo, &physicalDevice).CheckResult("xrGetVulkanGraphicsDevice2KHR");
            }
            else
            {
                var getDevice = (delegate* unmanaged<Instance, ulong, nint, nint*, Result>)GetFunction(xr, instance, "xrGetVulkanGraphicsDeviceKHR").Handle;
                getDevice(instance, systemId, vulkanInstance.Handle, &physicalDevice).CheckResult("xrGetVulkanGraphicsDeviceKHR");
            }
            return new VkPhysicalDevice(physicalDevice);
        }

        private GraphicsBindingVulkanKHR CreateVulkanGraphicsBinding(GraphicsDevice device)
        {
            // The session must use the physical device the runtime renders with
            if (GetVulkanPhysicalDevice(Xr, Instance, SystemId, vulkanEnable2, device.NativeInstance) != device.NativePhysicalDevice)
                Logger.Warning($"The OpenXR runtime renders on another GPU than {device.Adapter.Description}; the headset may stay black");

            // XrGraphicsBindingVulkan2KHR has the layout and structure type of the version 1 binding
            return new GraphicsBindingVulkanKHR
            {
                Type = StructureType.GraphicsBindingVulkanKhr,
                Next = null,
                Instance = new VkHandle(device.NativeInstance.Handle),
                PhysicalDevice = new VkHandle(device.NativePhysicalDevice.Handle),
                Device = new VkHandle(device.NativeDevice.Handle),
                // Stride submits everything to the first queue of family 0
                QueueFamilyIndex = 0,
                QueueIndex = 0,
            };
        }

        private void ResetVulkanCreationHooks()
        {
            // The hooks call into this instance, which is about to go; a device created later is a plain one
            VulkanCreationHooks.Reset();
        }

        /// <summary>
        ///   Picks the swapchain format matching the frame Stride renders (R8G8B8A8 sRGB), so the frame copies as is.
        /// </summary>
        private long SelectVulkanSwapchainFormat(Session session, out PixelFormat pixelFormat)
        {
            uint count = 0;
            Xr.EnumerateSwapchainFormats(session, 0, &count, null).CheckResult();
            var formats = new long[count];
            fixed (long* formatsPointer = formats)
                Xr.EnumerateSwapchainFormats(session, count, &count, formatsPointer).CheckResult();

            Logger.Debug($"Swapchain formats: {string.Join(", ", Array.ConvertAll(formats, f => ((VkFormat)f).ToString()))}");

            if (Array.IndexOf(formats, (long)VkFormat.R8G8B8A8Srgb) >= 0)
            {
                pixelFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                return (long)VkFormat.R8G8B8A8Srgb;
            }

            if (Array.IndexOf(formats, (long)VkFormat.B8G8R8A8Srgb) >= 0)
            {
                // Same size, so the copy works, but red and blue swap
                Logger.Warning("The OpenXR runtime has no R8G8B8A8 sRGB swapchain; using B8G8R8A8, red and blue are swapped");
                pixelFormat = PixelFormat.B8G8R8A8_UNorm_SRgb;
                return (long)VkFormat.B8G8R8A8Srgb;
            }

            throw new NotSupportedException("The OpenXR runtime offers no 8-bit sRGB swapchain format");
        }

        /// <summary>
        ///   Wraps the runtime's swapchain images as Textures, so CommandList can copy into them and track their layout.
        /// </summary>
        private void CreateVulkanSwapchainTextures(GraphicsDevice device, Swapchain swapchain, PixelFormat pixelFormat)
        {
            uint count = 0;
            Xr.EnumerateSwapchainImages(swapchain, 0, ref count, null).CheckResult();

            vulkanImages = new SwapchainImageVulkanKHR[count];
            for (int i = 0; i < count; i++)
                vulkanImages[i].Type = StructureType.SwapchainImageVulkanKhr;

            fixed (SwapchainImageVulkanKHR* imagesPointer = vulkanImages)
                Xr.EnumerateSwapchainImages(swapchain, count, ref count, (SwapchainImageBaseHeader*)imagesPointer).CheckResult();

            // RenderTarget: the runtime hands the images over in COLOR_ATTACHMENT_OPTIMAL, which that layout tracks
            var description = TextureDescription.New2D(renderSize.Width, renderSize.Height, pixelFormat, TextureFlags.RenderTarget);
            swapchainTextures = new Texture[count];
            for (int i = 0; i < count; i++)
            {
                var texture = new Texture(device).InitializeWithoutResources(description);
                texture.SetNativeHandles(new VkImage(vulkanImages[i].Image), VkImageView.Null);
                swapchainTextures[i] = texture;
            }
        }

        private void CopyToVulkanSwapchain(CommandList commandList, Texture renderFrame)
        {
            var target = swapchainTextures![swapchainPointer];

            commandList.Copy(renderFrame, target);

            // The runtime expects a color swapchain image back in COLOR_ATTACHMENT_OPTIMAL
            commandList.ResourceBarrierTransition(target, BarrierLayout.RenderTarget);

            // The runtime reads the image once it's released, so the copy must be on the queue by then;
            // recording then goes on in a new command buffer
            commandList.Flush();
            commandList.Reset();
        }

        private void DisposeVulkanSwapchainTextures()
        {
            if (swapchainTextures is null)
                return;

            foreach (var texture in swapchainTextures)
                texture.Dispose();

            swapchainTextures = null;
        }
    }
}

#endif
