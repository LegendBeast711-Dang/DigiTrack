using DigiTrack.Models;

namespace DigiTrack.Forms;

public class AnalyticsForm : Form
{
    private readonly List<WpmSnapshot> _wpmHistory;
    private readonly string _text;
    private readonly bool _isDarkMode;

    private Panel _wpmChartPanel = null!;
    private Panel _wordFreqPanel = null!;
    private Label _lblSummary = null!;
    private TabControl _tabs = null!;

    // Theme colors
    private Color _bgColor;
    private Color _fgColor;
    private Color _panelBg;
    private Color _accentColor;

    public AnalyticsForm(List<WpmSnapshot> wpmHistory, string text, bool isDarkMode)
    {
        _wpmHistory = wpmHistory;
        _text = text;
        _isDarkMode = isDarkMode;

        SetThemeColors();
        InitializeComponent();
    }

    private void SetThemeColors()
    {
        if (_isDarkMode)
        {
            _bgColor = Color.FromArgb(37, 37, 38);
            _fgColor = Color.FromArgb(220, 220, 220);
            _panelBg = Color.FromArgb(30, 30, 30);
            _accentColor = Color.FromArgb(0, 122, 204);
        }
        else
        {
            _bgColor = Color.FromArgb(245, 245, 245);
            _fgColor = Color.FromArgb(30, 30, 30);
            _panelBg = Color.White;
            _accentColor = Color.FromArgb(0, 100, 200);
        }
    }

