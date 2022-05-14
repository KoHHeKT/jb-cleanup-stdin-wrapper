using System;
using System.IO;
using System.Linq;

namespace Cleanup.Wrapper;

public static class PathTools
{
    public static string? FindEditorConfig(string startSearchDir)
    {
        return FindFileRecursiveUp(startSearchDir, ".editorconfig");
    }

    public static string? FindFileRecursiveUp(string dir, string fileName)
    {
        dir = Path.GetFullPath(dir);

        while (true)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (string.IsNullOrEmpty(dir) || Path.GetPathRoot(dir) == dir)
                return null;

            var fullDir = Path.GetFullPath(dir);

            if (File.Exists(Path.Combine(fullDir, fileName)))
                return Path.Combine(fullDir, fileName);

            dir = Directory.GetParent(fullDir).FullName;
        }
    }

    public static string GetUtilCmd(string utilPath)
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                return utilPath;
                break;
            case PlatformID.Unix:
                return utilPath;
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static string? GetUtilPath(string jbUtilName)
    {
        var splitBy = Environment.OSVersion.Platform == PlatformID.Unix ? ':' : ';';
        var paths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)
            .Split(splitBy, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(path => File.Exists(Path.Combine(path, jbUtilName)))
            .Select(path => Path.Combine(path, jbUtilName));

        return paths.FirstOrDefault();
    }
}