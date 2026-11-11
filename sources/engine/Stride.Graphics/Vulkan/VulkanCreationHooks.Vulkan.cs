// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Stride.Graphics;

/// <summary>
///   Lets another API that shares Stride's Vulkan instance and device take part in creating them, such as an
///   OpenXR runtime: it can request extra extensions (<c>XR_KHR_vulkan_enable</c>), create the instance and device
///   itself (<c>XR_KHR_vulkan_enable2</c>), and point at the GPU it needs.
/// </summary>
/// <remarks>
///   Hooks apply only to an instance or device created after they are set: set them before the first
///   <see cref="GraphicsDevice"/> is created, and before anything enumerates <see cref="GraphicsAdapterFactory.Adapters"/>.
///   Nothing is set by default, so a game's instance and device stay as they were.
/// </remarks>
internal static unsafe class VulkanCreationHooks
{
    /// <summary>Creates a Vulkan instance from the create info Stride built, adding what the caller needs.</summary>
    internal delegate VkResult CreateInstanceCallback(VkInstanceCreateInfo* createInfo, out VkInstance instance);

    /// <summary>Creates a Vulkan device from the create info Stride built, adding what the caller needs.</summary>
    internal delegate VkResult CreateDeviceCallback(VkInstance instance, VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* createInfo, out VkDevice device);

    /// <summary>Gets the physical device the caller needs, or <see cref="VkPhysicalDevice.Null"/> when it has no preference.</summary>
    internal delegate VkPhysicalDevice PreferredPhysicalDeviceCallback(VkInstance instance);

    // Unmanaged UTF-8 copies, as the create infos take them
    private static readonly List<VkUtf8String> instanceExtensions = [];
    private static readonly List<VkUtf8String> deviceExtensions = [];

    internal static IReadOnlyList<VkUtf8String> InstanceExtensions => instanceExtensions;

    internal static IReadOnlyList<VkUtf8String> DeviceExtensions => deviceExtensions;

    /// <summary>
    ///   Replaces <c>vkCreateInstance</c>. A requested extension is still checked and added to the create info first.
    /// </summary>
    internal static CreateInstanceCallback CreateInstance { get; set; }

    /// <summary>
    ///   Replaces <c>vkCreateDevice</c>. A requested extension is still checked and added to the create info first.
    /// </summary>
    internal static CreateDeviceCallback CreateDevice { get; set; }

    /// <summary>
    ///   Picks the default adapter: the physical device it returns goes first in <see cref="GraphicsAdapterFactory.Adapters"/>.
    /// </summary>
    internal static PreferredPhysicalDeviceCallback PreferredPhysicalDevice { get; set; }

    /// <summary>
    ///   Requests an instance extension, by its Vulkan name. One the driver doesn't offer makes instance creation fail,
    ///   because the API that asked for it can't work without it.
    /// </summary>
    internal static void AddInstanceExtension(string name) => Add(instanceExtensions, name);

    /// <summary>
    ///   Requests a device extension, by its Vulkan name. One the GPU doesn't offer makes device creation fail,
    ///   because the API that asked for it can't work without it.
    /// </summary>
    internal static void AddDeviceExtension(string name) => Add(deviceExtensions, name);

    /// <summary>Removes every hook and request, for an instance or device created afterwards.</summary>
    internal static void Reset()
    {
        CreateInstance = null;
        CreateDevice = null;
        PreferredPhysicalDevice = null;
        Clear(instanceExtensions);
        Clear(deviceExtensions);
    }

    private static void Clear(List<VkUtf8String> list)
    {
        // Vulkan doesn't keep the names after creation, so they can go
        foreach (var name in list)
            Marshal.FreeCoTaskMem((nint)name.Buffer);
        list.Clear();
    }

    private static void Add(List<VkUtf8String> list, string name)
    {
        foreach (var existing in list)
        {
            if (existing.ToString() == name)
                return;
        }

        var utf8 = (byte*)Marshal.StringToCoTaskMemUTF8(name);
        list.Add(new VkUtf8String(utf8));
    }
}

#endif
