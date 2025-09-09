using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        string distDir;

        private string FindDistDir()
        {
            var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);

            // Sobe até 8 níveis procurando pela pasta "dist"
            for (int i = 0; i < 8 && dir != null; i++)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, "dist");
                if (System.IO.Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent!;
            }

            // fallback: cria dist local se não encontrar
            var fallback = System.IO.Path.Combine(AppContext.BaseDirectory, "dist");
            System.IO.Directory.CreateDirectory(fallback);
            return fallback;
        }

        public MainWindow()
        {
            InitializeComponent();
            distDir = FindDistDir();
            AppendLog("[ui] distDir = " + distDir);
        }

        void AppendLog(string line)
        {
            try
            {
                var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var ev = root.TryGetProperty("event", out var e) ? e.GetString() : null;
                var det = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
                LogList.Items.Add(ev != null ? $"[{ev}] {det}" : line);
            }
            catch { LogList.Items.Add(line); }
        }

        async Task<Process> LaunchAsync(string exeName)
        {
            var psi = new ProcessStartInfo(System.IO.Path.Combine(distDir, exeName))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = distDir
            };
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (_, e) => { if (e.Data != null) Dispatcher.Invoke(() => AppendLog(e.Data)); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) Dispatcher.Invoke(() => AppendLog("[ERR] " + e.Data)); };
            p.Exited += (_, __) => Dispatcher.Invoke(UpdateStatus);
            string full = System.IO.Path.Combine(distDir, exeName);
            if (!System.IO.File.Exists(full))
            {
                AppendLog($"[ui] NÃO encontrei: {full}");
                AppendLog($"[ui] Conteúdo de {distDir}:");
                foreach (var f in System.IO.Directory.GetFiles(distDir, "*.exe"))
                    AppendLog(" - " + System.IO.Path.GetFileName(f));
                MessageBox.Show($"Falta {exeName} em {distDir}");
                return null!;
            }
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            await Task.Delay(200);
            return p;
        }

        void UpdateStatus()
        {
            bool anyRunning = procs.Exists(p => !p.HasExited);
            StatusDot.Fill = anyRunning ? Brushes.Green : Brushes.Red;
            StatusText.Text = anyRunning ? "Rodando" : "Parado";
        }

        async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (procs.Exists(p => !p.HasExited)) return;

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
            foreach (var exe in pair) procs.Add(await LaunchAsync(exe));

            senderProc = sel switch
            {
                "Memória" => procs.Find(p => p.StartInfo.FileName.EndsWith("MemWriter.exe", StringComparison.OrdinalIgnoreCase)),
                "Pipes" => procs.Find(p => p.StartInfo.FileName.EndsWith("PipeWriter.exe", StringComparison.OrdinalIgnoreCase)),
                "Sockets" => procs.Find(p => p.StartInfo.FileName.EndsWith("SocketClient.exe", StringComparison.OrdinalIgnoreCase)),
                _ => null
            };
            UpdateStatus();
            AppendLog($"[ui] started {sel}");
        }

        async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in procs)
            {
                try { if (!p.HasExited) await p.StandardInput.WriteLineAsync("sair"); } catch { }
            }
            await Task.Delay(1200);
            foreach (var p in procs)
            {
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
                try { p.Dispose(); } catch { }
            }
            procs.Clear(); senderProc = null;
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
