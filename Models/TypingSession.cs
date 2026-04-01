namespace DigiTrack.Models;

public class TypingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public double AverageWPM { get; set; }
    public List<WpmSnapshot> WpmHistory { get; set; } = new();
    public bool IsEncrypted { get; set; }
    public string FileName { get; set; } = string.Empty;

    public TimeSpan Duration => EndTime.HasValue
        ? EndTime.Value - StartTime
        : DateTime.Now - StartTime;

    public string DurationText
    {
        get
        {
            var d = Duration;
            return d.TotalHours >= 1
                ? $"{(int)d.TotalHours}h {d.Minutes}m {d.Seconds}s"
                : $"{d.Minutes}m {d.Seconds}s";
        }
    }
}

public class WpmSnapshot
{
    public DateTime Timestamp { get; set; }
    public double Wpm { get; set; }
    public int WordCount { get; set; }
}

public class SessionSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public double AverageWPM { get; set; }
    public bool IsEncrypted { get; set; }
    public string FileName { get; set; } = string.Empty;

    public string DurationText
    {
        get
        {
            var d = EndTime - StartTime;
            return d.TotalHours >= 1
                ? $"{(int)d.TotalHours}h {d.Minutes}m {d.Seconds}s"
                : $"{d.Minutes}m {d.Seconds}s";
        }
    }
}
