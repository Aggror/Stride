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
        // The runtime made by PrepareGraphicsDevice, before the device existed, so its Vulkan extensions could be requested
        private static (XR Xr, Instance Instance, ulong SystemId, bool Passthrough)? preparedRuntime;

        private SwapchainImageVulkanKHR[]? vulkanImages;
        private Texture[]? swapchainTextures;

        private static bool PrepareVulkan(bool requestPassthrough)
        {
            if (preparedRuntime is not null)
                return true;

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

            foreach (var name in GetVulkanExtensions(xr, instance, systemId, "xrGetVulkanInstanceExtensionsKHR"))
                VulkanExtensionRequests.AddInstanceExtension(name);

            foreach (var name in GetVulkanExtensions(xr, instance, systemId, "xrGetVulkanDeviceExtensionsKHR"))
                VulkanExtensionRequests.AddDeviceExtension(name);

            preparedRuntime = (xr, instance, systemId, requestPassthrough);
            return true;
        }

        private static OpenXRHmd? TakePreparedRuntime(bool requestPassthrough)
        {
            if (preparedRuntime is not { } runtime)
                return null;

            if (requestPassthrough && !runtime.Passthrough)
                Logger.Warning("Passthrough was not requested in PrepareGraphicsDevice, so it is not available");

            preparedRuntime = null;
            return new OpenXRHmd(runtime.Xr, runtime.Instance, runtime.SystemId, runtime.Passthrough);
        }

        private static PfnVoidFunction GetFunction(XR xr, Instance instance, string name)
        {
            var function = new PfnVoidFunction();
            xr.GetInstanceProcAddr(instance, name, ref function).CheckResult(name);
            return function;
        }

        /// <summary>
        ///   Calls xrGetVulkanInstanceExtensionsKHR or xrGetVulkanDeviceExtensionsKHR, which share a signature
        ///   and return the names in one space-separated string.
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

        private GraphicsBindingVulkanKHR CreateVulkanGraphicsBinding(GraphicsDevice device)
        {
            // The runtime requires this call before the session is created
            var getRequirements = (delegate* unmanaged<Instance, ulong, GraphicsRequirementsVulkanKHR*, Result>)GetFunction(Xr, Instance, "xrGetVulkanGraphicsRequirementsKHR").Handle;
            var requirements = new GraphicsRequirementsVulkanKHR { Type = StructureType.GraphicsRequirementsVulkanKhr };
            getRequirements(Instance, SystemId, &requirements).CheckResult("xrGetVulkanGraphicsRequirementsKHR");
            Logger.Debug($"Vulkan API versions supported by the runtime: {requirements.MinApiVersionSupported} to {requirements.MaxApiVersionSupported}");

            // The session must use the physical device the runtime renders with
            var getDevice = (delegate* unmanaged<Instance, ulong, nint, nint*, Result>)GetFunction(Xr, Instance, "xrGetVulkanGraphicsDeviceKHR").Handle;
            nint runtimePhysicalDevice;
            getDevice(Instance, SystemId, device.NativeInstance.Handle, &runtimePhysicalDevice).CheckResult("xrGetVulkanGraphicsDeviceKHR");
            if (runtimePhysicalDevice != device.NativePhysicalDevice.Handle)
                Logger.Warning($"The OpenXR runtime renders on another GPU than {device.Adapter.Description}; the headset may stay black");

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
