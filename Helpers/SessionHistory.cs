using Newtonsoft.Json;
using DigiTrack.Models;

namespace DigiTrack.Helpers;

public static class SessionHistory
{
    private static readonly string SessionsDir;
    private static readonly string HistoryFile;
    private static List<SessionSummary> _history = new();

    static SessionHistory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "DigiTrack");
        SessionsDir = Path.Combine(appDir, "Sessions");
        HistoryFile = Path.Combine(appDir, "history.json");

        Directory.CreateDirectory(SessionsDir);
        LoadHistoryIndex();
    }

    private static void LoadHistoryIndex()
    {
        try
        {
            if (File.Exists(HistoryFile))
            {
                var json = File.ReadAllText(HistoryFile);
                _history = JsonConvert.DeserializeObject<List<SessionSummary>>(json) ?? new();
            }
        }
        catch
        {
            _history = new();
        }
    }

    private static void PersistHistoryIndex()
    {
        var json = JsonConvert.SerializeObject(_history, Formatting.Indented);
        File.WriteAllText(HistoryFile, json);
    }

    public static void SaveSession(TypingSession session, bool encrypt)
    {
        session.EndTime ??= DateTime.Now;

        var fileName = $"session_{session.Id:N}.DigiTrack";
        var filePath = Path.Combine(SessionsDir, fileName);

        var content = encrypt
            ? EncryptionHelper.Encrypt(session.Text)
            : session.Text;

        File.WriteAllText(filePath, content);

        session.IsEncrypted = encrypt;
        session.FileName = fileName;

        var summary = new SessionSummary
        {
            Id = session.Id,
            Title = session.Title,
            StartTime = session.StartTime,
            EndTime = session.EndTime.Value,
            WordCount = session.WordCount,
            CharCount = session.CharCount,
            AverageWPM = Math.Round(session.AverageWPM, 1),
            IsEncrypted = encrypt,
            FileName = fileName
        };

        _history.RemoveAll(h => h.Id == session.Id);
        _history.Insert(0, summary);
        _history = _history.Take(200).ToList();

        PersistHistoryIndex();
    }

    public static List<SessionSummary> GetHistory() => new(_history);

    public static (string? text, List<WpmSnapshot> wpmHistory) LoadSessionContent(SessionSummary summary)
    {
        try
        {
            var filePath = Path.Combine(SessionsDir, summary.FileName);
            if (!File.Exists(filePath)) return (null, new());

            var raw = File.ReadAllText(filePath);
            var text = summary.IsEncrypted ? EncryptionHelper.Decrypt(raw) : raw;

            // Try loading WPM sidecar if it exists
            var wpmFile = Path.ChangeExtension(filePath, ".wpm.json");
            List<WpmSnapshot> wpmHistory = new();
            if (File.Exists(wpmFile))
            {
                try
                {
                    var wpmJson = File.ReadAllText(wpmFile);
                    wpmHistory = JsonConvert.DeserializeObject<List<WpmSnapshot>>(wpmJson) ?? new();
                }
                catch { }
            }

            return (text, wpmHistory);
        }
        catch (Exception ex)
        {
            return ($"[Error loading session: {ex.Message}]", new());
        }
    }

    public static void SaveWpmHistory(TypingSession session)
    {
        if (!session.WpmHistory.Any()) return;

        var filePath = Path.Combine(SessionsDir, Path.ChangeExtension(session.FileName, ".wpm.json"));
        var json = JsonConvert.SerializeObject(session.WpmHistory, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static void DeleteSession(SessionSummary summary)
    {
        try
        {
            var filePath = Path.Combine(SessionsDir, summary.FileName);
            if (File.Exists(filePath)) File.Delete(filePath);

            var wpmFile = Path.Combine(SessionsDir,
                Path.ChangeExtension(summary.FileName, ".wpm.json"));
            if (File.Exists(wpmFile)) File.Delete(wpmFile);
        }
        catch { }

        _history.RemoveAll(h => h.Id == summary.Id);
        PersistHistoryIndex();
    }
}
