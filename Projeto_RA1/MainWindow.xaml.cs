using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Projeto_RA1
{
    public partial class MainWindow : Window
    {
        readonly List<Process> procs = new();
        Process? senderProc = null;
        readonly string distDir;

        public MainWindow()
        {
            InitializeComponent();
            distDir = FindDistDir();
        }

        string FindDistDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent!)
            {
                var candidate = Path.Combine(dir.FullName, "dist");
                bool looksRight =
                    Directory.Exists(Path.Combine(candidate, "Pipes")) ||
                    Directory.Exists(Path.Combine(candidate, "SharedMemory")) ||
                    Directory.Exists(Path.Combine(candidate, "Sockets"));
                if (looksRight) return candidate;
            }
            var fallback = Path.Combine(AppContext.BaseDirectory, "dist");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        string? FindExePath(string exeName)
        {
            // raiz
            var full = Path.Combine(distDir, exeName);
            if (File.Exists(full)) return full;
            // subpastas conhecidas
            foreach (var sub in new[] { "Pipes", "SharedMemory", "Sockets" })
            {
                var cand = Path.Combine(distDir, sub, exeName);
                if (File.Exists(cand)) return cand;
            }
            return null;
        }

        void AppendLog(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var ev = root.TryGetProperty("event", out var e) ? e.GetString() : null;
                var det = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
                LogList.Items.Add(ev != null ? $"[{ev}] {det}" : line);
            }
            catch { LogList.Items.Add(line); }
        }

        async Task<Process?> LaunchAsync(string exeName)
        {
            var path = FindExePath(exeName);
            if (path == null)
            {
                AppendLog($"[ui] NÃO encontrei: {exeName} em {distDir}");
                return null;
            }

            var psi = new ProcessStartInfo(path)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path)!,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

            p.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) Dispatcher.BeginInvoke(new Action(() => AppendLog(e.Data)));
            };
            p.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) Dispatcher.BeginInvoke(new Action(() => AppendLog("[ERR] " + e.Data)));
            };
            p.Exited += (_, __) => Dispatcher.BeginInvoke(new Action(UpdateStatus));

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            await Task.Delay(200);
            return p;
        }

        void UpdateStatus()
        {
            bool anyRunning = procs.Exists(p => p != null && !p.HasExited);
            StatusDot.Fill = anyRunning ? Brushes.Green : Brushes.Red;
            StatusText.Text = anyRunning ? "Rodando" : "Parado";
        }

        async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (procs.Exists(p => p != null && !p.HasExited)) return;

            string sel = ((System.Windows.Controls.ComboBoxItem)IpcSelector.SelectedItem).Content?.ToString() ?? "";
            string[] pair = sel switch
            {
                "Memória" => new[] { "MemWriter.exe", "MemReader.exe" },
                "Pipes" => new[] { "PipeWriter.exe", "PipeReader.exe" },
                "Sockets" => new[] { "SocketServer.exe", "SocketClient.exe" },
                _ => Array.Empty<string>()
            };
            if (pair.Length == 0) return;

            procs.Clear();
            foreach (var exe in pair)
            {
                var p = await LaunchAsync(exe);
                if (p != null) procs.Add(p);
            }
            if (procs.Count != pair.Length)
            {
                AppendLog("[ui] start incompleto");
                UpdateStatus();
                return;
            }

            senderProc = sel switch
            {
                "Memória" => procs.Find(p => Path.GetFileName(p.StartInfo.FileName)
                                   .Equals("MemWriter.exe", StringComparison.OrdinalIgnoreCase)),
                "Pipes" => procs.Find(p => Path.GetFileName(p.StartInfo.FileName)
                                   .Equals("PipeWriter.exe", StringComparison.OrdinalIgnoreCase)),
                "Sockets" => procs.Find(p => Path.GetFileName(p.StartInfo.FileName)
                                   .Equals("SocketClient.exe", StringComparison.OrdinalIgnoreCase)),
                _ => null
            };

            UpdateStatus();
            AppendLog($"[ui] started {sel}");
        }

        async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            // pede término educado
            foreach (var p in procs)
            {
                try { if (!p.HasExited) await p.StandardInput.WriteLineAsync("sair"); } catch { }
                try { p.StandardInput.Close(); } catch { }
            }

            await Task.Delay(1500);

            foreach (var p in procs)
            {
                try { if (!p.HasExited) p.CloseMainWindow(); } catch { }
                await Task.Delay(200);
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
                try { p.Dispose(); } catch { }
            }

            procs.Clear();
            senderProc = null;
            UpdateStatus();
            AppendLog("[ui] stopped");
        }

        async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (senderProc is null || senderProc.HasExited) return;
            var msg = MsgInput.Text ?? "";
            if (string.IsNullOrWhiteSpace(msg)) return;
            try { await senderProc.StandardInput.WriteLineAsync(msg); } catch { }
            MsgInput.Clear();
        }

        void BtnClearLog_Click(object sender, RoutedEventArgs e) => LogList.Items.Clear();
    }
}
