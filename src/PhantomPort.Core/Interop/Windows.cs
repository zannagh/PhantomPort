using System.Runtime.InteropServices;

namespace PhantomPort.Interop;

internal static partial class Windows
{
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DefineDosDeviceW(uint dwFlags, string lpDeviceName, string? lpTargetPath);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint QueryDosDeviceW(string lpDeviceName, nint lpTargetPath, uint ucchMax);

    internal const uint DDD_RAW_TARGET_PATH = 0x00000001;
    internal const uint DDD_REMOVE_DEFINITION = 0x00000002;

    internal static bool DosDeviceExists(string deviceName)
    {
        var buffer = Marshal.AllocHGlobal(512);
        try
        {
            return QueryDosDeviceW(deviceName, buffer, 256) > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
