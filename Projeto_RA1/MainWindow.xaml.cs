using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using IOPath = System.IO.Path; // evita conflito com System.Windows.Shapes.Path

namespace Projeto_RA1
{
    public partial class MainWindow : Window
    {
        // ===== Processos / dist =====
        readonly List<Process> procs = new();
        Process? senderProc = null;
        readonly string distDir;

        // ===== Visual IPC =====
        enum IpcState { Parado, Conectando, Conectado, Erro }

        System.Windows.Point uiPos = new(120, 120);
        System.Windows.Point bePos = new(520, 120);

        Ellipse? uiNode, beNode;
        Line? linkLine;
        TextBlock? counters;
        int sent, recv, errs;

        public MainWindow()
        {
            InitializeComponent();
            distDir = FindDistDir();
            Loaded += Window_Loaded;
        }

        // ===== Helpers de processo =====
        static bool IsAlive(Process? p)
        {
            if (p is null) return false;
            try { return !p.HasExited; }
            catch { return false; }
        }
        static bool AnyRunning(IEnumerable<Process> list)
        {
            foreach (var p in list) if (IsAlive(p)) return true;
            return false;
        }

        // ===== Visual: lifecycle =====
        void Window_Loaded(object? s, RoutedEventArgs e)
        {
            InitVisual();
            SetState(IpcState.Parado);
            SetIOEnabled(false);   // caixa e botão OFF no início
            UpdateButtons();
            UpdateLocks();
        }

        void InitVisual()
        {
            VisualArea.Children.Clear();

            linkLine = new Line
            {
                X1 = uiPos.X,
                Y1 = uiPos.Y,
                X2 = bePos.X,
                Y2 = bePos.Y,
                Stroke = Brushes.Gray,
                StrokeThickness = 3
            };
            VisualArea.Children.Add(linkLine);

            uiNode = new Ellipse { Width = 40, Height = 40, Fill = Brushes.SlateBlue };
            Canvas.SetLeft(uiNode, uiPos.X - 20); Canvas.SetTop(uiNode, uiPos.Y - 20);
            VisualArea.Children.Add(uiNode);

            beNode = new Ellipse { Width = 40, Height = 40, Fill = Brushes.DarkGray };
            Canvas.SetLeft(beNode, bePos.X - 20); Canvas.SetTop(beNode, bePos.Y - 20);
            VisualArea.Children.Add(beNode);

            counters = new TextBlock { FontSize = 12, Foreground = Brushes.Black };
            Canvas.SetLeft(counters, 8); Canvas.SetTop(counters, 8);
            VisualArea.Children.Add(counters);
            UpdateCounters();
        }

        void SetState(IpcState st)
        {
            Brush link = st switch
            {
                IpcState.Conectado => Brushes.Green,
                IpcState.Conectando => Brushes.Goldenrod,
                IpcState.Erro => Brushes.Red,
                _ => Brushes.Gray
            };
            if (linkLine != null) linkLine.Stroke = link;
            if (beNode != null) beNode.Fill = link;
        }

        void UpdateCounters()
        {
            if (counters != null)
                counters.Text = $"Enviadas: {sent}  Recebidas: {recv}  Erros: {errs}";
        }

        void AnimatePacket(bool uiToBackend)
        {
            var start = uiToBackend ? uiPos : bePos;
            var end = uiToBackend ? bePos : uiPos;

            var dot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Black, Opacity = 0.9 };
            VisualArea.Children.Add(dot);
            Canvas.SetLeft(dot, start.X - 5);
            Canvas.SetTop(dot, start.Y - 5);

            var sb = new Storyboard();

            var ax = new DoubleAnimation
            {
                From = start.X - 5,
                To = end.X - 5,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(ax, dot);
            Storyboard.SetTargetProperty(ax, new PropertyPath("(Canvas.Left)"));
            sb.Children.Add(ax);

            var ay = new DoubleAnimation
            {
                From = start.Y - 5,
                To = end.Y - 5,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(ay, dot);
            Storyboard.SetTargetProperty(ay, new PropertyPath("(Canvas.Top)"));
            sb.Children.Add(ay);

            sb.Completed += (_, __) => VisualArea.Children.Remove(dot);
            sb.Begin();
        }

        void OnStartVisual() { sent = recv = errs = 0; UpdateCounters(); SetState(IpcState.Conectando); }
        void OnConnectedVisual() => SetState(IpcState.Conectado);
        void OnSendVisual() { sent++; UpdateCounters(); AnimatePacket(true); }
        void OnRecvVisual() { recv++; UpdateCounters(); AnimatePacket(false); }
        void OnErrorVisual() { errs++; UpdateCounters(); SetState(IpcState.Erro); }
        void OnStopVisual() => SetState(IpcState.Parado);

        // ===== Proc launch / dist =====
        string FindDistDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent!)
            {
                var candidate = IOPath.Combine(dir.FullName, "dist");
                bool looksRight =
                    Directory.Exists(IOPath.Combine(candidate, "Pipes")) ||
                    Directory.Exists(IOPath.Combine(candidate, "SharedMemory")) ||
                    Directory.Exists(IOPath.Combine(candidate, "Sockets"));
                if (looksRight) return candidate;
            }
            var fallback = IOPath.Combine(AppContext.BaseDirectory, "dist");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        string? FindExePath(string exeName)
        {
            var full = IOPath.Combine(distDir, exeName);
            if (File.Exists(full)) return full;
            foreach (var sub in new[] { "Pipes", "SharedMemory", "Sockets" })
            {
                var cand = IOPath.Combine(distDir, sub, exeName);
                if (File.Exists(cand)) return cand;
            }
            return null;
        }

