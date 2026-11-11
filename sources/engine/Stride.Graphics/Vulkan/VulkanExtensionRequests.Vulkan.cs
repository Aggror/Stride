// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Stride.Graphics;

/// <summary>
///   Vulkan extensions that another API sharing Stride's device needs enabled, such as an OpenXR runtime
///   (<c>xrGetVulkanInstanceExtensionsKHR</c> and <c>xrGetVulkanDeviceExtensionsKHR</c>).
/// </summary>
/// <remarks>
///   Requests apply only to an instance or device created after they are made: add them before the first
///   <see cref="GraphicsDevice"/> is created. Nothing is requested by default, so a game's device stays as it was.
///   A requested extension the driver doesn't offer makes instance or device creation fail, because the API that
///   asked for it can't work without it.
/// </remarks>
internal static class VulkanExtensionRequests
{
    // The strings live for the whole process: Vulkan reads them at every instance or device creation
    private static readonly List<VkUtf8String> instanceExtensions = [];
    private static readonly List<VkUtf8String> deviceExtensions = [];

    internal static IReadOnlyList<VkUtf8String> InstanceExtensions => instanceExtensions;

    internal static IReadOnlyList<VkUtf8String> DeviceExtensions => deviceExtensions;

    /// <summary>Requests an instance extension, by its Vulkan name.</summary>
    public static void AddInstanceExtension(string name) => Add(instanceExtensions, name);

    /// <summary>Requests a device extension, by its Vulkan name.</summary>
    public static void AddDeviceExtension(string name) => Add(deviceExtensions, name);

    private static unsafe void Add(List<VkUtf8String> list, string name)
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
