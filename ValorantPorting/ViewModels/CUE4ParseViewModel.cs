using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Versions;
using ValorantPorting.AppUtils;
using ValorantPorting.Services.Endpoints;

namespace ValorantPorting.ViewModels;

public class CUE4ParseViewModel : ObservableObject
{
    public static readonly VersionContainer Version = new(EGame.GAME_UE5_3);

    // UEDB's stable API that always points at the current mappings — no more hardcoded per-patch URL.
    private const string MappingsApiUrl = "https://uedb.dev/svc/api/v1/valorant/mappings";

    private static readonly string MappingsDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mappings");

    /// <summary>
    /// Checks uedb.dev for the current mapping version, downloads it if we don't already
    /// have that version cached locally, and deletes any stale .usmap files.
    /// </summary>
    private static async Task<string> ResolveMappingsPathAsync()
    {
        Directory.CreateDirectory(MappingsDir);

        using var http = new HttpClient();
        var json = await http.GetStringAsync(MappingsApiUrl);

        using var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("version").GetString(); // e.g. "VALORANT_13.05"
        var usmapUrl = doc.RootElement
            .GetProperty("mappings")
            .GetProperty("ZStandard")
            .GetString();

        var fileName = $"{version}.usmap";
        var localPath = Path.Combine(MappingsDir, fileName);

        if (!File.Exists(localPath))
        {
            AppLog.Information($"New mappings version detected ({version}), downloading from uedb.dev...");

            // Clear out old .usmap files so nothing stale lingers around
            foreach (var old in Directory.GetFiles(MappingsDir, "*.usmap"))
                File.Delete(old);

            var bytes = await http.GetByteArrayAsync(usmapUrl);
            await File.WriteAllBytesAsync(localPath, bytes);

            AppLog.Information("Mappings downloaded successfully.");
        }

        return localPath;
    }

    /// <summary>
    /// Offline/failure fallback: reuse whatever .usmap file already exists locally,
    /// same as the old behavior.
    /// </summary>
    private static string? FindAnyLocalMappingsFile()
    {
        if (!Directory.Exists(MappingsDir)) return null;
        var usmapFiles = Directory.GetFiles(MappingsDir, "*.usmap");
        return usmapFiles.Length > 0 ? usmapFiles[0] : null;
    }

    public readonly List<FAssetData> AssetDataBuffers = new();
    public readonly ValorantPortingFileProvider Provider;

    public FAssetRegistryState? AssetRegistry;

    public CUE4ParseViewModel(string directory, EInstallType installType)
    {
        if (installType is EInstallType.Local && !Directory.Exists(directory))
        {
            AppLog.Warning(
                "Installation Not Found, Valorant installation path does not exist or has not been set. Please go to settings to verify you've set the right path and restart. The program will not work properly on Local Installation mode if you do not set it.");
            return;
        }

        Provider = installType switch
        {
            EInstallType.Local => new ValorantPortingFileProvider(new DirectoryInfo(directory), SearchOption.AllDirectories, true, Version),
            EInstallType.Live => new ValorantPortingFileProvider(true, Version)
        };
    }

    private ApiEndpointViewModel _apiEndpointView => ApiEndpointView;

    public async Task Initialize()
    {
        if (Provider is null) return;

        string? mappingsPath;
        try
        {
            mappingsPath = await ResolveMappingsPathAsync();
        }
        catch (Exception ex)
        {
            AppLog.Warning($"Automatic mappings check/download failed: {ex.Message}. Falling back to any local mappings file.");
            mappingsPath = FindAnyLocalMappingsFile();
        }

        if (mappingsPath is null || !File.Exists(mappingsPath))
        {
            AppLog.Warning(
                "No mappings file could be found or downloaded. UE5 Valorant assets will fail to parse without it.");
        }
        else
        {
            var mappingsProvider = new CustomUsmapTypeMappingsProvider();
            mappingsProvider.Load(mappingsPath);
            Provider.MappingsContainer = mappingsProvider;
        }

        try
        {
            await CUE4Parse.Compression.OodleHelper.InitializeAsync();
        }
        catch (Exception ex)
        {
            AppLog.Warning($"Oodle initialization failed: {ex.Message}. Compressed assets will fail to load.");
        }

        // Use the library's built-in pure C# texture decoder for BC7/BC6H/ETC-compressed
        // textures instead of the native Detex library. Nothing to load, nothing to
        // initialize, nothing that can fail with "not initialized".
        CUE4Parse_Conversion.Textures.TextureDecoder.UseAssetRipperTextureDecoder = true;

        await InitializeProvider();
        await InitializeKeys();

        Provider.LoadVirtualPaths();

        Provider.TryCreateReader("ShooterGame/AssetRegistry.bin", out var assetArchive);
        if (assetArchive is not null)
        {
            AssetRegistry = new FAssetRegistryState(assetArchive);
            AssetDataBuffers.AddRange(AssetRegistry.PreallocatedAssetDataBuffers);
        }
        else
        {
            AppLog.Warning("AssetRegistry.bin could not be loaded, so the asset handler will not have registry data to initialize.");
        }
    }

    private async Task InitializeKeys()
    {
        var keyResponse = AppSettings.Current.AesResponse;
        var keyString = "0x4BE71AF2459CF83899EC9DC2CB60E22AC4B3047E0211034BBABE9D174C069DD6";
        await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(keyString));
    }


    private async Task InitializeProvider()
    {
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Local:
            {
                Provider.InitializeLocal();
                break;
            }
            case EInstallType.Live:
            {
                var manifestInfo = _apiEndpointView.ValorantApi.GetManifest(CancellationToken.None);
                if (manifestInfo == null)
                    throw new Exception(
                        "Could not load latest Valorant manifest, you may have to switch to your local installation.");
                for (var i = 0; i < manifestInfo.Paks.Length; i++)
                    Provider.Initialize(manifestInfo.Paks[i].GetFullName(), new[] { manifestInfo.GetPakStream(i) });
                break;
            }
        }
    }
    public class CustomUsmapTypeMappingsProvider : CUE4Parse.MappingsProvider.AbstractTypeMappingsProvider
{
    private string? _path;
    public override CUE4Parse.MappingsProvider.TypeMappings? MappingsForGame { get; protected set; }

    public override void Load(string path, StringComparer? comparer = null)
    {
        _path = path;
        MappingsForGame = new UsmapParser(path, comparer: comparer).Mappings;
    }

    public override void Load(byte[] bytes, StringComparer? comparer = null)
    {
        MappingsForGame = new UsmapParser(bytes, comparer: comparer).Mappings;
    }

    public override void Reload()
    {
        if (_path != null) Load(_path);
    }
}
}
