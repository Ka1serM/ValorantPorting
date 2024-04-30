using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ValorantPorting.Export;

namespace ValorantPorting.Services.Export;

public abstract class SocketServiceBase
{
    protected UdpClient Client = new();

    public SocketServiceBase()
    {
        Client.Connect(Endpoint);
    }

    protected virtual IPEndPoint Endpoint { get; set; }

    public virtual void Send(List<ExportDataBase> data, ExportSettingsBase settings)
    {
    }

    public bool PingServer()
    {
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_Ping));
        return ReceivePing();
    }

    private bool ReceivePing()
    {
        if (TryReceive(Endpoint, out var response))
        {
            var responseString = Encoding.UTF8.GetString(response);
            return responseString.Equals(Globals.UDPClient_Ping);
        }

        return false;
    }

    public static int SendSpliced(UdpClient client, IEnumerable<byte> arr, int size)
    {
        return arr.Chunk(size).ToList().Sum(chunk => client.Send(chunk));
    }

    private bool TryReceive(IPEndPoint endpoint, out byte[] data)
    {
        data = Array.Empty<byte>();
        try
        {
            data = Task.Run(() => Client.Receive(ref endpoint)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (SocketException)
        {
            Client.Close();
            Client = new UdpClient();
            Client.Connect(endpoint);
            return false;
        }

        return true;
    }
}