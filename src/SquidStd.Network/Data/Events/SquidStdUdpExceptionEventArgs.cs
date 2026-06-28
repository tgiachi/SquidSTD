using SquidStd.Network.Client;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload containing an exception raised by the UDP client receive/send paths.
/// </summary>
public sealed class SquidStdUdpExceptionEventArgs : EventArgs
{
    /// <summary>
    ///     Exception raised by the networking component.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     UDP client related to the exception, when available.
    /// </summary>
    public SquidStdUdpClient? Client { get; }

    public SquidStdUdpExceptionEventArgs(Exception exception, SquidStdUdpClient? client = null)
    {
        Exception = exception;
        Client = client;
    }
}
