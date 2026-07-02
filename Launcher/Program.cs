using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Security.Cryptography;

namespace MMOnsterpatchLauncher;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LauncherForm(args));
    }
}

internal sealed class LauncherForm : Form
{
    private const string LauncherVersion = "v0.11.2";
    private const string PatcherFileName = "MMOnsterpatchOfficialServerPatcher.dll";
    private const string PatcherPdbName = "MMOnsterpatchOfficialServerPatcher.pdb";
    private const string BackupDirName = "BepInEx.MMOnsterpatchOfflineBackup";
    private const string RuntimeMarkerFileName = ".mmonsterpatch_online_runtime";
    private const string RootBackupSuffix = ".MMOnsterpatchOfflineBackup";

    private readonly TextBox _gamePath = new() { Left = 18, Top = 22, Width = 515 };
    private readonly Button _browse = new() { Left = 542, Top = 20, Width = 86, Height = 28, Text = "Browse" };
    private readonly Button _playOnline = new() { Left = 18, Top = 64, Width = 250, Height = 46, Text = "Play Online" };
    private readonly Button _playOffline = new() { Left = 282, Top = 64, Width = 250, Height = 46, Text = "Play Offline" };
    private readonly Button _restoreNow = new() { Left = 542, Top = 64, Width = 86, Height = 46, Text = "Restore" };
    private readonly CheckBox _cleanRuntime = new() { Left = 18, Top = 124, Width = 500, Text = "Use clean online BepInEx runtime staged by launcher", Checked = true };
    private readonly CheckBox _restoreOnExit = new() { Left = 18, Top = 150, Width = 500, Text = "Restore offline BepInEx/mods and reopen launcher when game closes", Checked = true };
    private readonly Label _status = new() { Left = 18, Top = 184, Width = 610, Height = 125, AutoSize = false, Text = "Select Monsterpatch.exe, then Play Online. The launcher will stage its payload and will refuse to launch vanilla if online runtime staging is incomplete." };

    private Process? _runningGame;
    private string? _runningGameDir;
    private string? _runningGameProcessName;
    private readonly string? _autoPlayOnlineExe;

    public LauncherForm(string[]? args = null)
    {
        Text = $"MMOnsterpatch Launcher {LauncherVersion}";
        Width = 666;
        Height = 372;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Controls.AddRange(new Control[] { _gamePath, _browse, _playOnline, _playOffline, _restoreNow, _cleanRuntime, _restoreOnExit, _status });
        _gamePath.Text = LoadPath();
        _browse.Click += (_, _) => Browse();
        _playOnline.Click += (_, _) => Launch(true);
        _playOffline.Click += (_, _) => Launch(false);
        _restoreNow.Click += (_, _) => RestoreSelectedGame();

        _autoPlayOnlineExe = ParseAutoPlayOnlineExe(args);
        if (!string.IsNullOrWhiteSpace(_autoPlayOnlineExe))
        {
            _gamePath.Text = _autoPlayOnlineExe;
            SavePath(_autoPlayOnlineExe);
            _status.Text = IsRunningAsAdministrator()
                ? "Elevated online launch approved. Staging online runtime..."
                : "Online launch requested, but the launcher is not elevated.";
            Shown += (_, _) => BeginInvoke(new Action(() => Launch(true)));
        }
    }

