using System.Runtime.InteropServices;
using System.Security.Principal;

namespace PhantomPort.Core.Tests;

internal static partial class TestEnvironment
{
    /// <summary>
    /// Returns <c>true</c> when the test process is running with elevated permissions
    /// (root on POSIX, Administrator on Windows) — meaning virtual ports should be
    /// discoverable via <c>SerialPort.GetPortNames()</c>.
    /// </summary>
    internal static bool IsElevated
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return IsWindowsAdmin();

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                return geteuid() == 0;

            return false;
        }
    }

    [LibraryImport("libc")]
    private static partial uint geteuid();

    private static bool IsWindowsAdmin()
    {
        if (!OperatingSystem.IsWindows()) return false;

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
