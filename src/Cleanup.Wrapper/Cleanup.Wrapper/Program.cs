using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;

namespace Cleanup.Wrapper;

public static class EntryPoint
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = CreateRootCommand();
        var result = await rootCommand.InvokeAsync(args);
        return result;
    }

    private static RootCommand CreateRootCommand()
    {
        var editorConfigSearchDirOption = new Option<string>(new[] { "--editor-config-dir", "-e" }, () => Environment.CurrentDirectory);
        var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, () => false);

        var rootCommand = new RootCommand { verboseOption, editorConfigSearchDirOption };

        rootCommand.SetHandler((bool v, string dir) => ApplyCleanUp(v, dir), verboseOption, editorConfigSearchDirOption);

        return rootCommand;
    }

    private static async Task<int> ApplyCleanUp(bool verbose, string startSearchDir)
    {
        //read stdin

        var input = await Console.In.ReadToEndAsync();
        //save to tmp
        var systemTmp = Path.GetTempPath();
        var tmpDir = Directory.CreateDirectory(Path.Combine(systemTmp, "jb-code-cleanup"));
        var tmpFileName = Guid.NewGuid().ToString()[..8];
        var tmpFilePath = Path.Combine(tmpDir.FullName, tmpFileName + ".cs");
        File.WriteAllText(tmpFilePath, input);

        //check if jb cleanup exists
        var utilPath = PathTools.GetUtilPath(WellKnownPaths.CleanupUtilName);

        if (utilPath == null)
        {
            await Console.Error.WriteLineAsync($"Can't find JetBrains cli tools by name '{WellKnownPaths.CleanupUtilName}'");
            return -1;
        }

        var cleanupCmd = PathTools.GetUtilCmd(utilPath);

        //var execResult = await Cli.Wrap(cleanupCmd).WithArguments($@"--include=""{tmpFilePath}"" --profile=""Built-in: Reformat Code"" --settings=""C:\dev\notes-schedule\.editorconfig\""")
        var argsList = PrepareArgs(startSearchDir, tmpFilePath);

        var execResult = Cli.Wrap(cleanupCmd).WithArguments(argsList, false)
            .WithWorkingDirectory(tmpDir.FullName)
            .ExecuteBufferedAsync(CancellationToken.None)
            .GetAwaiter().GetResult();

        LogCmdOutputsIfVerbose(verbose, execResult);
        //Linux: jb cleanupcode --include="./unformatted.cs" --profile="Built-in: Reformat Code" --settings="../.editorconfig"

        //read from tmp
        var result = await File.ReadAllTextAsync(tmpFilePath);
        var deleteTask = Task.Run(() => File.Delete(tmpFilePath));
        //if noerror write stdout
        Console.WriteLine(result);
        await deleteTask;
        return execResult.ExitCode;
    }

    private static List<string> PrepareArgs(string editorConfigSearchFromDir, string tmpFilePath)
    {
        var argsList = new List<string> { $@"--include=""{tmpFilePath}""", @"--profile=""Built-in: Reformat Code""" };
        if (Environment.OSVersion.Platform == PlatformID.Unix)
            argsList.Insert(0, "cleanupcode");
        var editorConfig = PathTools.FindEditorConfig(editorConfigSearchFromDir);

        if (editorConfig != null)
            argsList.Add($@"--settings=""{editorConfig}""");
        return argsList;
    }

    private static void LogCmdOutputsIfVerbose(bool verbose, BufferedCommandResult execResult)
    {
        if (!verbose) return;

        Console.WriteLine("std out");
        Console.WriteLine(execResult.StandardOutput);
        Console.WriteLine("std err");
        Console.WriteLine(execResult.StandardError);
    }
}