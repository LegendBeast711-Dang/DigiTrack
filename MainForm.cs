using DigiTrack.Forms;
using DigiTrack.Helpers;
using DigiTrack.Models;

namespace DigiTrack;

public class MainForm : Form
{
    // ── Controls ──────────────────────────────────────────────────────────────
    private Button _btnRecord = null!;
    private Label _lblStatus = null!;
    private Label _lblWords = null!;
    private Label _lblChars = null!;
    private Label _lblWpm = null!;
    private Label _lblTime = null!;
    private RichTextBox _rtbText = null!;
    private TextBox _txtSearch = null!;
    private Label _lblStatusBar = null!;
    private Label _lblNotification = null!;
    private Button _btnTheme = null!;
    private CheckBox _chkAutoSave = null!;
    private CheckBox _chkEncrypt = null!;
    private CheckBox _chkGlobalHook = null!;
    private Panel _pnlLeft = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _isRecording;
    private bool _isDarkMode = true;
    private TypingSession _currentSession = new();
    private GlobalKeyboardHook? _globalHook;

    // ── Tray ──────────────────────────────────────────────────────────────────
    private NotifyIcon _notifyIcon = null!;
    private Icon _trayIconIdle = null!;
    private Icon _trayIconRecording = null!;

    // ── Timers ────────────────────────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _statsTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _autoSaveTimer = new() { Interval = 60_000 };
    private readonly System.Windows.Forms.Timer _notifTimer = new() { Interval = 3000 };
    private readonly System.Windows.Forms.Timer _wpmSnapshotTimer = new() { Interval = 10_000 };

    // ── Search ────────────────────────────────────────────────────────────────
    private int _searchStart;

    // ── Theme colors ──────────────────────────────────────────────────────────
    private Color BgColor => _isDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(240, 240, 245);
    private Color PanelColor => _isDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(255, 255, 255);
    private Color FgColor => _isDarkMode ? Color.FromArgb(220, 220, 220) : Color.FromArgb(30, 30, 30);
    private Color AccentColor => Color.FromArgb(0, 122, 204);
    private Color SubtleColor => _isDarkMode ? Color.FromArgb(130, 130, 130) : Color.FromArgb(120, 120, 120);
    private Color TextBoxBg => _isDarkMode ? Color.FromArgb(37, 37, 38) : Color.White;
    private Color RecordActiveColor => Color.FromArgb(196, 43, 28);
    private Color RecordIdleColor => Color.FromArgb(16, 124, 16);

    // ─────────────────────────────────────────────────────────────────────────

