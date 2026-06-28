using SquidStd.Network.Client;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload containing an exception raised by server or client network loops.
/// </summary>
public sealed class SquidStdTcpExceptionEventArgs : EventArgs
{
    /// <summary>
    ///     Exception raised by the networking component.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     Client related to the exception, when available.
    /// </summary>
    public SquidStdTcpClient? Client { get; }

    public SquidStdTcpExceptionEventArgs(Exception exception, SquidStdTcpClient? client = null)
    {
        Exception = exception;
        Client = client;
    }
}