        // ===== Log + mapeamento visual =====
        void AppendLog(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var ev = root.TryGetProperty("event", out var e) ? e.GetString() : null;
                var det = root.TryGetProperty("detail", out var d) ? d.GetString() : null;

                if (ev != null)
                {
                    LogList.Items.Add($"[{ev}] {det}");
                    MapVisualEvent(ev);
                }
                else
                {
                    LogList.Items.Add(line);
                }
            }
            catch
            {
                LogList.Items.Add(line);
            }
        }

        void MapVisualEvent(string ev)
        {
            switch (ev)
            {
                case "waiting_connection": OnStartVisual(); break;
                case "server_started":
                case "bind_success":
                case "client_connected": OnConnectedVisual(); break;
                case "send": OnSendVisual(); break;
                case "recv": OnRecvVisual(); break;
                case "error":
                case "exception": OnErrorVisual(); break;
            }
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
                WorkingDirectory = IOPath.GetDirectoryName(path)!,
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
                if (e.Data != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AppendLog("[ERR] " + e.Data);
                        OnErrorVisual();
                    }));
                }
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
            bool anyRunning = AnyRunning(procs);
            StatusDot.Fill = anyRunning ? Brushes.Green : Brushes.Red;
            StatusText.Text = anyRunning ? "Rodando" : "Parado";
            UpdateButtons();
            SetIOEnabled(anyRunning);
            UpdateLocks();
        }

        // ===== UI Handlers =====
        async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (AnyRunning(procs)) return;
            SetIOEnabled(false);           // evita envio antes de subir
            BtnStart.IsEnabled = false;    // trava duplo clique
            BtnStop.IsEnabled = true;
            IpcSelector.IsEnabled = false;  // evita troca durante conexão

            LogList.Items.Clear();
            OnStartVisual();

            string sel = ((ComboBoxItem)IpcSelector.SelectedItem).Content?.ToString() ?? "";
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
                UpdateStatus();            // reverte botões e IO
                return;
            }

            senderProc = sel switch
            {
                "Memória" => procs.Find(p => IOPath.GetFileName(p.StartInfo.FileName)
                                   .Equals("MemWriter.exe", StringComparison.OrdinalIgnoreCase)),
                "Pipes" => procs.Find(p => IOPath.GetFileName(p.StartInfo.FileName)
                                   .Equals("PipeWriter.exe", StringComparison.OrdinalIgnoreCase)),
                "Sockets" => procs.Find(p => IOPath.GetFileName(p.StartInfo.FileName)
                                   .Equals("SocketClient.exe", StringComparison.OrdinalIgnoreCase)),
                _ => null
            };

            UpdateStatus();                // habilita IO
            AppendLog($"[ui] started {sel}");
        }

        async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            SetIOEnabled(false);

            foreach (var p in procs)
            {
                try { if (IsAlive(p)) await p.StandardInput.WriteLineAsync("sair"); } catch { }
                try { p?.StandardInput.Close(); } catch { }
            }

            await Task.Delay(1500);

            foreach (var p in procs)
            {
                try { if (IsAlive(p)) p.CloseMainWindow(); } catch { }
                await Task.Delay(200);
                try { if (IsAlive(p)) p.Kill(entireProcessTree: true); } catch { }
                try { p?.Dispose(); } catch { }
            }

            procs.Clear();
            senderProc = null;
            UpdateStatus();   // Start ON, Stop OFF, IO OFF
            AppendLog("[ui] stopped");
            OnStopVisual();
            UpdateLocks();  // reabilita ComboBox ao parar
        }

        async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (senderProc is null || !IsAlive(senderProc)) return;
            var msg = MsgInput.Text ?? "";
            if (string.IsNullOrWhiteSpace(msg)) return;
            try
            {
                await senderProc.StandardInput.WriteLineAsync(msg);
                OnSendVisual();
            }
            catch { }
            MsgInput.Clear();
        }

        void BtnClearLog_Click(object sender, RoutedEventArgs e) => LogList.Items.Clear();

        void UpdateButtons()
        {
            bool running = AnyRunning(procs);
            BtnStart.IsEnabled = !running;
            BtnStop.IsEnabled = running;
        }

        void SetIOEnabled(bool on)
        {
            MsgInput.IsEnabled = on;
            BtnSend.IsEnabled = on;
        
        }
        void UpdateLocks()
        {
            bool running = AnyRunning(procs);
            IpcSelector.IsEnabled = !running;         // trava ComboBox quando conectado
                                                      // Se quiser apenas bloquear clique sem “cinza”:
                                                      // IpcSelector.IsHitTestVisible = !running;
        }

    }
}
