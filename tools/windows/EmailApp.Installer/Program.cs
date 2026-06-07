using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

const string ServiceName = "CIP Station Alarm Notification";
const string AppDisplayName = "CIP Station Alarm Notification";
const string Publisher = "Agatos";
const string AppExeName = "EmailApp.exe";
const string UninstallExeName = "uninstall.exe";
const string DefaultUrls = "http://0.0.0.0:5146";

try
{
    if (!OperatingSystem.IsWindows())
    {
        Console.Error.WriteLine("This installer only supports Windows.");
        return 1;
    }

    if (!IsAdministrator())
    {
        RelaunchAsAdministrator(args);
        return 0;
    }

    var options = InstallerOptions.Parse(args);
    var sourceDirectory = options.SourceDirectory ?? AppContext.BaseDirectory;
    var installDirectory = options.InstallDirectory ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EmailApp");
    var urls = options.Urls ?? DefaultUrls;

    sourceDirectory = Path.GetFullPath(sourceDirectory);
    installDirectory = Path.GetFullPath(installDirectory);

    var sourceAppExe = Path.Combine(sourceDirectory, AppExeName);
    if (!File.Exists(sourceAppExe))
    {
        Console.Error.WriteLine($"{AppExeName} was not found in {sourceDirectory}");
        return 1;
    }

    Console.WriteLine($"Installing {AppDisplayName}");
    Console.WriteLine($"Source : {sourceDirectory}");
    Console.WriteLine($"Target : {installDirectory}");

    StopAndDeleteServiceIfExists(ServiceName);
    KillExistingAppProcesses(installDirectory);

    Directory.CreateDirectory(installDirectory);
    CopyDirectory(sourceDirectory, installDirectory);

    var installedAppExe = Path.Combine(installDirectory, AppExeName);
    if (!File.Exists(installedAppExe))
    {
        Console.Error.WriteLine($"{AppExeName} was not copied to {installDirectory}");
        return 1;
    }

    EnsureUninstallerExists(installDirectory);
    CreateService(installedAppExe, urls);
    WriteUninstallRegistry(installDirectory);
    StartService(ServiceName);

    Console.WriteLine();
    Console.WriteLine("Installation complete. The service is installed and running.");
    Console.WriteLine($"Open the application at http://localhost:5146");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Installation failed.");
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
        throw new InvalidOperationException("Cannot determine installer executable path.");
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

static void CopyDirectory(string sourceDirectory, string targetDirectory)
{
    foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(sourceDirectory, directory);
        Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
    }

    foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(sourceDirectory, file);
        var fileName = Path.GetFileName(file);

        if (fileName.Equals("setup.exe", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("EmailApp.Installer.exe", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var destination = Path.Combine(targetDirectory, relativePath);
        var destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (relativePath.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase) && File.Exists(destination))
        {
            var newSettingsPath = Path.Combine(targetDirectory, $"appsettings.new-{DateTime.Now:yyyyMMddHHmmss}.json");
            File.Copy(file, newSettingsPath, overwrite: false);
            continue;
        }

        File.Copy(file, destination, overwrite: true);
    }
}

static void EnsureUninstallerExists(string installDirectory)
{
    var uninstallerPath = Path.Combine(installDirectory, UninstallExeName);
    if (!File.Exists(uninstallerPath))
    {
        Console.WriteLine($"{UninstallExeName} was not found. The service can still be removed with sc.exe delete.");
    }
}

static void CreateService(string appExePath, string urls)
{
    var binaryPath = $"\"{appExePath}\" --urls {urls}";
    RunProcess("sc.exe", ["create", ServiceName, "binPath=", binaryPath, "start=", "auto"]);
    RunProcess("sc.exe", ["description", ServiceName, "CIP Station alarm email notification service"], ignoreExitCode: true);
}

static void StartService(string serviceName)
{
    RunProcess("sc.exe", ["start", serviceName], ignoreExitCode: true);
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

static void KillExistingAppProcesses(string installDirectory)
{
    foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AppExeName)))
    {
        try
        {
            var path = process.MainModule?.FileName;
            if (path is not null && path.StartsWith(installDirectory, StringComparison.OrdinalIgnoreCase))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }
        }
        catch
        {
            // Ignore processes that disappeared or cannot expose their module path.
        }
    }
}

static void WriteUninstallRegistry(string installDirectory)
{
    using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EmailApp");
    if (key is null)
    {
        return;
    }

    var uninstallPath = Path.Combine(installDirectory, UninstallExeName);
    key.SetValue("DisplayName", AppDisplayName);
    key.SetValue("DisplayVersion", "1.0.0");
    key.SetValue("Publisher", Publisher);
    key.SetValue("InstallLocation", installDirectory);
    key.SetValue("DisplayIcon", Path.Combine(installDirectory, AppExeName));
    key.SetValue("UninstallString", $"\"{uninstallPath}\"");
    key.SetValue("QuietUninstallString", $"\"{uninstallPath}\" --quiet");
    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
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

sealed record InstallerOptions(string? SourceDirectory, string? InstallDirectory, string? Urls)
{
    public static InstallerOptions Parse(string[] args)
    {
        string? sourceDirectory = null;
        string? installDirectory = null;
        string? urls = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--source", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                sourceDirectory = args[++i];
            }
            else if (arg.Equals("--install-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                installDirectory = args[++i];
            }
            else if (arg.Equals("--urls", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                urls = args[++i];
            }
        }

        return new InstallerOptions(sourceDirectory, installDirectory, urls);
    }
}
