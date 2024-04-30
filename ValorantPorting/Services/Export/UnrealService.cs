using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using ValorantPorting.Export;
using ValorantPorting.Export.Unreal;
using ValorantPorting.Services.Export;

namespace ValorantPorting.Services;

public class UnrealService : SocketServiceBase
{
    private static readonly UdpClient Client = new();

    static UnrealService()
    {
        Client.Connect("localhost", Globals.UNREAL_PORT);
    }

    public static void Send(ExportData data)
    {
        var export = new UnrealExport
        {
            Data = data,
            AssetsRoot = App.AssetsFolder.FullName.Replace("\\", "/")
        };

        var message = JsonConvert.SerializeObject(export);
        var messageBytes = Encoding.ASCII.GetBytes(message);
        SendSpliced(Client, messageBytes, Globals.BUFFER_SIZE);
        Console.WriteLine(message);
        Client.Send(Encoding.ASCII.GetBytes("MessageFinished"));
    }
}