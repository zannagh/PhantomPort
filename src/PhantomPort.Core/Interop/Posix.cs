using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace PhantomPort.Interop;

internal static partial class Posix
{
    [LibraryImport("libc", SetLastError = true)]
    internal static partial int posix_openpt(int flags);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int grantpt(int fd);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int unlockpt(int fd);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial nint ptsname(int fd);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int open(string pathname, int flags);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial nint read(int fd, Span<byte> buf, nint count);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial nint write(int fd, ReadOnlySpan<byte> buf, nint count);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int tcgetattr(int fd, nint termios);

    [LibraryImport("libc")]
    internal static partial void cfmakeraw(nint termios);

    [LibraryImport("libc", SetLastError = true)]
    internal static partial int tcsetattr(int fd, int optionalActions, nint termios);

    internal const int O_RDWR = 2;
    internal const int TCSANOW = 0;

    // O_NOCTTY prevents the PTY from becoming the controlling terminal.
    // The constant value differs between Linux and macOS.
    internal static int NocttyFlag =>
        OperatingSystem.IsMacOS() ? 0x20000 :
        OperatingSystem.IsLinux() ? 0x100 :
        0;
}
