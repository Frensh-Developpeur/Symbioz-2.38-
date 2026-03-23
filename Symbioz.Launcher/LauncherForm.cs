using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Symbioz.Launcher
{
    /// <summary>
    /// Fenêtre principale du launcher Symbioz.
    /// Affiche deux panneaux côte à côte (Auth / World) avec console embarquée,
    /// et démarre automatiquement les deux serveurs à l'ouverture.
    /// </summary>
    public class LauncherForm : Form
    {
        // Répertoire de base de l'exécutable du launcher
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Panneaux des deux serveurs
        private readonly ConsolePanel _auth;
        private readonly ConsolePanel _world;

        public LauncherForm()
        {
            Text = "Symbioz Launcher";
            Size = new Size(1100, 680);
            MinimumSize = new Size(700, 420);
            BackColor = Color.FromArgb(12, 12, 20);
            ForeColor = Color.Silver;
            Font = new Font("Consolas", 9f);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;

            // ── Barre du haut : titre + sous-titre ───────────────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(16, 16, 30)
            };
            // Liseré coloré en bas de la barre
            var topAccent = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = Color.FromArgb(40, 80, 160)
            };
            var titleLabel = new Label
            {
                Text = "◈  SYMBIOZ LAUNCHER",
                Font = new Font("Consolas", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(110, 180, 255),
                AutoSize = true,
                Location = new Point(16, 10)
            };
            var subLabel = new Label
            {
                Text = "v2.38  ·  .NET Core 6  ·  Auth Server  +  World Server",
                Font = new Font("Consolas", 7.5f),
                ForeColor = Color.FromArgb(55, 65, 100),
                AutoSize = true,
                Location = new Point(18, 38)
            };
            topBar.Controls.Add(topAccent);
            topBar.Controls.Add(titleLabel);
            topBar.Controls.Add(subLabel);

            // ── Barre du bas : boutons globaux ────────────────────────────────────
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(10, 10, 18)
            };
            // Liseré en haut de la barre
            var bottomAccent = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(30, 30, 55)
            };

            var btnStartAll = MakeButton("▶  Lancer tout",  Color.FromArgb(15, 80, 15));
            var btnStopAll  = MakeButton("■  Stopper tout", Color.FromArgb(100, 15, 15));
            var btnClear    = MakeButton("⌫  Vider logs",   Color.FromArgb(30, 30, 55));
            btnStartAll.Location = new Point(16, 9);
            btnStopAll.Location  = new Point(196, 9);
            btnClear.Location    = new Point(376, 9);

            // Les boutons globaux agissent sur les deux serveurs simultanément
            btnStartAll.Click += (s, e) => { _auth.Start();    _world.Start(); };
            btnStopAll.Click  += (s, e) => { _auth.Stop();     _world.Stop(); };
            btnClear.Click    += (s, e) => { _auth.ClearLog(); _world.ClearLog(); };

            bottomBar.Controls.Add(bottomAccent);
            bottomBar.Controls.Add(btnStartAll);
            bottomBar.Controls.Add(btnStopAll);
            bottomBar.Controls.Add(btnClear);

            // ── SplitContainer : panneau Auth à gauche, World à droite ───────────
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 20),
                BorderStyle = BorderStyle.None,
                SplitterWidth = 6   // séparateur glissable
            };

            // On applique les MinSize et le démarrage auto dans Load,
            // car la taille réelle du contrôle n'est connue qu'après affichage
            Load += (s, e) =>
            {
                split.Panel1MinSize = 250;
                split.Panel2MinSize = 250;
                split.SplitterDistance = split.Width / 2; // séparateur centré

                // Démarrage automatique des deux serveurs à l'ouverture
                _auth.Start();
                _world.Start();
            };
            split.Panel1.BackColor = Color.FromArgb(12, 12, 20);
            split.Panel2.BackColor = Color.FromArgb(12, 12, 20);

            _auth  = new ConsolePanel("AUTH SERVER",  ResolveExe("Symbioz.Auth.exe"),  Color.FromArgb(80, 160, 255));
            _world = new ConsolePanel("WORLD SERVER", ResolveExe("Symbioz.World.exe"), Color.FromArgb(80, 220, 130));
            _auth.Dock  = DockStyle.Fill;
            _world.Dock = DockStyle.Fill;

            split.Panel1.Controls.Add(_auth);
            split.Panel2.Controls.Add(_world);

            // IMPORTANT : le contrôle Fill doit être ajouté AVANT les contrôles dockés
            // (Top/Bottom), sinon WinForms les empile dans le mauvais ordre
            Controls.Add(split);
            Controls.Add(bottomBar);
            Controls.Add(topBar);

            // Arrêt propre des processus à la fermeture de la fenêtre
            FormClosing += (s, e) => { _auth.Stop(); _world.Stop(); };
        }

        /// <summary>
        /// Résout le chemin vers l'exécutable d'un projet frère.
        /// Cherche d'abord dans net6.0-windows puis net6.0,
        /// et en fallback dans le même dossier que le launcher.
        /// </summary>
        private static string ResolveExe(string exeName)
        {
            string project = Path.GetFileNameWithoutExtension(exeName);
            // Remonte de 4 niveaux depuis bin/Debug/net6.0-windows/ pour atteindre la racine de la solution
            string root = Path.GetFullPath(Path.Combine(BaseDir, "..", "..", "..", ".."));

            foreach (var fw in new[] { "net6.0-windows", "net6.0" })
            {
                string candidate = Path.Combine(root, project, "bin", "Debug", fw, exeName);
                if (File.Exists(candidate)) return candidate;
            }

            // Fallback : même dossier que le launcher
            return Path.Combine(BaseDir, exeName);
        }

        /// <summary>
        /// Crée un bouton stylé avec fond coloré et pas de bordure.
        /// </summary>
        private static Button MakeButton(string text, Color back)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(164, 34),
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9f)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.3f);
            return btn;
        }
    }

    /// <summary>
    /// Panneau représentant un serveur : header avec statut + boutons,
    /// et une RichTextBox embarquée qui affiche stdout/stderr du process en temps réel.
    /// </summary>
    public class ConsolePanel : Panel
    {
        private readonly string _exe;           // Chemin vers l'exécutable du serveur
        private Process? _process;              // Process en cours d'exécution (null si arrêté)
        private readonly Color _accent;         // Couleur thématique du panneau

        // Contrôles du header
        private readonly Label _dot;            // Indicateur coloré (gris = arrêté, couleur = actif)
        private readonly Label _statusLabel;    // Texte "En cours" / "Arrêté"
        private readonly RichTextBox _console;  // Zone d'affichage des logs
        private readonly Button _btnStart;
        private readonly Button _btnStop;
        private readonly TextBox _input;        // Zone de saisie des commandes
        private readonly Button _btnSend;       // Bouton d'envoi

        public ConsolePanel(string serverName, string exe, Color accent)
        {
            _exe    = exe;
            _accent = accent;
            BackColor = Color.FromArgb(14, 14, 24);
            Padding = new Padding(0);

            // ── Header ───────────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(20, 20, 36)
            };

            // Indicateur de statut (●) : gris par défaut, prend la couleur accent quand actif
            _dot = new Label
            {
                Text = "●",
                ForeColor = Color.FromArgb(45, 45, 65),
                Font = new Font("Consolas", 12f),
                AutoSize = true,
                Location = new Point(12, 11)
            };

            // Nom du serveur (ex: "AUTH SERVER") — positionné à droite par PositionHeaderControls
            var nameLabel = new Label
            {
                Text = serverName,
                ForeColor = accent,
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(34, 12)
            };

            // Texte de statut affiché sous le dot
            _statusLabel = new Label
            {
                ForeColor = Color.FromArgb(60, 60, 85),
                Font = new Font("Consolas", 8f),
                AutoSize = true,
                Location = new Point(34, 30)
            };

            _btnStop = new Button
            {
                Text = "■ Stop",
                Size = new Size(84, 30),
                BackColor = Color.FromArgb(90, 16, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 8.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false // désactivé tant que le process ne tourne pas
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Click += (s, e) => Stop();

            _btnStart = new Button
            {
                Text = "▶ Start",
                Size = new Size(84, 30),
                BackColor = Color.FromArgb(16, 75, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 8.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += (s, e) => Start();

            header.Controls.Add(_dot);
            header.Controls.Add(_statusLabel);
            header.Controls.Add(nameLabel);
            header.Controls.Add(_btnStart);
            header.Controls.Add(_btnStop);

            // Positionnement initial + recalcul à chaque redimensionnement du header
            PositionHeaderControls(header, nameLabel);
            header.Resize += (s, e) => PositionHeaderControls(header, nameLabel);

            // ── Liseré coloré sous le header ─────────────────────────────────────
            var accentLine = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = accent
            };

            // ── Console (RichTextBox) ─────────────────────────────────────────────
            // Affiche stdout (vert clair) et stderr (rouge) du serveur en temps réel
            _console = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(7, 7, 12),
                ForeColor = Color.FromArgb(155, 205, 155),
                Font = new Font("Consolas", 8.5f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };

            // ── Barre de saisie de commandes ──────────────────────────────────────
            var inputBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(10, 10, 18)
            };

            _input = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 32),
                ForeColor = Color.FromArgb(180, 220, 180),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                PlaceholderText = "  Entrez une commande...",
                Enabled = false  // activé uniquement quand le serveur tourne
            };
            // Envoi sur touche Entrée
            _input.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendCommand(); }
            };

            _btnSend = new Button
            {
                Text = "↵",
                Dock = DockStyle.Right,
                Width = 40,
                BackColor = Color.FromArgb(30, 30, 55),
                ForeColor = Color.FromArgb(110, 180, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 11f),
                Enabled = false
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += (s, e) => SendCommand();

            inputBar.Controls.Add(_input);
            inputBar.Controls.Add(_btnSend);

            // IMPORTANT : Fill en premier, puis les Top dans l'ordre inverse d'affichage
            Controls.Add(_console);
            Controls.Add(inputBar);
            Controls.Add(accentLine);
            Controls.Add(header);

            // Message d'init : confirme si l'exe est trouvé ou non
            if (File.Exists(exe))
                AppendLine($"  [{Now()}] Prêt  ·  {Path.GetFileName(exe)}", Color.FromArgb(45, 75, 45));
            else
                AppendLine($"  [!!] Introuvable : {exe}", Color.OrangeRed);
        }

        /// <summary>
        /// Place les boutons à droite et le nom du serveur juste à leur gauche.
        /// Appelé à l'init et à chaque Resize du header.
        /// </summary>
        private void PositionHeaderControls(Panel header, Label nameLabel)
        {
            _btnStop.Location  = new Point(header.Width - _btnStop.Width - 10, 8);
            _btnStart.Location = new Point(header.Width - _btnStop.Width - _btnStart.Width - 18, 8);
            nameLabel.Location = new Point(header.Width - _btnStop.Width - _btnStart.Width - nameLabel.Width - 28, 12);
        }

        /// <summary>
        /// Démarre le serveur : crée un Process avec redirection stdout/stderr vers la console embarquée.
        /// </summary>
        public void Start()
        {
            // Ne rien faire si déjà en cours
            if (_process != null && !_process.HasExited) return;

            if (!File.Exists(_exe))
            {
                AppendLine($"  [ERREUR] Fichier introuvable : {_exe}", Color.OrangeRed);
                return;
            }

            AppendLine($"\n  [{Now()}] ▶ Démarrage...\n", Color.FromArgb(70, 130, 70));

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _exe,
                    WorkingDirectory = Path.GetDirectoryName(_exe) ?? BaseDir,
                    UseShellExecute = false,        // obligatoire pour la redirection
                    RedirectStandardInput  = true,  // stdin  ← zone de saisie
                    RedirectStandardOutput = true,  // stdout → console embarquée (vert)
                    RedirectStandardError  = true,  // stderr → console embarquée (rouge)
                    CreateNoWindow = true,           // pas de fenêtre console externe
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding  = System.Text.Encoding.UTF8
                },
                EnableRaisingEvents = true  // nécessaire pour l'événement Exited
            };

            // Chaque ligne stdout s'affiche en vert clair
            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) AppendLine("  " + e.Data, Color.FromArgb(165, 210, 165));
            };
            // Chaque ligne stderr s'affiche en rouge
            _process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) AppendLine("  " + e.Data, Color.FromArgb(255, 105, 105));
            };
            // Quand le process se termine (crash ou arrêt normal)
            _process.Exited += (s, e) =>
            {
                var code = (s as Process)?.ExitCode;
                AppendLine($"\n  [{Now()}] ■ Processus terminé  (exit {code})\n", Color.FromArgb(195, 135, 40));
                SafeInvoke(SetStopped);
            };

            _process.Start();
            _process.BeginOutputReadLine(); // démarre la lecture asynchrone de stdout
            _process.BeginErrorReadLine();  // démarre la lecture asynchrone de stderr
            SetRunning();
        }

        /// <summary>
        /// Arrête le serveur et tout son arbre de processus fils.
        /// </summary>
        public void Stop()
        {
            if (_process == null || _process.HasExited) return;
            try { _process.Kill(entireProcessTree: true); } catch { }
            AppendLine($"\n  [{Now()}] ■ Arrêté manuellement\n", Color.FromArgb(195, 135, 40));
            SetStopped();
        }

        /// <summary>Vide la console embarquée.</summary>
        public void ClearLog() => SafeInvoke(() => _console.Clear());

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;
        private static string Now()   => DateTime.Now.ToString("HH:mm:ss");

        /// <summary>
        /// Ajoute une ligne colorée dans la RichTextBox.
        /// Thread-safe via SafeInvoke.
        /// </summary>
        private void AppendLine(string text, Color color)
        {
            SafeInvoke(() =>
            {
                int start = _console.TextLength;
                _console.AppendText(text + "\n");
                _console.Select(start, text.Length);
                _console.SelectionColor = color;
                _console.Select(_console.TextLength, 0); // désélectionne
                _console.ScrollToCaret();
            });
        }

        /// <summary>
        /// Envoie la commande saisie dans le stdin du process et l'affiche dans la console.
        /// </summary>
        private void SendCommand()
        {
            if (_process == null || _process.HasExited) return;
            string cmd = _input.Text.Trim();
            if (cmd.Length == 0) return;

            AppendLine($"  > {cmd}", Color.FromArgb(110, 180, 255));
            _process.StandardInput.WriteLine(cmd);
            SafeInvoke(() => _input.Clear());
        }

        /// <summary>Passe le panneau en état "En cours" : dot coloré, boutons inversés.</summary>
        private void SetRunning()
        {
            SafeInvoke(() =>
            {
                _dot.ForeColor         = _accent;
                _statusLabel.ForeColor = _accent;
                _btnStart.Enabled      = false;
                _btnStop.Enabled       = true;
                _input.Enabled         = true;
                _btnSend.Enabled       = true;
            });
        }

        /// <summary>Passe le panneau en état "Arrêté" : dot gris, boutons inversés.</summary>
        private void SetStopped()
        {
            SafeInvoke(() =>
            {
                _dot.ForeColor         = Color.FromArgb(45, 45, 65);
                _statusLabel.ForeColor = Color.FromArgb(60, 60, 85);
                _btnStart.Enabled      = true;
                _btnStop.Enabled       = false;
                _input.Enabled         = false;
                _btnSend.Enabled       = false;
            });
        }

        /// <summary>
        /// Exécute une action sur le thread UI.
        /// Ignore silencieusement si le contrôle est déjà détruit.
        /// </summary>
        private void SafeInvoke(Action a)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { if (InvokeRequired) Invoke(a); else a(); }
            catch (ObjectDisposedException) { }
        }
    }
}
