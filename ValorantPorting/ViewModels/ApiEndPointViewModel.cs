using System;
using System.IO;
using System.Threading.Tasks;
using RestSharp;
using ValorantPorting.Services.Endpoints;

namespace ValorantPorting.ViewModels;

public class ApiEndpointViewModel
{
    private readonly RestClient _client = new(new RestClientOptions
    {
        UserAgent = $"FModel/{Globals.APP_VERSION}",
        MaxTimeout = 3 * 1000
    });

    public ApiEndpointViewModel()
    {
        ValorantApi = new ValorantApiEndpoint(_client);
    }

    public ValorantApiEndpoint ValorantApi { get; }

    public async Task DownloadFileAsync(string fileLink, string installationPath)
    {
        var request = new RestRequest(fileLink);
        var data = _client.DownloadData(request) ?? Array.Empty<byte>();
        await File.WriteAllBytesAsync(installationPath, data);
    }

    public void DownloadFile(string fileLink, string installationPath)
    {
        DownloadFileAsync(fileLink, installationPath).GetAwaiter().GetResult();
    }
}