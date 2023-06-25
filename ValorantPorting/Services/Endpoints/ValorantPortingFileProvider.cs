using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Versions;

namespace ValorantPorting.Services.Endpoints;

public class ValorantPortingFileProvider : CustomFileProvider
{
    public ValorantPortingFileProvider(bool isCaseInsensitive = false, VersionContainer? versions = null) : base(isCaseInsensitive, versions)
    {
        
    }

    public ValorantPortingFileProvider(DirectoryInfo mainDirectory, List<DirectoryInfo> extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(mainDirectory, extraDirectories, searchOption, isCaseInsensitive, versions)
    {
    }
    
    public ValorantPortingFileProvider(DirectoryInfo mainDirectory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(mainDirectory, searchOption, isCaseInsensitive, versions)
    {
    }
}