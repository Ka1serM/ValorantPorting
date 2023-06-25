using ValorantPorting.ViewModels;

namespace ValorantPorting.Services;

public static class ApplicationService
{
    public static ApplicationViewModel AppVM = new();
    public static ApiEndpointViewModel ApiEndpointView { get; } = new();
}