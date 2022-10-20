using RestSharp;

namespace ValorantPorting.Services.Endpoints;

public abstract class EndpointBase
{
    protected RestClient _client;

    protected EndpointBase(RestClient client)
    {
        _client = client;
    }
}