using System.Runtime.InteropServices;
using PhantomPort.Interop;

namespace PhantomPort;

/// <summary>
/// A stream that wraps a POSIX file descriptor, providing read/write access to a PTY master.
/// </summary>
internal sealed class PosixDeviceStream : Stream
{
    private readonly int _fd;
    private volatile bool _disposed;

    internal PosixDeviceStream(int fd) => _fd = fd;

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var span = buffer.AsSpan(offset, count);
        var result = Posix.read(_fd, span, span.Length);

        if (result < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            throw new IOException($"Read from PTY master failed (errno: {errno})");
        }

        return (int)result;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var span = new ReadOnlySpan<byte>(buffer, offset, count);
        var totalWritten = 0;

        while (totalWritten < count)
        {
            var slice = span.Slice(totalWritten);
            var result = Posix.write(_fd, slice, slice.Length);

            if (result < 0)
            {
                var errno = Marshal.GetLastPInvokeError();
                throw new IOException($"Write to PTY master failed (errno: {errno})");
            }

            totalWritten += (int)result;
        }
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    internal void MarkDisposed() => _disposed = true;
}
