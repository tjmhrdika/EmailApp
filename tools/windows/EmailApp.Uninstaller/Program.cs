using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

const string ServiceName = "CIP Station Alarm Notification";
const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EmailApp";

try
{
    if (!OperatingSystem.IsWindows())
    {
        Console.Error.WriteLine("This uninstaller only supports Windows.");
        return 1;
    }

    if (!IsAdministrator())
    {
        RelaunchAsAdministrator(args);
        return 0;
    }

    var installDirectory = ResolveInstallDirectory(args);
    Console.WriteLine($"Removing {ServiceName}");

    StopAndDeleteServiceIfExists(ServiceName);
    Registry.LocalMachine.DeleteSubKeyTree(RegistryKeyPath, throwOnMissingSubKey: false);

    if (IsSafeInstallDirectory(installDirectory))
    {
        ScheduleDirectoryRemoval(installDirectory, Environment.ProcessId);
        Console.WriteLine($"Scheduled removal of {installDirectory}");
    }
    else
    {
        Console.WriteLine($"Skipped file removal because the install directory is not recognized: {installDirectory}");
    }

    Console.WriteLine("Uninstall complete.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Uninstall failed.");
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static bool IsAdministrator()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}

static void RelaunchAsAdministrator(string[] args)
{
    var currentExe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
    if (string.IsNullOrWhiteSpace(currentExe))
    {
        throw new InvalidOperationException("Cannot determine uninstaller executable path.");
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = currentExe,
        UseShellExecute = true,
        Verb = "runas",
        WorkingDirectory = AppContext.BaseDirectory
    };

    foreach (var arg in args)
    {
        startInfo.ArgumentList.Add(arg);
    }

    Process.Start(startInfo);
}

static string ResolveInstallDirectory(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i].Equals("--install-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return Path.GetFullPath(args[++i]);
        }
    }

    using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
    var registryInstallLocation = key?.GetValue("InstallLocation") as string;
    if (!string.IsNullOrWhiteSpace(registryInstallLocation))
    {
        return Path.GetFullPath(registryInstallLocation);
    }

    return Path.GetFullPath(AppContext.BaseDirectory);
}

static bool IsSafeInstallDirectory(string installDirectory)
{
    var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installDirectory));
    var programFiles = Path.TrimEndingDirectorySeparator(
        Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
    var programFilesX86 = Path.TrimEndingDirectorySeparator(
        Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)));

    return normalized.StartsWith(programFiles + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        normalized.StartsWith(programFilesX86 + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}

static void ScheduleDirectoryRemoval(string installDirectory, int processId)
{
    var scriptPath = Path.Combine(Path.GetTempPath(), $"EmailApp-uninstall-{Guid.NewGuid():N}.ps1");
    var escapedInstallDirectory = installDirectory.Replace("'", "''");
    var escapedScriptPath = scriptPath.Replace("'", "''");
    var script = $$"""
$processId = {{processId}}
$installDirectory = '{{escapedInstallDirectory}}'
$scriptPath = '{{escapedScriptPath}}'
Wait-Process -Id $processId -Timeout 60 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $scriptPath) {
    Remove-Item -LiteralPath $scriptPath -Force
}
""";

    File.WriteAllText(scriptPath, script);

    Process.Start(new ProcessStartInfo
    {
        FileName = "powershell.exe",
        UseShellExecute = false,
        CreateNoWindow = true,
        ArgumentList =
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            scriptPath
        }
    });
}

static void StopAndDeleteServiceIfExists(string serviceName)
{
    if (!ServiceExists(serviceName))
    {
        return;
    }

    RunProcess("sc.exe", ["stop", serviceName], ignoreExitCode: true);
    WaitForServiceStopped(serviceName, TimeSpan.FromSeconds(30));
    RunProcess("sc.exe", ["delete", serviceName], ignoreExitCode: true);
    Thread.Sleep(TimeSpan.FromSeconds(3));
}

static bool ServiceExists(string serviceName)
{
    var result = RunProcess("sc.exe", ["query", serviceName], ignoreExitCode: true);
    return result.ExitCode == 0;
}

static void WaitForServiceStopped(string serviceName, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow.Add(timeout);
    while (DateTime.UtcNow < deadline)
    {
        var result = RunProcess("sc.exe", ["query", serviceName], ignoreExitCode: true);
        if (result.ExitCode != 0 || result.Output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Thread.Sleep(500);
    }
}

static ProcessResult RunProcess(string fileName, IEnumerable<string> arguments, bool ignoreExitCode = false)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo) ??
        throw new InvalidOperationException($"Failed to start {fileName}.");

    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0 && !ignoreExitCode)
    {
        throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}. {error}{output}");
    }

    return new ProcessResult(process.ExitCode, output, error);
}

readonly record struct ProcessResult(int ExitCode, string Output, string Error);
