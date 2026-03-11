using System.IO.Pipes;

namespace PhantomPort;

/// <summary>
/// Stream wrapper around a <see cref="NamedPipeServerStream"/> that handles client
/// connection lifecycle transparently. Blocks on the first read/write until a client connects,
/// and automatically accepts new clients after disconnection.
/// </summary>
internal sealed class WindowsPipeStream : Stream
{
    private readonly NamedPipeServerStream _pipe;
    private bool _everConnected;

    internal WindowsPipeStream(NamedPipeServerStream pipe) => _pipe = pipe;

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
        EnsureConnected();
        return _pipe.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureConnected();
        _pipe.Write(buffer, offset, count);
    }

    public override void Flush() => _pipe.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    private void EnsureConnected()
    {
        if (_pipe.IsConnected) return;

        if (_everConnected)
        {
            try { _pipe.Disconnect(); }
            catch (InvalidOperationException) { }
        }

        _pipe.WaitForConnection();
        _everConnected = true;
    }
}