    private static string? ParseAutoPlayOnlineExe(string[]? args)
    {
        if (args == null) return null;
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--play-online", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1].Trim().Trim('"');
        }
        return null;
    }

    private static string SettingsDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MMOnsterpatch");
    private static string SettingsPath => Path.Combine(SettingsDir, "launcher_settings.txt");
    private static string SessionPath => Path.Combine(SettingsDir, "launcher_session.json");
    private static string StageLogPath => Path.Combine(SettingsDir, "launcher_stage_log.txt");

    private static string LoadPath() => File.Exists(SettingsPath) ? File.ReadAllText(SettingsPath).Trim() : string.Empty;

    private static void SavePath(string path)
    {
        Directory.CreateDirectory(SettingsDir);
        File.WriteAllText(SettingsPath, path);
    }

    private void Browse()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Monsterpatch executable|Monsterpatch.exe|Executable|*.exe|All files|*.*",
            Title = "Select Monsterpatch.exe"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _gamePath.Text = ofd.FileName;
            SavePath(ofd.FileName);
        }
    }

    private void Launch(bool online)
    {
        string exe = _gamePath.Text.Trim().Trim('"');
        if (!File.Exists(exe))
        {
            MessageBox.Show(this, "Monsterpatch.exe was not found.");
            return;
        }

        string gameDir = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory;
        SavePath(exe);
        Directory.CreateDirectory(SettingsDir);
        BeginStageLog($"Launch requested. online={online}; exe={exe}; gameDir={gameDir}");

        try
        {
            if (online && _cleanRuntime.Checked && ShouldElevateForOnlineStaging(gameDir))
            {
                RelaunchElevatedForOnline(exe);
                return;
            }

            if (online)
            {
                string stageSummary;
                if (_cleanRuntime.Checked)
                {
                    stageSummary = StageOnlineRuntime(gameDir);
                    _status.Text = stageSummary;
                }
                else
                {
                    stageSummary = "Clean runtime staging is unchecked. Launcher session will be created, but the existing game BepInEx folder will be used.";
                    AppendStageLog(stageSummary);
                    _status.Text = stageSummary;
                }

                File.WriteAllText(SessionPath, JsonSerializer.Serialize(new
                {
                    mode = "online",
                    version = LauncherVersion,
                    createdUtc = DateTime.UtcNow,
                    token = Guid.NewGuid().ToString("N"),
                    stagedRuntime = _cleanRuntime.Checked
                }, new JsonSerializerOptions { WriteIndented = true }));
                AppendStageLog("Session written: " + SessionPath);
            }
            else
            {
                if (File.Exists(SessionPath)) File.Delete(SessionPath);
                _status.Text = "Starting Monsterpatch offline. The launcher will not stage or change BepInEx for offline launch.";
                AppendStageLog("Offline launch selected. Session cleared.");
            }

            var psi = new ProcessStartInfo(exe)
            {
                WorkingDirectory = gameDir,
                UseShellExecute = false
            };

            if (online)
            {
                psi.Environment["MMONSTERPATCH_LAUNCHER"] = "1";
                psi.Environment["MMONSTERPATCH_LAUNCHER_SESSION"] = SessionPath;
                psi.Environment["MMONSTERPATCH_ONLINE_RUNTIME"] = _cleanRuntime.Checked ? "1" : "0";
            }

            var p = Process.Start(psi);
            if (p == null)
            {
                MessageBox.Show(this, "Monsterpatch did not start.");
                AppendStageLog("Process.Start returned null.");
                return;
            }

            _runningGame = p;
            _runningGameDir = gameDir;
            _runningGameProcessName = Path.GetFileNameWithoutExtension(exe);
            AppendStageLog($"Process started. pid={p.Id}; name={_runningGameProcessName}");

            if (online)
            {
                _status.Text = (_cleanRuntime.Checked ? "Online runtime staged. " : "Online launcher session created. ") + "Monsterpatch is running. Launcher hidden until game closes.";
                SetLaunchButtonsEnabled(false);
                ShowInTaskbar = false;
                Hide();
                p.EnableRaisingEvents = true;
                p.Exited += (_, _) => _ = Task.Run(OnGameExitedAfterProcessTreeAsync);
            }
        }
        catch (UnauthorizedAccessException ex) when (online && !IsRunningAsAdministrator())
        {
            AppendStageLog("Access denied while staging online runtime. Relaunching elevated. " + ex);
            RelaunchElevatedForOnline(exe);
        }
        catch (Exception ex)
        {
            _status.Text = "Launch failed: " + ex.Message;
            AppendStageLog("Launch failed: " + ex);
            MessageBox.Show(this, ex.Message, "MMOnsterpatch Launcher");
            if (online)
            {
                TryDeleteSession();
                if (_cleanRuntime.Checked)
                {
                    try { RestoreOnlineRuntime(gameDir); } catch { }
                }
            }
        }
    }

    private void RelaunchElevatedForOnline(string exe)
    {
        try
        {
            SavePath(exe);
            Directory.CreateDirectory(SettingsDir);
            AppendStageLog("Online staging needs elevated file access. Relaunching launcher as administrator for: " + exe);

            string? launcherExe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(launcherExe) || !File.Exists(launcherExe))
                launcherExe = Application.ExecutablePath;

            string args = "--play-online " + QuoteArg(exe);
            var psi = new ProcessStartInfo(launcherExe)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(launcherExe) ?? AppContext.BaseDirectory
            };

            Process.Start(psi);
            _status.Text = "Windows needs admin permission to stage the online runtime inside this Steam install. Approve the UAC prompt to continue.";
            Close();
        }
        catch (Exception elevateEx)
        {
            _status.Text = "Online staging needs admin permission, but elevation did not start: " + elevateEx.Message;
            AppendStageLog("Elevation failed: " + elevateEx);
            MessageBox.Show(this,
                "Windows blocked access to the Monsterpatch install folder.\n\n" +
                "This happens when the Steam library is under Program Files.\n\n" +
                "Run the launcher as Administrator or move Monsterpatch to a non-protected Steam library such as C:\\Games\\SteamLibrary.",
                "MMOnsterpatch Launcher");
        }
    }

    private static string QuoteArg(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static bool ShouldElevateForOnlineStaging(string gameDir)
    {
        if (IsRunningAsAdministrator()) return false;
        if (IsLikelyProtectedInstallPath(gameDir))
        {
            AppendStageLog("Protected install path detected, elevation required: " + gameDir);
            return true;
        }
        if (!CanWriteProbe(gameDir))
        {
            AppendStageLog("Write probe failed, elevation required: " + gameDir);
            return true;
        }
        return false;
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLikelyProtectedInstallPath(string gameDir)
    {
        string full = Path.GetFullPath(gameDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (string root in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        })
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (full.Equals(rootFull, StringComparison.OrdinalIgnoreCase) || full.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool CanWriteProbe(string gameDir)
    {
        string testPath = Path.Combine(gameDir, ".mmonsterpatch_write_test_" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(testPath, "test");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            try { if (File.Exists(testPath)) File.Delete(testPath); } catch { }
            return false;
        }
    }

    private async Task OnGameExitedAfterProcessTreeAsync()
    {
        string? processName = _runningGameProcessName;
        string? gameDir = _runningGameDir;
        AppendStageLog("Primary Monsterpatch process exited. Waiting briefly for child/relaunched process check.");

        await Task.Delay(1500);
        if (!string.IsNullOrWhiteSpace(processName))
        {
            for (int i = 0; i < 720; i++)
            {
                if (!IsProcessStillRunning(processName!, gameDir)) break;
                await Task.Delay(2500);
            }
        }

        OnGameExited();
    }

    private static bool IsProcessStillRunning(string processName, string? gameDir)
    {
        try
        {
            foreach (Process proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (proc.HasExited) continue;
                    if (string.IsNullOrWhiteSpace(gameDir)) return true;
                    string? modulePath = null;
                    try { modulePath = proc.MainModule?.FileName; } catch { }
                    if (string.IsNullOrWhiteSpace(modulePath)) return true;
                    string? procDir = Path.GetDirectoryName(modulePath);
                    if (string.Equals(procDir, gameDir, StringComparison.OrdinalIgnoreCase)) return true;
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    private void OnGameExited()
    {
        string? gameDir = _runningGameDir;
        string result = "Monsterpatch closed.";

        try
        {
            TryDeleteSession();
            if (_restoreOnExit.Checked && !string.IsNullOrWhiteSpace(gameDir))
            {
                result = RestoreOnlineRuntime(gameDir!);
            }
        }
        catch (Exception ex)
        {
            result = "Game closed, but restore had an issue: " + ex.Message;
            AppendStageLog("Restore after exit failed: " + ex);
        }

        try
        {
            BeginInvoke(new Action(() =>
            {
                _status.Text = result;
                SetLaunchButtonsEnabled(true);
                ShowInTaskbar = true;
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }));
        }
        catch
        {
            // Window may already be closing.
        }
    }

    private void RestoreSelectedGame()
    {
        string exe = _gamePath.Text.Trim().Trim('"');
        if (!File.Exists(exe))
        {
            MessageBox.Show(this, "Monsterpatch.exe was not found.");
            return;
        }

        string gameDir = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory;
        try
        {
            TryDeleteSession();
            _status.Text = RestoreOnlineRuntime(gameDir);
        }
        catch (Exception ex)
        {
            _status.Text = "Restore failed: " + ex.Message;
            AppendStageLog("Manual restore failed: " + ex);
            MessageBox.Show(this, ex.Message, "MMOnsterpatch Launcher Restore");
        }
    }

    private void SetLaunchButtonsEnabled(bool enabled)
    {
        _browse.Enabled = enabled;
        _playOnline.Enabled = enabled;
        _playOffline.Enabled = enabled;
        _restoreNow.Enabled = enabled;
        _cleanRuntime.Enabled = enabled;
    }

    private static void TryDeleteSession()
    {
        try { if (File.Exists(SessionPath)) File.Delete(SessionPath); } catch { }
    }

    private static string StageOnlineRuntime(string gameDir)
    {
        string liveBepInEx = Path.Combine(gameDir, "BepInEx");
        string offlineBackup = Path.Combine(gameDir, BackupDirName);
        string marker = Path.Combine(liveBepInEx, RuntimeMarkerFileName);
        string packageRoot = FindPackageRoot();
        string launcherRoot = FindLauncherRoot();
        AppendStageLog($"StageOnlineRuntime: packageRoot={packageRoot}; launcherRoot={launcherRoot}");

        if (Directory.Exists(offlineBackup) && Directory.Exists(liveBepInEx) && File.Exists(marker))
        {
            AppendStageLog("Removing leftover launcher-staged live BepInEx before restage.");
            Directory.Delete(liveBepInEx, true);
        }

        if (Directory.Exists(offlineBackup) && Directory.Exists(liveBepInEx) && !File.Exists(marker))
        {
            throw new InvalidOperationException("An offline BepInEx backup already exists, and the live BepInEx folder is not marked as launcher-staged. Click Restore first, or manually check BepInEx and BepInEx.MMOnsterpatchOfflineBackup before launching online again.");
        }

        if (!Directory.Exists(offlineBackup) && Directory.Exists(liveBepInEx))
        {
            AppendStageLog("Moving live BepInEx to offline backup: " + offlineBackup);
            Directory.Move(liveBepInEx, offlineBackup);
        }

        string? payloadBepInEx = FindPayloadBepInEx(packageRoot, launcherRoot);
        string? sourceCore = null;
        string coreSourceReason = string.Empty;

        if (!string.IsNullOrWhiteSpace(payloadBepInEx) && Directory.Exists(Path.Combine(payloadBepInEx, "core")))
        {
            sourceCore = Path.Combine(payloadBepInEx, "core");
            coreSourceReason = "Launcher Payload BepInEx/core";
        }
        else if (Directory.Exists(Path.Combine(offlineBackup, "core")))
        {
            sourceCore = Path.Combine(offlineBackup, "core");
            coreSourceReason = "existing offline BepInEx/core backup";
        }

        if (string.IsNullOrWhiteSpace(sourceCore) || !Directory.Exists(sourceCore))
        {
            throw new InvalidOperationException("No BepInEx/core source was found. For this launcher-owned test, keep your existing BepInEx folder installed so the launcher can borrow BepInEx/core, or place a full BepInEx runtime in Launcher\\Payload\\BepInEx.");
        }

        Directory.CreateDirectory(liveBepInEx);
        File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
        CopyDirectory(sourceCore, Path.Combine(liveBepInEx, "core"), overwrite: true);
        Directory.CreateDirectory(Path.Combine(liveBepInEx, "config"));
        Directory.CreateDirectory(Path.Combine(liveBepInEx, "plugins"));
        Directory.CreateDirectory(Path.Combine(liveBepInEx, "patchers"));

        string patcherSource = FindPatcherDll(packageRoot, launcherRoot);
        string patcherTarget = Path.Combine(liveBepInEx, "patchers", PatcherFileName);
        File.Copy(patcherSource, patcherTarget, overwrite: true);
        if (!File.Exists(patcherTarget) || new FileInfo(patcherTarget).Length <= 0)
            throw new IOException("The MMOnsterpatch patcher was not copied into the live online BepInEx patchers folder.");

        string? patcherPdb = FindExistingFile(new[]
        {
            Path.ChangeExtension(patcherSource, ".pdb"),
            Path.Combine(packageRoot, "patchers", PatcherPdbName),
            Path.Combine(packageRoot, "Source", "bin", "Release", "net472", PatcherPdbName),
            Path.Combine(launcherRoot, "Payload", "BepInEx", "patchers", PatcherPdbName),
            Path.Combine(AppContext.BaseDirectory, "Payload", "BepInEx", "patchers", PatcherPdbName)
        });
        if (!string.IsNullOrWhiteSpace(patcherPdb))
        {
            File.Copy(patcherPdb, Path.Combine(liveBepInEx, "patchers", PatcherPdbName), overwrite: true);
        }

        string bootstrapSummary = EnsureBootstrapFiles(gameDir, packageRoot, launcherRoot);
        string readinessSummary = ValidateOnlineRuntimeReady(gameDir, patcherSource);
        string relativePatcher = MakeRelativeSafe(gameDir, patcherTarget);
        string summary = "Online runtime staged successfully.\n" +
                         "BepInEx/core source: " + coreSourceReason + "\n" +
                         "Patcher staged: " + relativePatcher + "\n" +
                         bootstrapSummary + "\n" +
                         readinessSummary + "\n" +
                         "Offline BepInEx/mods are isolated until the game closes.";
        AppendStageLog(summary);
        return summary;
    }

    private static string RestoreOnlineRuntime(string gameDir)
    {
        string liveBepInEx = Path.Combine(gameDir, "BepInEx");
        string offlineBackup = Path.Combine(gameDir, BackupDirName);
        string marker = Path.Combine(liveBepInEx, RuntimeMarkerFileName);
        AppendStageLog("RestoreOnlineRuntime: " + gameDir);

        if (Directory.Exists(liveBepInEx) && File.Exists(marker))
        {
            Directory.Delete(liveBepInEx, true);
            AppendStageLog("Deleted launcher-staged live BepInEx.");
        }

        if (Directory.Exists(offlineBackup) && !Directory.Exists(liveBepInEx))
        {
            Directory.Move(offlineBackup, liveBepInEx);
            RestoreRootFileBackup(Path.Combine(gameDir, "doorstop_config.ini"));
            return "Offline BepInEx/mods restored. Launcher session cleared.";
        }

        if (Directory.Exists(offlineBackup) && Directory.Exists(liveBepInEx))
        {
            return "Offline backup still exists, but live BepInEx was not launcher-staged. I did not overwrite it. Check BepInEx and BepInEx.MMOnsterpatchOfflineBackup manually.";
        }

        RestoreRootFileBackup(Path.Combine(gameDir, "doorstop_config.ini"));
        return "No launcher-staged online runtime needed restore. Launcher session cleared.";
    }

    private static string ValidateOnlineRuntimeReady(string gameDir, string stagedPatcherPath)
    {
        string liveBepInEx = Path.Combine(gameDir, "BepInEx");
        string preloader = Path.Combine(liveBepInEx, "core", "BepInEx.Preloader.dll");
        string patchersDir = Path.Combine(liveBepInEx, "patchers");
        string patcher = Path.Combine(patchersDir, PatcherFileName);
        string winhttp = Path.Combine(gameDir, "winhttp.dll");
        string doorstop = Path.Combine(gameDir, "doorstop_config.ini");
        string marker = Path.Combine(liveBepInEx, RuntimeMarkerFileName);

        var missing = new List<string>();
        if (!Directory.Exists(liveBepInEx)) missing.Add("BepInEx/");
        if (!File.Exists(marker)) missing.Add("BepInEx/" + RuntimeMarkerFileName);
        if (!File.Exists(preloader)) missing.Add(@"BepInEx\core\BepInEx.Preloader.dll");
        if (!Directory.Exists(patchersDir)) missing.Add(@"BepInEx\patchers\");
        if (!File.Exists(patcher)) missing.Add(@"BepInEx\patchers\" + PatcherFileName);
        if (!File.Exists(winhttp)) missing.Add("winhttp.dll");
        if (!File.Exists(doorstop)) missing.Add("doorstop_config.ini");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException("Online runtime staging is incomplete. Online launch was stopped so Monsterpatch does not boot vanilla. Missing: " + string.Join(", ", missing));
        }

        string doorstopText = File.ReadAllText(doorstop);
        if (doorstopText.IndexOf("enabled=true", StringComparison.OrdinalIgnoreCase) < 0 ||
            doorstopText.IndexOf(@"targetAssembly=BepInEx\core\BepInEx.Preloader.dll", StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException("doorstop_config.ini is not pointing at BepInEx\\core\\BepInEx.Preloader.dll. Online launch was stopped so Monsterpatch does not boot vanilla.");
        }

        long patcherSize = new FileInfo(patcher).Length;
        if (patcherSize <= 0)
        {
            throw new InvalidOperationException("The staged MMOnsterpatch patcher DLL is empty. Online launch was stopped so Monsterpatch does not boot vanilla.");
        }

        string stagedHash = TrySha256(patcher);
        string sourceHash = File.Exists(stagedPatcherPath) ? TrySha256(stagedPatcherPath) : "source-missing-after-copy";
        if (!string.Equals(stagedHash, sourceHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("The staged MMOnsterpatch patcher hash does not match the payload/source DLL. Online launch was stopped so Monsterpatch does not boot vanilla.");
        }

        string summary = "Runtime readiness verified: patcher size=" + patcherSize + " sha256=" + stagedHash + "; doorstop target ok; preloader present.";
        AppendStageLog(summary);
        return summary;
    }

    private static string EnsureBootstrapFiles(string gameDir, string packageRoot, string launcherRoot)
    {
        string winhttp = Path.Combine(gameDir, "winhttp.dll");
        string doorstop = Path.Combine(gameDir, "doorstop_config.ini");
        bool hadWinhttp = File.Exists(winhttp);
        bool hadDoorstop = File.Exists(doorstop);

        string? payloadRoot = FindPayloadRoot(packageRoot, launcherRoot);
        if (payloadRoot != null)
        {
            foreach (string file in Directory.GetFiles(payloadRoot))
            {
                string name = Path.GetFileName(file);
                if (name.Equals("README.md", StringComparison.OrdinalIgnoreCase)) continue;
                string target = Path.Combine(gameDir, name);
                if (!File.Exists(target)) File.Copy(file, target, overwrite: false);
            }
        }

        bool hasWinhttp = File.Exists(winhttp);
        bool hasDoorstop = File.Exists(doorstop);
        if (!hasWinhttp || !hasDoorstop)
        {
            throw new InvalidOperationException("BepInEx bootstrap files were not found in the game folder. Keep winhttp.dll and doorstop_config.ini in the Monsterpatch folder for this test, or place them in Launcher\\Payload\\Root so the launcher can stage them.");
        }

        string doorstopSummary = EnsureDoorstopConfigEnabled(doorstop);
        string bootstrapSource = hadWinhttp && hadDoorstop
            ? "existing game-root winhttp.dll + doorstop_config.ini used"
            : "missing game-root bootstrap files copied from Launcher/Payload/Root";
        return "BepInEx bootstrap: " + bootstrapSource + "; " + doorstopSummary;
    }

    private static string EnsureDoorstopConfigEnabled(string doorstopPath)
    {
        BackupRootFileOnce(doorstopPath);
        string text = File.ReadAllText(doorstopPath);
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool sawEnabled = false;
        bool sawTarget = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith("enabled=", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = "enabled=true";
                sawEnabled = true;
            }
            else if (trimmed.StartsWith("targetAssembly=", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = @"targetAssembly=BepInEx\core\BepInEx.Preloader.dll";
                sawTarget = true;
            }
        }

        var finalLines = new List<string>(lines.Where((line, index) => index < lines.Length - 1 || line.Length > 0));
        if (!sawEnabled) finalLines.Add("enabled=true");
        if (!sawTarget) finalLines.Add(@"targetAssembly=BepInEx\core\BepInEx.Preloader.dll");
        File.WriteAllText(doorstopPath, string.Join(Environment.NewLine, finalLines) + Environment.NewLine);
        return "doorstop enabled for BepInEx\\core\\BepInEx.Preloader.dll";
    }

    private static void BackupRootFileOnce(string path)
    {
        string backup = path + RootBackupSuffix;
        if (File.Exists(path) && !File.Exists(backup))
            File.Copy(path, backup, overwrite: false);
    }

    private static void RestoreRootFileBackup(string path)
    {
        string backup = path + RootBackupSuffix;
        if (File.Exists(backup))
        {
            File.Copy(backup, path, overwrite: true);
            File.Delete(backup);
            AppendStageLog("Restored root file backup: " + path);
        }
    }

    private static string FindPackageRoot()
    {
        string dir = AppContext.BaseDirectory;
        var probe = new DirectoryInfo(dir);
        while (probe != null)
        {
            if (Directory.Exists(Path.Combine(probe.FullName, "Source")) && Directory.Exists(Path.Combine(probe.FullName, "Launcher")))
                return probe.FullName;
            if (File.Exists(Path.Combine(probe.FullName, "CHANGELOG.md")) && Directory.Exists(Path.Combine(probe.FullName, "Source")))
                return probe.FullName;
            probe = probe.Parent;
        }

        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            current = Path.GetFullPath(Path.Combine(current, ".."));
            if (Directory.Exists(Path.Combine(current, "Source"))) return current;
        }

        return AppContext.BaseDirectory;
    }

    private static string FindLauncherRoot()
    {
        string dir = AppContext.BaseDirectory;
        var probe = new DirectoryInfo(dir);
        while (probe != null)
        {
            if (File.Exists(Path.Combine(probe.FullName, "MMOnsterpatchLauncher.csproj")) || Directory.Exists(Path.Combine(probe.FullName, "Payload")))
                return probe.FullName;
            probe = probe.Parent;
        }

        string packageRoot = FindPackageRoot();
        string launcher = Path.Combine(packageRoot, "Launcher");
        return Directory.Exists(launcher) ? launcher : AppContext.BaseDirectory;
    }

    private static string? FindPayloadBepInEx(string packageRoot, string launcherRoot)
    {
        return FindExistingDirectory(new[]
        {
            Path.Combine(launcherRoot, "Payload", "BepInEx"),
            Path.Combine(packageRoot, "Launcher", "Payload", "BepInEx"),
            Path.Combine(AppContext.BaseDirectory, "Payload", "BepInEx")
        });
    }

    private static string? FindPayloadRoot(string packageRoot, string launcherRoot)
    {
        return FindExistingDirectory(new[]
        {
            Path.Combine(launcherRoot, "Payload", "Root"),
            Path.Combine(packageRoot, "Launcher", "Payload", "Root"),
            Path.Combine(AppContext.BaseDirectory, "Payload", "Root")
        });
    }

    private static string FindPatcherDll(string packageRoot, string launcherRoot)
    {
        string[] payloadCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Payload", "BepInEx", "patchers", PatcherFileName),
            Path.Combine(launcherRoot, "Payload", "BepInEx", "patchers", PatcherFileName),
            Path.Combine(packageRoot, "Launcher", "Payload", "BepInEx", "patchers", PatcherFileName)
        };

        string? payloadFound = FindExistingFile(payloadCandidates);
        if (!string.IsNullOrWhiteSpace(payloadFound))
        {
            AppendStageLog("Patcher payload found: " + payloadFound + " size=" + new FileInfo(payloadFound).Length + " sha256=" + TrySha256(payloadFound));
            return payloadFound;
        }

        string? devFound = FindExistingFile(new[]
        {
            Path.Combine(packageRoot, "patchers", PatcherFileName),
            Path.Combine(packageRoot, "Source", "bin", "Release", "net472", PatcherFileName),
            Path.Combine(packageRoot, "Source", "bin", "Debug", "net472", PatcherFileName)
        });

        if (!string.IsNullOrWhiteSpace(devFound))
        {
            AppendStageLog("Patcher dev fallback found: " + devFound + " size=" + new FileInfo(devFound).Length + " sha256=" + TrySha256(devFound));
            return devFound;
        }

        string searched = string.Join(Environment.NewLine + "  - ", payloadCandidates);
        throw new FileNotFoundException("MMOnsterpatchOfficialServerPatcher.dll was not found in the launcher payload. Online launch was stopped so the game does not boot vanilla. Build the client patcher first, or place the DLL in Launcher\\Payload\\BepInEx\\patchers. Searched:" + Environment.NewLine + "  - " + searched);
    }

    private static string TrySha256(string path)
    {
        try
        {
            using FileStream fs = File.OpenRead(path);
            byte[] hash = SHA256.HashData(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            return "sha256-error:" + ex.GetType().Name;
        }
    }

    private static string? FindExistingFile(IEnumerable<string> paths)
    {
        return paths.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    private static string? FindExistingDirectory(IEnumerable<string> paths)
    {
        return paths.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p));
    }

    private static void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
    {
        Directory.CreateDirectory(targetDir);
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string target = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, target, overwrite);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string target = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, target, overwrite);
        }
    }

    private static string MakeRelativeSafe(string root, string path)
    {
        try { return Path.GetRelativePath(root, path); }
        catch { return path; }
    }

    private static void BeginStageLog(string text)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(StageLogPath, DateTime.Now + Environment.NewLine + text + Environment.NewLine);
        }
        catch { }
    }

    private static void AppendStageLog(string text)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.AppendAllText(StageLogPath, DateTime.Now + Environment.NewLine + text + Environment.NewLine);
        }
        catch { }
    }
}
