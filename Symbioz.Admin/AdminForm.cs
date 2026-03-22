using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Symbioz.Admin
{
    public class AdminForm : Form
    {
        private const string DefaultHost     = "127.0.0.1";
        private const int    DefaultPort     = 9600;
        private const string DefaultPassword = "symbiozadmin";

        // ── Couleurs ──────────────────────────────────────────────────────
        private static readonly Color C_BG      = Color.FromArgb(10,  12,  16);
        private static readonly Color C_PANEL   = Color.FromArgb(18,  22,  30);
        private static readonly Color C_PANEL2  = Color.FromArgb(26,  31,  42);
        private static readonly Color C_BORDER  = Color.FromArgb(40,  48,  62);
        private static readonly Color C_TEXT    = Color.FromArgb(210, 218, 226);
        private static readonly Color C_MUTED   = Color.FromArgb(100, 110, 125);
        private static readonly Color C_ACCENT  = Color.FromArgb(56,  139, 253);
        private static readonly Color C_SUCCESS = Color.FromArgb(63,  185,  80);
        private static readonly Color C_ERROR   = Color.FromArgb(248,  81,  73);
        private static readonly Color C_WARNING = Color.FromArgb(210, 153,  34);
        private static readonly Color C_INGAME  = Color.FromArgb(22,  50,  90);
        private static readonly Color C_SERVER  = Color.FromArgb(22,  60,  35);

        // ── Fonts ─────────────────────────────────────────────────────────
        private static readonly Font F_NORMAL  = new Font("Segoe UI",  11f);
        private static readonly Font F_BOLD    = new Font("Segoe UI",  11f, FontStyle.Bold);
        private static readonly Font F_TITLE   = new Font("Segoe UI",  15f, FontStyle.Bold);
        private static readonly Font F_SMALL   = new Font("Segoe UI",  10f);
        private static readonly Font F_BTN     = new Font("Segoe UI",  10f, FontStyle.Bold);
        private static readonly Font F_LOG     = new Font("Consolas",  10.5f);
        private static readonly Font F_SECTION = new Font("Segoe UI",   9f, FontStyle.Bold);

        private RichTextBox _log;
        private TextBox     _input;
        private TextBox     _playerInput;
        private TextBox     _txtPassword;
        private Button      _btnConnect;
        private Label       _lblStatus;

        private TcpClient?    _tcp;
        private StreamWriter? _writer;
        private bool          _connected;

        public AdminForm()
        {
            Text            = "Symbioz — Admin Console";
            Size            = new Size(1920, 1080);
            MinimumSize     = new Size(1280, 720);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = C_BG;
            ForeColor       = C_TEXT;
            Font            = F_NORMAL;
            WindowState     = FormWindowState.Maximized;

            BuildUI();
            Load += (s, e) => AppendLog("Prêt — entrez le mot de passe et cliquez sur Connecter.", C_MUTED);
        }

        // ── UI ────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            var table = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = C_BG,
                Padding     = Padding.Empty,
                Margin      = Padding.Empty
            };
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // top bar
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // log
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));  // bottom
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            table.Controls.Add(BuildTopBar(), 0, 0);
            table.Controls.Add(BuildLog(),    0, 1);
            table.Controls.Add(BuildBottom(), 0, 2);

            Controls.Add(table);
        }

        // ── Top bar ───────────────────────────────────────────────────────

        private Panel BuildTopBar()
        {
            var bar = new Panel { Dock = DockStyle.Fill, BackColor = C_PANEL };
            bar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(C_BORDER, 2), 0, bar.Height - 1, bar.Width, bar.Height - 1);
                e.Graphics.DrawLine(new Pen(C_ACCENT,  3), 0, bar.Height - 1, 200, bar.Height - 1);
            };

            bar.Controls.Add(new Label
            {
                Text      = "⬡  SYMBIOZ ADMIN",
                Font      = F_TITLE,
                ForeColor = C_ACCENT,
                AutoSize  = true,
                Location  = new Point(24, 18)
            });

            // Séparateur vertical
            var sep = new Panel
            {
                Size      = new Size(2, 36),
                Location  = new Point(230, 18),
                BackColor = C_BORDER
            };
            bar.Controls.Add(sep);

            _lblStatus = new Label
            {
                Text      = "●  Déconnecté",
                Font      = F_BOLD,
                ForeColor = C_ERROR,
                AutoSize  = true,
                Location  = new Point(248, 22)
            };
            bar.Controls.Add(_lblStatus);

            // Champ mot de passe
            bar.Controls.Add(new Label
            {
                Text      = "MOT DE PASSE",
                Font      = F_SECTION,
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(460, 14)
            });

            _txtPassword = new TextBox
            {
                Text         = DefaultPassword,
                PasswordChar = '●',
                Size         = new Size(200, 32),
                Location     = new Point(460, 32),
                BackColor    = C_PANEL2,
                ForeColor    = C_TEXT,
                BorderStyle  = BorderStyle.FixedSingle,
                Font         = F_NORMAL
            };
            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) OnConnectClick(s, e); };
            bar.Controls.Add(_txtPassword);

            _btnConnect = MakeBtn("  Connecter", Color.FromArgb(25, 80, 25), new Size(160, 40));
            _btnConnect.Click += OnConnectClick;
            bar.Layout += (s, e) => _btnConnect.SetBounds(bar.Width - 184, 16, 160, 40);
            bar.Controls.Add(_btnConnect);

            return bar;
        }

        // ── Log ───────────────────────────────────────────────────────────

        private Panel BuildLog()
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(16, 12, 16, 0) };

            _log = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = C_BG,
                ForeColor   = C_TEXT,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Font        = F_LOG,
                Padding     = new Padding(4)
            };

            wrapper.Controls.Add(_log);
            return wrapper;
        }

        // ── Bottom section ────────────────────────────────────────────────

        private Panel BuildBottom()
        {
            var bottom = new Panel { Dock = DockStyle.Fill, BackColor = C_PANEL };
            bottom.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER, 2), 0, 0, bottom.Width, 0);

            // ── Ligne 1 : Joueur cible ────────────────────────────────────
            var lblJoueur = new Label
            {
                Text      = "JOUEUR CIBLE",
                Font      = F_SECTION,
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(24, 18)
            };
            bottom.Controls.Add(lblJoueur);

            _playerInput = new TextBox
            {
                Location    = new Point(24, 36),
                Size        = new Size(280, 34),
                BackColor   = C_PANEL2,
                ForeColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = F_NORMAL
            };
            bottom.Controls.Add(_playerInput);

            // ── Ligne 2 : Commandes in-game ───────────────────────────────
            bottom.Controls.Add(new Label
            {
                Text      = "COMMANDES IN-GAME",
                Font      = F_SECTION,
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(24, 88)
            });

            var gameFlow = new FlowLayoutPanel
            {
                Location     = new Point(24, 110),
                Size         = new Size(bottom.Width - 48, 48),
                BackColor    = C_PANEL,
                WrapContents = false,
                AutoSize     = false,
                Padding      = Padding.Empty,
                Margin       = Padding.Empty
            };
            bottom.Layout += (s, e) => gameFlow.Width = bottom.Width - 48;

            var gameCmds = new[]
            {
                "item", "level", "kamas", "spell", "go", "goto",
                "teleport", "kick", "ban", "mute", "monsters", "look", "map", "infos"
            };
            foreach (var cmd in gameCmds)
            {
                var c   = cmd;
                var btn = MakeQuickBtn(c, C_INGAME);
                btn.Margin = new Padding(0, 0, 8, 0);
                btn.Click += (s, e) =>
                {
                    string p = _playerInput.Text.Trim();
                    _input.Text = "cmd " + (p.Length > 0 ? p + " " : "") + c + " ";
                    _input.Focus();
                    _input.SelectionStart = _input.Text.Length;
                };
                gameFlow.Controls.Add(btn);
            }
            bottom.Controls.Add(gameFlow);

            // ── Ligne 3 : Commandes serveur ───────────────────────────────
            bottom.Controls.Add(new Label
            {
                Text      = "SERVEUR",
                Font      = F_SECTION,
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(24, 172)
            });

            var srvFlow = new FlowLayoutPanel
            {
                Location     = new Point(24, 194),
                Size         = new Size(600, 44),
                BackColor    = C_PANEL,
                WrapContents = false,
                AutoSize     = false,
                Padding      = Padding.Empty,
                Margin       = Padding.Empty
            };
            var srvCmds = new[] { "help", "save", "backup", "reboot" };
            foreach (var cmd in srvCmds)
            {
                var c   = cmd;
                var btn = MakeQuickBtn(c, C_SERVER);
                btn.Margin = new Padding(0, 0, 8, 0);
                btn.Click += (s, e) => { _input.Text = c + " "; _input.Focus(); _input.SelectionStart = _input.Text.Length; };
                srvFlow.Controls.Add(btn);
            }
            bottom.Controls.Add(srvFlow);

            // ── Barre de saisie ───────────────────────────────────────────
            var inputBar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = C_PANEL
            };
            inputBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER, 1), 0, 0, inputBar.Width, 0);

            _input = new TextBox
            {
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height      = 36,
                BackColor   = C_PANEL2,
                ForeColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = F_NORMAL
            };
            _input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendCommand(); } };

            var btnSend = MakeBtn("  Envoyer  ▶", C_ACCENT, new Size(160, 38));
            btnSend.Click += (s, e) => SendCommand();

            inputBar.Controls.Add(_input);
            inputBar.Controls.Add(btnSend);
            inputBar.Layout += (s, e) =>
            {
                btnSend.SetBounds(inputBar.Width - 180, 11, 160, 38);
                _input.SetBounds(24, 12, inputBar.Width - 216, 36);
            };

            bottom.Controls.Add(inputBar);
            return bottom;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static Button MakeQuickBtn(string text, Color back)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(130, 40),
                BackColor = back,
                ForeColor = C_TEXT,
                FlatStyle = FlatStyle.Flat,
                Font      = F_BTN,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = C_BORDER;
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.2f);
            return btn;
        }

        private static Button MakeBtn(string text, Color back, Size? size = null)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = size ?? new Size(140, 40),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = F_BTN,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.2f);
            return btn;
        }

        // ── Connection ────────────────────────────────────────────────────

        private void OnConnectClick(object? sender, EventArgs e)
        {
            if (_connected) Disconnect();
            else ConnectAsync();
        }

        private void ConnectAsync()
        {
            _btnConnect.Enabled  = false;
            _txtPassword.Enabled = false;
            AppendLog("Connexion en cours...", C_MUTED);

            string password = _txtPassword.Text;

            new Thread(() =>
            {
                TcpClient? tcp = null;
                try
                {
                    tcp = new TcpClient();
                    tcp.Connect(DefaultHost, DefaultPort);

                    var stream = tcp.GetStream();
                    var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                    var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

                    writer.WriteLine(password);

                    string? response = reader.ReadLine();
                    if (response != "AUTH_OK")
                    {
                        _log.BeginInvoke(() =>
                        {
                            AppendLog("Mot de passe incorrect.", C_ERROR);
                            _btnConnect.Enabled  = true;
                            _txtPassword.Enabled = true;
                        });
                        tcp.Close();
                        return;
                    }

                    _tcp       = tcp;
                    _writer    = writer;
                    _connected = true;
                    new Thread(() => ReadLoop(reader)) { IsBackground = true }.Start();
                    _log.BeginInvoke(() =>
                    {
                        AppendLog("Connecté au serveur World.", C_SUCCESS);
                        SetStatus(true);
                        _btnConnect.Enabled  = true;
                        _txtPassword.Enabled = false;
                    });
                }
                catch (Exception ex)
                {
                    tcp?.Close();
                    _log.BeginInvoke(() =>
                    {
                        AppendLog("Connexion échouée : " + ex.Message, C_ERROR);
                        AppendLog("Assurez-vous que le serveur World est démarré.", C_MUTED);
                        _btnConnect.Enabled  = true;
                        _txtPassword.Enabled = true;
                    });
                }
            }) { IsBackground = true }.Start();
        }

        private void ReadLoop(StreamReader reader)
        {
            try
            {
                string? line;
                while (_connected && (line = reader.ReadLine()) != null)
                {
                    var l = line;
                    if (l.Contains("[OK]"))
                        AppendLog(l, C_SUCCESS);
                    else if (l.Contains("[FAIL]"))
                        AppendLog(l, C_ERROR);
                }
            }
            catch { }
            finally { if (_connected) SafeInvoke(Disconnect); }
        }

        private void Disconnect()
        {
            _writer?.Dispose();
            _tcp?.Close();
            _writer    = null;
            _tcp       = null;
            _connected = false;
            SetStatus(false);
            _txtPassword.Enabled = true;
            AppendLog("Déconnecté.", C_MUTED);
        }

        private void SendCommand()
        {
            string cmd = _input.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            if (!_connected) { AppendLog("Non connecté.", C_ERROR); return; }

            try
            {
                _writer!.WriteLine(cmd);
                AppendLog("> " + cmd, C_ACCENT);
                _input.Clear();
            }
            catch (Exception ex)
            {
                AppendLog("Erreur : " + ex.Message, C_ERROR);
                Disconnect();
            }
        }

        private void SetStatus(bool connected)
        {
            SafeInvoke(() =>
            {
                _lblStatus.Text       = connected ? "●  Connecté" : "●  Déconnecté";
                _lblStatus.ForeColor  = connected ? C_SUCCESS : C_ERROR;
                _btnConnect.Text      = connected ? "  Déconnecter" : "  Connecter";
                _btnConnect.BackColor = connected
                    ? Color.FromArgb(90, 25, 25)
                    : Color.FromArgb(25, 80, 25);
            });
        }

        private void AppendLog(string message, Color color)
        {
            SafeInvoke(() =>
            {
                _log.SelectionStart  = _log.TextLength;
                _log.SelectionLength = 0;
                _log.SelectionColor  = C_MUTED;
                _log.AppendText($"[{DateTime.Now:HH:mm:ss}]  ");
                _log.SelectionColor  = color;
                _log.AppendText(message + "\n");
                _log.ScrollToCaret();
            });
        }

        private void SafeInvoke(Action a)
        {
            if (_log.InvokeRequired) _log.BeginInvoke(a);
            else a();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_connected) Disconnect();
            base.OnFormClosing(e);
        }
    }
}