    public MainForm()
    {
        InitializeComponent();
        WireTimers();
        SetupTrayIcon();
        ApplyTheme();
        ShowNotification("Welcome to DigiTrack! Press Start Recording to begin.", 4000);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        Text = "DigiTrack – Smart Typing Recorder & Analyzer";
        Size = new Size(980, 680);
        MinimumSize = new Size(780, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // ── Left sidebar ──────────────────────────────────────────────────────
        _pnlLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 195,
            Padding = new Padding(10, 12, 10, 10)
        };

        // App title
        var lblTitle = new Label
        {
            Text = "DigiTrack",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = AccentColor,
            AutoSize = false,
            Height = 32,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSubtitle = new Label
        {
            Text = "Smart Typing Recorder",
            Font = new Font("Segoe UI", 7.5f),
            AutoSize = false,
            Height = 18,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var sep0 = MakeSeparator();

        // Record button
        _btnRecord = new Button
        {
            Text = "▶  Start Recording",
            Height = 44,
            Dock = DockStyle.Top,
            BackColor = RecordIdleColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnRecord.FlatAppearance.BorderSize = 0;
        _btnRecord.Click += (_, _) => ToggleRecording();

        _lblStatus = MakeLabel("● Idle", new Font("Segoe UI", 8.5f));

        var sep1 = MakeSeparator();

        // Stats labels
        var lblStatsHeader = MakeLabel("SESSION STATS", new Font("Segoe UI", 7.5f, FontStyle.Bold));
        _lblWords = MakeLabel("Words:       0", new Font("Consolas", 9f));
        _lblChars = MakeLabel("Characters:  0", new Font("Consolas", 9f));
        _lblWpm = MakeLabel("WPM:         0.0", new Font("Consolas", 9f));
        _lblTime = MakeLabel("Time:        00:00", new Font("Consolas", 9f));

        var sep2 = MakeSeparator();

        // Action buttons
        var lblActionsHeader = MakeLabel("ACTIONS", new Font("Segoe UI", 7.5f, FontStyle.Bold));

        var btnSave = MakeSideButton("💾  Save Session", Color.FromArgb(0, 99, 177));
        btnSave.Click += (_, _) => SaveSession();

        var btnLoad = MakeSideButton("📂  Load File", Color.FromArgb(70, 70, 80));
        btnLoad.Click += (_, _) => LoadFile();

        var btnHistory = MakeSideButton("📋  Session History", Color.FromArgb(70, 70, 80));
        btnHistory.Click += (_, _) => OpenHistory();

        var btnAnalytics = MakeSideButton("📊  Analytics", Color.FromArgb(70, 70, 80));
        btnAnalytics.Click += (_, _) => OpenAnalytics();

        var btnClear = MakeSideButton("🗑  Clear Text", Color.FromArgb(120, 60, 60));
        btnClear.Click += (_, _) => ClearText();

        var sep3 = MakeSeparator();

        // Options
        var lblOptionsHeader = MakeLabel("OPTIONS", new Font("Segoe UI", 7.5f, FontStyle.Bold));

        _chkAutoSave = MakeCheckbox("Auto-Save (1 min)");
        _chkAutoSave.CheckedChanged += (_, _) => _autoSaveTimer.Enabled = _chkAutoSave.Checked;

        _chkEncrypt = MakeCheckbox("Encrypt on Save");

        _chkGlobalHook = MakeCheckbox("Global Typing Mode");
        _chkGlobalHook.CheckedChanged += (_, _) => ToggleGlobalHook();

        _btnTheme = MakeSideButton("☀  Light Mode", Color.FromArgb(80, 80, 90));
        _btnTheme.Click += (_, _) => ToggleTheme();

        // Stack controls top-down (reversed order for Dock=Top stacking)
        Control[] leftControls =
        {
            _btnTheme,
            _chkGlobalHook,
            _chkEncrypt,
            _chkAutoSave,
            lblOptionsHeader,
            sep3,
            btnClear,
            btnAnalytics,
            btnHistory,
            btnLoad,
            btnSave,
            lblActionsHeader,
            sep2,
            _lblTime,
            _lblWpm,
            _lblChars,
            _lblWords,
            lblStatsHeader,
            sep1,
            _lblStatus,
            _btnRecord,
            sep0,
            lblSubtitle,
            lblTitle
        };

        foreach (var c in leftControls)
            _pnlLeft.Controls.Add(c);

        // ── Right area ────────────────────────────────────────────────────────
        var pnlRight = new Panel { Dock = DockStyle.Fill };

        // Notification bar (top)
        _lblNotification = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.White,
            BackColor = AccentColor,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Padding = new Padding(4)
        };

        // Main text area
        _rtbText = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 12f),
            AcceptsTab = true,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(8)
        };
        _rtbText.TextChanged += RtbText_TextChanged;
        _rtbText.KeyDown += RtbText_KeyDown;

        // Bottom search + status bar
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Padding = new Padding(6, 4, 6, 4)
        };

        var lblSearch = new Label
        {
            Text = "🔍",
            AutoSize = true,
            Font = new Font("Segoe UI", 10f),
            Location = new Point(4, 7)
        };

