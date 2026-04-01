using DigiTrack.Helpers;
using DigiTrack.Models;

namespace DigiTrack.Forms;

public class HistoryForm : Form
{
    private ListView _listView = null!;
    private Button _btnOpen = null!;
    private Button _btnDelete = null!;
    private Button _btnExport = null!;
    private Button _btnClose = null!;
    private Label _lblInfo = null!;

    public string? LoadedText { get; private set; }
    public List<WpmSnapshot> LoadedWpmHistory { get; private set; } = new();

    private readonly bool _isDarkMode;

    public HistoryForm(bool isDarkMode)
    {
        _isDarkMode = isDarkMode;
        InitializeComponent();
        ApplyTheme();
        LoadHistory();
    }

    private void InitializeComponent()
    {
        Text = "Session History";
        Size = new Size(820, 500);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            View = View.Details,
            Font = new Font("Consolas", 9f)
        };

        _listView.Columns.AddRange(new[]
        {
            new ColumnHeader { Text = "Title",     Width = 200 },
            new ColumnHeader { Text = "Date",      Width = 140 },
            new ColumnHeader { Text = "Duration",  Width = 80  },
            new ColumnHeader { Text = "Words",     Width = 70  },
            new ColumnHeader { Text = "Chars",     Width = 70  },
            new ColumnHeader { Text = "Avg WPM",   Width = 70  },
            new ColumnHeader { Text = "Encrypted", Width = 75  },
        });

        _listView.DoubleClick += (_, _) => OpenSelected();
        _listView.SelectedIndexChanged += (_, _) => UpdateButtonStates();

        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Padding = new Padding(6),
            FlowDirection = FlowDirection.LeftToRight
        };

        _btnOpen = CreateButton("Open Session", Color.FromArgb(0, 122, 204));
        _btnDelete = CreateButton("Delete", Color.FromArgb(196, 43, 28));
        _btnExport = CreateButton("Export .txt", Color.FromArgb(70, 70, 70));
        _btnClose = CreateButton("Close", Color.FromArgb(80, 80, 80));

        _btnOpen.Click += (_, _) => OpenSelected();
        _btnDelete.Click += (_, _) => DeleteSelected();
        _btnExport.Click += (_, _) => ExportSelected();
        _btnClose.Click += (_, _) => Close();

        _lblInfo = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.Gray,
            Padding = new Padding(4, 8, 0, 0)
        };

        pnlBottom.Controls.AddRange(new Control[]
            { _btnOpen, _btnDelete, _btnExport, _btnClose, _lblInfo });

        Controls.Add(_listView);
        Controls.Add(pnlBottom);

        UpdateButtonStates();
    }

    private Button CreateButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            Size = new Size(110, 30),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(2)
        };
    }

    private void LoadHistory()
    {
        _listView.Items.Clear();
        var history = SessionHistory.GetHistory();

        foreach (var s in history)
        {
            var item = new ListViewItem(s.Title);
            item.SubItems.Add(s.StartTime.ToString("yyyy-MM-dd HH:mm"));
            item.SubItems.Add(s.DurationText);
            item.SubItems.Add(s.WordCount.ToString("N0"));
            item.SubItems.Add(s.CharCount.ToString("N0"));
            item.SubItems.Add(s.AverageWPM > 0 ? $"{s.AverageWPM:F1}" : "-");
            item.SubItems.Add(s.IsEncrypted ? "Yes" : "No");
            item.Tag = s;
            _listView.Items.Add(item);
        }

        _lblInfo.Text = $"{history.Count} session(s) stored";
    }

    private void OpenSelected()
    {
        if (_listView.SelectedItems.Count == 0) return;
        var summary = (SessionSummary)_listView.SelectedItems[0].Tag!;

        var (text, wpmHistory) = SessionHistory.LoadSessionContent(summary);
        if (text == null)
        {
            MessageBox.Show("Session file not found. It may have been moved or deleted.",
                "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadedText = text;
        LoadedWpmHistory = wpmHistory;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void DeleteSelected()
    {
        if (_listView.SelectedItems.Count == 0) return;
        var summary = (SessionSummary)_listView.SelectedItems[0].Tag!;

        var result = MessageBox.Show(
            $"Delete session \"{summary.Title}\"?\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        SessionHistory.DeleteSession(summary);
        LoadHistory();
    }

    private void ExportSelected()
    {
        if (_listView.SelectedItems.Count == 0) return;
        var summary = (SessionSummary)_listView.SelectedItems[0].Tag!;

        var (text, _) = SessionHistory.LoadSessionContent(summary);
        if (text == null)
        {
            MessageBox.Show("Session file not found.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Title = "Export Session",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FileName = $"{summary.Title}.txt"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            File.WriteAllText(sfd.FileName, text, System.Text.Encoding.UTF8);
            MessageBox.Show($"Exported to:\n{sfd.FileName}", "Export Successful",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = _listView.SelectedItems.Count > 0;
        _btnOpen.Enabled = hasSelection;
        _btnDelete.Enabled = hasSelection;
        _btnExport.Enabled = hasSelection;
    }

    private void ApplyTheme()
    {
        if (_isDarkMode)
        {
            BackColor = Color.FromArgb(37, 37, 38);
            _listView.BackColor = Color.FromArgb(30, 30, 30);
            _listView.ForeColor = Color.FromArgb(220, 220, 220);
        }
        else
        {
            BackColor = Color.FromArgb(245, 245, 245);
            _listView.BackColor = Color.White;
            _listView.ForeColor = Color.FromArgb(30, 30, 30);
        }
    }
}
