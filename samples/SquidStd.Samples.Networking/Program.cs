using System.Net;
using System.Text;
using SquidStd.Network.Client;
using SquidStd.Network.Server;

#region step-1
var endPoint = new IPEndPoint(IPAddress.Loopback, 9099);
var server = new SquidTcpServer(endPoint);

server.OnClientConnect += (_, args) =>
    Console.WriteLine($"Client connected: session {args.Client.SessionId}");

server.OnDataReceived += (_, args) =>
    Console.WriteLine($"Received {args.Data.Length} byte(s): {Encoding.UTF8.GetString(args.Data.Span)}");
#endregion

if (!args.Contains("--run"))
{
    Console.WriteLine("Pass --run to start the TCP server and round-trip a message.");

    return;
}

#region step-2
await server.StartAsync(CancellationToken.None);

var client = await SquidStdTcpClient.ConnectAsync(endPoint);
await client.SendAsync(Encoding.UTF8.GetBytes("hello squid"), CancellationToken.None);

await Task.Delay(200);

await client.DisposeAsync();
await server.DisposeAsync();
#endregion