        _txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 9f),
            Width = 200,
            Location = new Point(28, 6),
            PlaceholderText = "Search in text..."
        };
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; FindNext(); }
            if (e.KeyCode == Keys.Escape) ClearSearch();
        };

        var btnFind = new Button
        {
            Text = "Find",
            Width = 60,
            Height = 24,
            Location = new Point(235, 6),
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f)
        };
        btnFind.FlatAppearance.BorderSize = 0;
        btnFind.Click += (_, _) => FindNext();

        var btnClearSearch = new Button
        {
            Text = "✕",
            Width = 28,
            Height = 24,
            Location = new Point(300, 6),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f)
        };
        btnClearSearch.FlatAppearance.BorderSize = 0;
        btnClearSearch.Click += (_, _) => ClearSearch();

        _lblStatusBar = new Label
        {
            AutoSize = true,
            Location = new Point(340, 9),
            Font = new Font("Segoe UI", 8.5f),
            Text = "Ready"
        };

        pnlBottom.Controls.AddRange(new Control[]
            { lblSearch, _txtSearch, btnFind, btnClearSearch, _lblStatusBar });

        // Splitter line between text and bottom
        var pnlSep = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1
        };

        pnlRight.Controls.Add(_rtbText);
        pnlRight.Controls.Add(_lblNotification);
        pnlRight.Controls.Add(pnlSep);
        pnlRight.Controls.Add(pnlBottom);

        // Vertical divider between left/right
        var divider = new Panel
        {
            Dock = DockStyle.Left,
            Width = 1
        };

        Controls.Add(pnlRight);
        Controls.Add(divider);
        Controls.Add(_pnlLeft);

        FormClosing += MainForm_Closing;
        Resize += MainForm_Resize;
    }

    // ── Timer wiring ──────────────────────────────────────────────────────────

    private void WireTimers()
    {
        _statsTimer.Tick += (_, _) => UpdateStats();
        _autoSaveTimer.Tick += (_, _) => AutoSave();
        _wpmSnapshotTimer.Tick += (_, _) => TakeWpmSnapshot();

        _notifTimer.Tick += (_, _) =>
        {
            _notifTimer.Stop();
            _lblNotification.Visible = false;
        };
    }

    // ── Tray Icon ─────────────────────────────────────────────────────────────

    private void SetupTrayIcon()
    {
        _trayIconIdle = MakeTrayIcon(Color.FromArgb(16, 124, 16));
        _trayIconRecording = MakeTrayIcon(Color.FromArgb(196, 43, 28));

        var menu = new ContextMenuStrip();
        var miShow = new ToolStripMenuItem("Show DigiTrack");
        miShow.Font = new Font(miShow.Font, FontStyle.Bold);
        miShow.Click += (_, _) => RestoreFromTray();

        var miToggle = new ToolStripMenuItem("Start / Stop Recording");
        miToggle.Click += (_, _) => { RestoreFromTray(); ToggleRecording(); };

        var miExit = new ToolStripMenuItem("Exit");
        miExit.Click += (_, _) => { _notifyIcon.Visible = false; Application.Exit(); };

        menu.Items.Add(miShow);
        menu.Items.Add(miToggle);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miExit);

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIconIdle,
            Text = "DigiTrack – Idle",
            ContextMenuStrip = menu,
            Visible = false
        };
        _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized && _chkGlobalHook.Checked)
        {
            Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(2000, "DigiTrack",
                _isRecording
                    ? "Still recording in the background. Type anywhere!"
                    : "Running in background. Enable recording from the tray.",
                ToolTipIcon.Info);
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        _notifyIcon.Visible = false;
    }

    private void UpdateTrayIcon()
    {
        _notifyIcon.Icon = _isRecording ? _trayIconRecording : _trayIconIdle;
        _notifyIcon.Text = _isRecording ? "DigiTrack – Recording…" : "DigiTrack – Idle";
    }

    private static Icon MakeTrayIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        // Outer ring
        using var pen = new Pen(Color.FromArgb(180, Color.White), 1f);
        g.DrawEllipse(pen, 1, 1, 13, 13);
        // Filled circle
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 3, 3, 10, 10);
        var hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        // Clone so we can destroy the GDI handle safely
        var clone = (Icon)icon.Clone();
        DestroyIcon(hIcon);
        return clone;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // ── Recording ─────────────────────────────────────────────────────────────

    private void ToggleRecording()
    {
        if (_isRecording)
            StopRecording();
        else
            StartRecording();
    }

    private void StartRecording()
    {
        _isRecording = true;
        _currentSession = new TypingSession
        {
            StartTime = DateTime.Now,
            Title = $"Session {DateTime.Now:yyyy-MM-dd HH:mm}"
        };

        _btnRecord.Text = "⏹  Stop Recording";
        _btnRecord.BackColor = RecordActiveColor;
        _lblStatus.Text = "● Recording";
        _lblStatus.ForeColor = Color.FromArgb(255, 80, 80);

        _statsTimer.Start();
        _wpmSnapshotTimer.Start();

        UpdateTrayIcon();
        _rtbText.Focus();
        ShowNotification("Recording started. Start typing!", 2500);
        SetStatus("Recording active");
    }

    private void StopRecording()
    {
        _isRecording = false;

        _statsTimer.Stop();
        _wpmSnapshotTimer.Stop();

        _currentSession.EndTime = DateTime.Now;
        _currentSession.Text = _rtbText.Text;
        _currentSession.WordCount = CountWords();
        _currentSession.CharCount = _rtbText.TextLength;
        _currentSession.AverageWPM = CalcAverageWpm();

        _btnRecord.Text = "▶  Start Recording";
        _btnRecord.BackColor = RecordIdleColor;
        _lblStatus.Text = "● Stopped";
        _lblStatus.ForeColor = SubtleColor;

        UpdateTrayIcon();
        ShowNotification(
            $"Recording stopped. Words: {_currentSession.WordCount:N0} | " +
            $"WPM: {_currentSession.AverageWPM:F1}", 3500);
        SetStatus("Recording stopped. Use Save Session to store.");
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    private void UpdateStats()
    {
        if (!_isRecording) return;

        int words = CountWords();
        int chars = _rtbText.TextLength;
        double elapsed = (DateTime.Now - _currentSession.StartTime).TotalMinutes;
        double wpm = elapsed > 0.05 ? words / elapsed : 0;

        _lblWords.Text = $"Words:       {words:N0}";
        _lblChars.Text = $"Characters:  {chars:N0}";
        _lblWpm.Text = $"WPM:         {wpm:F1}";

        var ts = DateTime.Now - _currentSession.StartTime;
        _lblTime.Text = $"Time:        {(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
    }

    private void TakeWpmSnapshot()
    {
        if (!_isRecording) return;

        int words = CountWords();
        double elapsed = (DateTime.Now - _currentSession.StartTime).TotalMinutes;
        double wpm = elapsed > 0.05 ? words / elapsed : 0;

        _currentSession.WpmHistory.Add(new WpmSnapshot
        {
            Timestamp = DateTime.Now,
            Wpm = Math.Round(wpm, 1),
            WordCount = words
        });
    }

    private int CountWords()
    {
        var text = _rtbText.Text;
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private double CalcAverageWpm()
    {
        if (_currentSession.WpmHistory.Any())
            return Math.Round(_currentSession.WpmHistory.Average(s => s.Wpm), 1);

        var end = _currentSession.EndTime ?? DateTime.Now;
        double elapsed = (end - _currentSession.StartTime).TotalMinutes;
        return elapsed > 0 ? Math.Round(CountWords() / elapsed, 1) : 0;
    }

    // ── Save & Load ───────────────────────────────────────────────────────────

    private void SaveSession()
    {
        _currentSession.Text = _rtbText.Text;
        _currentSession.WordCount = CountWords();
        _currentSession.CharCount = _rtbText.TextLength;
        _currentSession.AverageWPM = CalcAverageWpm();

        if (!_currentSession.EndTime.HasValue)
            _currentSession.Title = $"Session {_currentSession.StartTime:yyyy-MM-dd HH:mm}";

        bool encrypt = _chkEncrypt.Checked;
        SessionHistory.SaveSession(_currentSession, encrypt);

        if (_currentSession.WpmHistory.Any())
            SessionHistory.SaveWpmHistory(_currentSession);

        var encNote = encrypt ? " (encrypted)" : "";
        ShowNotification($"Session saved{encNote}.", 2500);
        SetStatus($"Saved: {_currentSession.Title}");
    }

    private void AutoSave()
    {
        if (string.IsNullOrWhiteSpace(_rtbText.Text) || !_isRecording) return;
        SaveSession();
        ShowNotification("Auto-saved.", 1500);
    }

    private void LoadFile()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Load File",
            Filter = "DigiTrack Files (*.DigiTrack)|*.DigiTrack|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog() != DialogResult.OK) return;

        try
        {
            var (text, wasEncrypted) = FileManager.LoadFromFile(ofd.FileName);
            _rtbText.Text = text;
            _currentSession = new TypingSession
            {
                Title = Path.GetFileNameWithoutExtension(ofd.FileName),
                Text = text,
                IsEncrypted = wasEncrypted
            };

            var note = wasEncrypted ? " (decrypted)" : "";
            ShowNotification($"Loaded: {Path.GetFileName(ofd.FileName)}{note}", 3000);
            SetStatus($"Loaded from file: {ofd.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load file:\n{ex.Message}", "Load Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveToFile()
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Export Text",
            Filter = "DigiTrack Files (*.DigiTrack)|*.DigiTrack|Text Files (*.txt)|*.txt",
            FileName = _currentSession.Title.Length > 0 ? _currentSession.Title : "session"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            FileManager.ExportToFile(_rtbText.Text, sfd.FileName, _chkEncrypt.Checked);
            ShowNotification($"Exported: {Path.GetFileName(sfd.FileName)}", 2500);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── History ───────────────────────────────────────────────────────────────

    private void OpenHistory()
    {
        using var form = new HistoryForm(_isDarkMode);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        if (form.LoadedText == null) return;

        if (!string.IsNullOrWhiteSpace(_rtbText.Text))
        {
            var res = MessageBox.Show("Replace current text with loaded session?",
                "Load Session", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;
        }

        _rtbText.Text = form.LoadedText;
        _currentSession.WpmHistory = form.LoadedWpmHistory;
        ShowNotification("Session loaded from history.", 2500);
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    private void OpenAnalytics()
    {
        if (string.IsNullOrWhiteSpace(_rtbText.Text))
        {
            ShowNotification("No text to analyze. Start typing first!", 2500);
            return;
        }

        using var form = new AnalyticsForm(_currentSession.WpmHistory, _rtbText.Text, _isDarkMode);
        form.ShowDialog(this);
    }

    // ── Search ────────────────────────────────────────────────────────────────

    private void FindNext()
    {
        var keyword = _txtSearch.Text;
        if (string.IsNullOrEmpty(keyword)) return;

        var text = _rtbText.Text;
        if (string.IsNullOrEmpty(text)) return;

        int start = _searchStart;
        if (start >= text.Length) start = 0;

        int idx = text.IndexOf(keyword, start, StringComparison.OrdinalIgnoreCase);

        if (idx < 0 && start > 0)
        {
            // Wrap around
            idx = text.IndexOf(keyword, 0, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) SetStatus($"Wrapped: found '{keyword}'");
        }

        if (idx >= 0)
        {
            _rtbText.SelectionStart = idx;
            _rtbText.SelectionLength = keyword.Length;
            _rtbText.ScrollToCaret();
            _rtbText.Focus();
            _searchStart = idx + keyword.Length;
            SetStatus($"Found '{keyword}' at position {idx + 1}");
        }
        else
        {
            SetStatus($"'{keyword}' not found.");
            _searchStart = 0;
        }
    }

    private void ClearSearch()
    {
        _txtSearch.Clear();
        _searchStart = 0;
        _rtbText.SelectionLength = 0;
        SetStatus("Search cleared.");
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    private void ClearText()
    {
        if (string.IsNullOrEmpty(_rtbText.Text)) return;

        var res = MessageBox.Show(
            "Clear all text? This will end the current session.",
            "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (res != DialogResult.Yes) return;

        if (_isRecording) StopRecording();

        _rtbText.Clear();
        _currentSession = new TypingSession();
        ResetStatsLabels();
        SetStatus("Text cleared. Ready for new session.");
    }

    private void ResetStatsLabels()
    {
        _lblWords.Text = "Words:       0";
        _lblChars.Text = "Characters:  0";
        _lblWpm.Text = "WPM:         0.0";
        _lblTime.Text = "Time:        00:00";
    }

    // ── Global Keyboard Hook ─────────────────────────────────────────────────

    private void ToggleGlobalHook()
    {
        if (_chkGlobalHook.Checked)
        {
            // Consent dialog
            var consent = MessageBox.Show(
                "Global Typing Mode will capture ALL keystrokes system-wide,\n" +
                "even when this window is not in focus.\n\n" +
                "This feature is for personal productivity monitoring only.\n" +
                "All data stays on your device.\n\n" +
                "Do you consent to enabling global key capture?",
                "Consent Required – Global Typing Mode",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (consent != DialogResult.Yes)
            {
                _chkGlobalHook.Checked = false;
                return;
            }

            try
            {
                _globalHook = new GlobalKeyboardHook();
                _globalHook.KeyCaptured += GlobalHook_KeyCaptured;
                _globalHook.Start();
                ShowNotification("Global keyboard capture active.", 3000);
                SetStatus("Global keyboard hook enabled.");
            }
            catch (Exception ex)
            {
                _chkGlobalHook.Checked = false;
                MessageBox.Show($"Failed to enable global hook:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            _globalHook?.Stop();
            _globalHook?.Dispose();
            _globalHook = null;
            ShowNotification("Global keyboard capture stopped.", 2000);
            SetStatus("Global keyboard hook disabled.");
        }
    }

    private void GlobalHook_KeyCaptured(object? sender, GlobalKeyEventArgs e)
    {
        if (!_isRecording) return;
        if (ContainsFocus) return; // Already captured by the TextBox

        InvokeOnUiThread(() =>
        {
            if (e.Key == Keys.Back)
            {
                if (_rtbText.TextLength > 0)
                    _rtbText.Text = _rtbText.Text[..^1];
            }
            else if (e.Key == Keys.Return)
            {
                _rtbText.AppendText(Environment.NewLine);
            }
            else if (e.Key == Keys.Tab)
            {
                _rtbText.AppendText("\t");
            }
            else if (e.Character != null && !string.IsNullOrEmpty(e.Character)
                     && !char.IsControl(e.Character[0]))
            {
                _rtbText.AppendText(e.Character);
            }

            _rtbText.SelectionStart = _rtbText.TextLength;
            _rtbText.ScrollToCaret();
        });
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        _btnTheme.Text = _isDarkMode ? "☀  Light Mode" : "🌙  Dark Mode";
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        BackColor = BgColor;

        _pnlLeft.BackColor = PanelColor;

        foreach (Control c in _pnlLeft.Controls)
        {
            c.BackColor = PanelColor;
            c.ForeColor = FgColor;

            if (c is Label lbl)
            {
                if (lbl == _lblStatus && _isRecording)
                    lbl.ForeColor = Color.FromArgb(255, 80, 80);
            }

            if (c is Button btn)
            {
                if (btn == _btnRecord) { /* keep its own color */ }
                else if (btn == _btnTheme)
                    btn.BackColor = Color.FromArgb(80, 80, 90);
            }

            if (c is CheckBox chk)
            {
                chk.BackColor = PanelColor;
                chk.ForeColor = FgColor;
            }
        }

        _rtbText.BackColor = TextBoxBg;
        _rtbText.ForeColor = FgColor;

        _txtSearch.BackColor = TextBoxBg;
        _txtSearch.ForeColor = FgColor;

        _lblStatusBar.ForeColor = SubtleColor;
        _lblStatusBar.BackColor = BgColor;

        // Divider color
        if (Controls.OfType<Panel>().FirstOrDefault(p => p.Width == 1) is Panel div)
            div.BackColor = _isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 200, 200);

        // Stats labels accent
        _lblWords.ForeColor = _isDarkMode ? Color.FromArgb(180, 214, 255) : AccentColor;
        _lblChars.ForeColor = _isDarkMode ? Color.FromArgb(180, 214, 255) : AccentColor;
        _lblWpm.ForeColor = _isDarkMode ? Color.FromArgb(180, 255, 180) : Color.FromArgb(16, 124, 16);
        _lblTime.ForeColor = SubtleColor;

        // Bottom bar — style search box and status label
        _txtSearch.BackColor = TextBoxBg;
        _txtSearch.ForeColor = FgColor;
        _lblStatusBar.ForeColor = SubtleColor;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void RtbText_TextChanged(object? sender, EventArgs e)
    {
        if (!_isRecording) return;
        UpdateStats();
    }

    private void RtbText_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+S → Save
        if (e.Control && e.KeyCode == Keys.S)
        {
            e.SuppressKeyPress = true;
            SaveSession();
        }
        // Ctrl+F → Focus search
        if (e.Control && e.KeyCode == Keys.F)
        {
            e.SuppressKeyPress = true;
            _txtSearch.Focus();
            _txtSearch.SelectAll();
        }
        // Ctrl+R → Toggle recording
        if (e.Control && e.KeyCode == Keys.R)
        {
            e.SuppressKeyPress = true;
            ToggleRecording();
        }
    }

    private void MainForm_Closing(object? sender, FormClosingEventArgs e)
    {
        if (_isRecording && !string.IsNullOrWhiteSpace(_rtbText.Text))
        {
            var res = MessageBox.Show(
                "Recording is active. Save session before closing?",
                "Unsaved Session",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel) { e.Cancel = true; return; }
            if (res == DialogResult.Yes) SaveSession();
        }

        _globalHook?.Stop();
        _globalHook?.Dispose();
        _statsTimer.Dispose();
        _autoSaveTimer.Dispose();
        _wpmSnapshotTimer.Dispose();
        _notifTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIconIdle.Dispose();
        _trayIconRecording.Dispose();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowNotification(string message, int durationMs = 3000)
    {
        _lblNotification.Text = message;
        _lblNotification.Visible = true;
        _notifTimer.Interval = durationMs;
        _notifTimer.Stop();
        _notifTimer.Start();
    }

    private void SetStatus(string text)
        => _lblStatusBar.Text = text;

    private void InvokeOnUiThread(Action action)
    {
        if (InvokeRequired) Invoke(action);
        else action();
    }

    private static Label MakeLabel(string text, Font? font = null)
        => new()
        {
            Text = text,
            AutoSize = false,
            Height = font?.Size > 10 ? 30 : 20,
            Dock = DockStyle.Top,
            Font = font ?? new Font("Segoe UI", 9f),
            Padding = new Padding(2, 2, 0, 0)
        };

    private static Panel MakeSeparator()
        => new()
        {
            Dock = DockStyle.Top,
            Height = 8,
            BackColor = Color.Transparent
        };

    private static Button MakeSideButton(string text, Color backColor)
        => new()
        {
            Text = text,
            Height = 30,
            Dock = DockStyle.Top,
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 0)
        };

    private static CheckBox MakeCheckbox(string text)
        => new()
        {
            Text = text,
            AutoSize = false,
            Height = 22,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 8.5f),
            Padding = new Padding(2, 0, 0, 0)
        };
}
