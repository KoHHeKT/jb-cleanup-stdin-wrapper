using System;

namespace Cleanup.Wrapper;

internal static class WellKnownPaths
{
    public static string CleanupUtilName =>
        Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => "jb" //
            ,
            PlatformID.Win32NT => "cleanupcode.exe" // from choco resharper-clt package
            ,
            _ => throw new PlatformNotSupportedException($"Unknown platform {Environment.OSVersion.Platform}")
        };
}