    private void InitializeComponent()
    {
        Text = "Typing Analytics Dashboard";
        Size = new Size(860, 600);
        MinimumSize = new Size(700, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgColor;
        ForeColor = _fgColor;

        // Summary panel at top
        _lblSummary = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Font = new Font("Segoe UI", 10f),
            ForeColor = _fgColor,
            BackColor = _bgColor,
            Padding = new Padding(12, 8, 0, 0),
            Text = BuildSummaryText()
        };

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f)
        };

        // Tab 1 - WPM chart
        var tabWpm = new TabPage("WPM Over Time")
        {
            BackColor = _bgColor,
            ForeColor = _fgColor
        };

        _wpmChartPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelBg
        };
        _wpmChartPanel.Paint += WpmChart_Paint;
        tabWpm.Controls.Add(_wpmChartPanel);

        // Tab 2 - Word frequency
        var tabFreq = new TabPage("Word Frequency")
        {
            BackColor = _bgColor,
            ForeColor = _fgColor
        };

        _wordFreqPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelBg
        };
        _wordFreqPanel.Paint += WordFreqChart_Paint;
        tabFreq.Controls.Add(_wordFreqPanel);

        // Tab 3 - Text stats
        var tabStats = new TabPage("Text Statistics")
        {
            BackColor = _bgColor,
            ForeColor = _fgColor
        };
        tabStats.Controls.Add(BuildStatsPanel());

        _tabs.TabPages.AddRange(new[] { tabWpm, tabFreq, tabStats });

        var btnClose = new Button
        {
            Text = "Close",
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Color.FromArgb(70, 70, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f)
        };
        btnClose.Click += (_, _) => Close();

        Controls.Add(_tabs);
        Controls.Add(_lblSummary);
        Controls.Add(btnClose);
    }

    private string BuildSummaryText()
    {
        var words = CountWords();
        var chars = _text.Length;
        double avgWpm = _wpmHistory.Any() ? _wpmHistory.Average(w => w.Wpm) : 0;
        double peakWpm = _wpmHistory.Any() ? _wpmHistory.Max(w => w.Wpm) : 0;

        return $"Total Words: {words:N0}   |   Characters: {chars:N0}   |" +
               $"   Avg WPM: {avgWpm:F1}   |   Peak WPM: {peakWpm:F1}";
    }

    private void WpmChart_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var panel = (Panel)sender!;
        var width = panel.ClientSize.Width;
        var height = panel.ClientSize.Height;

        int padL = 60, padR = 20, padT = 30, padB = 50;
        int chartW = width - padL - padR;
        int chartH = height - padT - padB;

        g.Clear(_panelBg);

        if (_wpmHistory.Count < 2)
        {
            using var emptyFont = new Font("Segoe UI", 12f);
            var msg = _wpmHistory.Count == 0
                ? "No WPM data available.\nStart a recording session to see your typing speed over time."
                : "Need at least 2 data points to draw a chart.";
            g.DrawString(msg, emptyFont, new SolidBrush(_fgColor), padL, padT + 20);
            return;
        }

        double maxWpm = _wpmHistory.Max(w => w.Wpm);
        double minWpm = Math.Max(0, _wpmHistory.Min(w => w.Wpm) - 5);
        double range = maxWpm - minWpm;
        if (range < 10) range = 10;

        // Draw grid and Y-axis labels
        using var gridPen = new Pen(Color.FromArgb(60, _fgColor), 1);
        gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        using var axisFont = new Font("Consolas", 7.5f);
        using var axisBrush = new SolidBrush(Color.FromArgb(160, _fgColor));

        int gridLines = 5;
        for (int i = 0; i <= gridLines; i++)
        {
            double val = minWpm + (range / gridLines) * i;
            int y = padT + chartH - (int)(chartH * (val - minWpm) / range);
            g.DrawLine(gridPen, padL, y, padL + chartW, y);
            g.DrawString($"{val:F0}", axisFont, axisBrush, 2, y - 7);
        }

        // Draw axes
        using var axisPen = new Pen(_fgColor, 1.5f);
        g.DrawLine(axisPen, padL, padT, padL, padT + chartH);
        g.DrawLine(axisPen, padL, padT + chartH, padL + chartW, padT + chartH);

        // Plot points
        var points = new PointF[_wpmHistory.Count];
        for (int i = 0; i < _wpmHistory.Count; i++)
        {
            float x = padL + (float)(chartW * i / (_wpmHistory.Count - 1));
            float y = padT + chartH - (float)(chartH * (_wpmHistory[i].Wpm - minWpm) / range);
            points[i] = new PointF(x, y);
        }

        // Filled area under curve
        var fillPoints = new List<PointF> { new(padL, padT + chartH) };
        fillPoints.AddRange(points);
        fillPoints.Add(new PointF(padL + chartW, padT + chartH));
        using var fillBrush = new SolidBrush(Color.FromArgb(40, _accentColor));
        g.FillPolygon(fillBrush, fillPoints.ToArray());

        // Line
        using var linePen = new Pen(_accentColor, 2.5f);
        g.DrawLines(linePen, points);

        // Data point dots
        using var dotBrush = new SolidBrush(_accentColor);
        foreach (var pt in points)
            g.FillEllipse(dotBrush, pt.X - 3.5f, pt.Y - 3.5f, 7, 7);

        // X-axis time labels
        int labelStep = Math.Max(1, _wpmHistory.Count / 8);
        for (int i = 0; i < _wpmHistory.Count; i += labelStep)
        {
            var label = _wpmHistory[i].Timestamp.ToString("HH:mm:ss");
            float x = padL + (float)(chartW * i / (_wpmHistory.Count - 1));
            g.DrawString(label, axisFont, axisBrush, x - 20, padT + chartH + 5);
        }

        // Title
        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        g.DrawString("Words Per Minute Over Time", titleFont, new SolidBrush(_fgColor), padL, 6);

        // Y-axis label
        g.TranslateTransform(12, padT + chartH / 2);
        g.RotateTransform(-90);
        g.DrawString("WPM", axisFont, axisBrush, -15, 0);
        g.ResetTransform();
    }

    private void WordFreqChart_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var panel = (Panel)sender!;
        var width = panel.ClientSize.Width;
        var height = panel.ClientSize.Height;

        g.Clear(_panelBg);

        var freq = GetWordFrequency(15);
        if (freq.Count == 0)
        {
            using var font = new Font("Segoe UI", 12f);
            g.DrawString("No text to analyze.", font, new SolidBrush(_fgColor), 20, 20);
            return;
        }

        int padL = 100, padR = 30, padT = 40, padB = 30;
        int chartW = width - padL - padR;
        int chartH = height - padT - padB;

        int barHeight = Math.Min(28, (chartH / freq.Count) - 4);
        int maxCount = freq[0].count;

        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        g.DrawString("Top Word Frequency", titleFont, new SolidBrush(_fgColor), padL, 8);

        using var labelFont = new Font("Consolas", 8.5f);
        using var countFont = new Font("Segoe UI", 8f);
        using var wordBrush = new SolidBrush(_fgColor);
        using var countBrush = new SolidBrush(Color.Gray);

        // Color palette for bars
        var colors = new Color[]
        {
            Color.FromArgb(0, 122, 204),
            Color.FromArgb(0, 150, 136),
            Color.FromArgb(103, 58, 183),
            Color.FromArgb(244, 81, 30),
            Color.FromArgb(30, 136, 229)
        };

        for (int i = 0; i < freq.Count; i++)
        {
            var (word, count) = freq[i];
            float y = padT + i * (barHeight + 6);
            float barWidth = (float)(chartW * count) / maxCount;

            using var barBrush = new SolidBrush(colors[i % colors.Length]);

            // Word label
            g.DrawString(word, labelFont, wordBrush, padL - 95, y + 2);

            // Bar
            g.FillRectangle(barBrush, padL, y, barWidth, barHeight);

            // Count label
            g.DrawString(count.ToString(), countFont, countBrush,
                padL + barWidth + 4, y + 3);
        }
    }

    private Panel BuildStatsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _bgColor,
            AutoScroll = true,
            Padding = new Padding(20)
        };

        var words = _text.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        var sentences = _text.Split(new[] { '.', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);
        var paragraphs = _text.Split(new[] { "\n\n" },
            StringSplitOptions.RemoveEmptyEntries);
        var uniqueWords = words.Select(w => w.ToLowerInvariant()
                .Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')'))
            .Distinct().Count();

        double avgWpm = _wpmHistory.Any() ? _wpmHistory.Average(w => w.Wpm) : 0;
        double avgWordLen = words.Any()
            ? words.Average(w => w.Length)
            : 0;

        var stats = new[]
        {
            ("Total Characters", _text.Length.ToString("N0")),
            ("Characters (no spaces)", _text.Replace(" ", "").Replace("\n", "").Length.ToString("N0")),
            ("Total Words", words.Length.ToString("N0")),
            ("Unique Words", uniqueWords.ToString("N0")),
            ("Sentences (approx.)", sentences.Length.ToString("N0")),
            ("Paragraphs (approx.)", paragraphs.Length.ToString("N0")),
            ("Avg Word Length", $"{avgWordLen:F1} chars"),
            ("Average WPM", $"{avgWpm:F1}"),
            ("Peak WPM", _wpmHistory.Any() ? $"{_wpmHistory.Max(w => w.Wpm):F1}" : "N/A"),
            ("WPM Snapshots", _wpmHistory.Count.ToString()),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = stats.Length,
            Padding = new Padding(10),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        using var boldFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var valFont = new Font("Consolas", 10f);

        foreach (var (label, value) in stats)
        {
            var lblKey = new Label
            {
                Text = label,
                Font = boldFont,
                ForeColor = _fgColor,
                BackColor = _bgColor,
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 6, 4, 6),
                AutoSize = false
            };
            var lblVal = new Label
            {
                Text = value,
                Font = valFont,
                ForeColor = _accentColor,
                BackColor = _bgColor,
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 6, 4, 6),
                AutoSize = false
            };
            layout.Controls.Add(lblKey);
            layout.Controls.Add(lblVal);
        }

        panel.Controls.Add(layout);
        return panel;
    }

    private int CountWords()
        => string.IsNullOrWhiteSpace(_text)
            ? 0
            : _text.Split(new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;

    private List<(string word, int count)> GetWordFrequency(int topN)
    {
        if (string.IsNullOrWhiteSpace(_text)) return new();

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to",
            "for", "of", "with", "is", "it", "this", "that", "i", "you",
            "he", "she", "we", "they", "was", "are", "be", "been", "have",
            "has", "had", "will", "would", "can", "could", "do", "did", "not"
        };

        return _text
            .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '"', '\'' },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant().Trim())
            .Where(w => w.Length > 2 && !stopWords.Contains(w) && w.All(char.IsLetter))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(topN)
            .Select(g => (g.Key, g.Count()))
            .ToList();
    }
}
