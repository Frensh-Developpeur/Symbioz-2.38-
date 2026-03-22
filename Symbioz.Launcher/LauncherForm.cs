using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Symbioz.Launcher
{
    public class LauncherForm : Form
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        private readonly ServerPanel _auth;
        private readonly ServerPanel _world;
        private readonly ServerPanel _admin;

        private Button _btnStartAll;
        private Button _btnStopAll;

        public LauncherForm()
        {
            Text = "Symbioz Launcher";
            Size = new Size(480, 340);
            MinimumSize = new Size(480, 340);
            MaximumSize = new Size(480, 340);
            BackColor = Color.FromArgb(20, 20, 30);
            ForeColor = Color.Silver;
            Font = new Font("Consolas", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Title label
            var title = new Label
            {
                Text = "Symbioz — Launcher",
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(16, 14)
            };
            Controls.Add(title);

            // Server panels
            _auth  = new ServerPanel("Auth Server",  ResolveExe("Symbioz.Auth.exe"),  new Point(16, 56));
            _world = new ServerPanel("World Server", ResolveExe("Symbioz.World.exe"), new Point(16, 130));
            _admin = new ServerPanel("Admin Console",ResolveExe("Symbioz.Admin.exe"), new Point(16, 204));

            Controls.Add(_auth);
            Controls.Add(_world);
            Controls.Add(_admin);

            // Bottom buttons
            _btnStartAll = MakeButton("▶  Lancer tout", Color.FromArgb(30, 100, 30), new Point(16, 272));
            _btnStopAll  = MakeButton("■  Stopper tout", Color.FromArgb(120, 30, 30), new Point(196, 272));
            _btnStartAll.Click += (s, e) => { _auth.Start(); _world.Start(); _admin.Start(); };
            _btnStopAll.Click  += (s, e) => { _auth.Stop(); _world.Stop(); _admin.Stop(); };

            Controls.Add(_btnStartAll);
            Controls.Add(_btnStopAll);

            FormClosing += (s, e) => { _auth.Stop(); _world.Stop(); _admin.Stop(); };
        }

        private static string ResolveExe(string exeName)
        {
            string project = Path.GetFileNameWithoutExtension(exeName);
            string root = Path.GetFullPath(Path.Combine(BaseDir, "..", "..", "..", ".."));

            // Try net6.0-windows then net6.0 (Admin is windows, Auth/World are not)
            foreach (var fw in new[] { "net6.0-windows", "net6.0" })
            {
                string candidate = Path.Combine(root, project, "bin", "Debug", fw, exeName);
                if (File.Exists(candidate)) return candidate;
            }

            // Fallback: same folder as launcher
            return Path.Combine(BaseDir, exeName);
        }

        private Button MakeButton(string text, Color back, Point loc)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(164, 34),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9f)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }

    // ─── One row per server ────────────────────────────────────────────────────
    public class ServerPanel : Panel
    {
        private readonly string _exe;
        private Process? _process;

        private readonly Label _dot;
        private readonly Label _name;
        private readonly Button _btnStart;
        private readonly Button _btnStop;

        public ServerPanel(string label, string exe, Point location)
        {
            _exe = exe;
            Location = location;
            Size = new Size(440, 64);
            BackColor = Color.FromArgb(28, 28, 42);

            _dot = new Label
            {
                Text = "●",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _name = new Label
            {
                Text = label,
                ForeColor = Color.Silver,
                AutoSize = true,
                Location = new Point(28, 10)
            };

            var path = new Label
            {
                Text = File.Exists(exe) ? Path.GetFileName(exe) : "⚠ introuvable",
                ForeColor = File.Exists(exe) ? Color.FromArgb(80, 80, 120) : Color.OrangeRed,
                AutoSize = true,
                Location = new Point(28, 30),
                Font = new Font("Consolas", 7.5f)
            };

            _btnStart = MakeBtn("▶", Color.FromArgb(30, 100, 30), new Point(330, 8));
            _btnStop  = MakeBtn("■", Color.FromArgb(120, 30, 30),  new Point(390, 8));
            _btnStop.Enabled = false;

            _btnStart.Click += (s, e) => Start();
            _btnStop.Click  += (s, e) => Stop();

            Controls.Add(_dot);
            Controls.Add(_name);
            Controls.Add(path);
            Controls.Add(_btnStart);
            Controls.Add(_btnStop);
        }

        public void Start()
        {
            if (_process != null && !_process.HasExited) return;
            if (!File.Exists(_exe)) return;

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _exe,
                    WorkingDirectory = Path.GetDirectoryName(_exe),
                    UseShellExecute = true
                },
                EnableRaisingEvents = true
            };
            _process.Exited += (s, e) => SafeInvoke(SetStopped);
            _process.Start();
            SetRunning();
        }

        public void Stop()
        {
            if (_process == null || _process.HasExited) return;
            try { _process.Kill(entireProcessTree: true); } catch { }
            SetStopped();
        }

        private void SetRunning()
        {
            SafeInvoke(() =>
            {
                _dot.ForeColor = Color.LimeGreen;
                _btnStart.Enabled = false;
                _btnStop.Enabled = true;
            });
        }

        private void SetStopped()
        {
            SafeInvoke(() =>
            {
                _dot.ForeColor = Color.Gray;
                _btnStart.Enabled = true;
                _btnStop.Enabled = false;
            });
        }

        private void SafeInvoke(Action a)
        {
            if (InvokeRequired) Invoke(a);
            else a();
        }

        private static Button MakeBtn(string text, Color back, Point loc)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(40, 46),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 11f)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}
