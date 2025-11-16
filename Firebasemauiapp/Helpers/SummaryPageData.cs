namespace Firebasemauiapp.Helpers;

public static class SummaryPageData
{
    public static string? Content { get; private set; }
    public static string? Mood { get; private set; }
    public static string? Suggestion { get; private set; }
    public static string? Keywords { get; private set; }
    public static string? Emotion { get; private set; }
    public static string? Score { get; private set; }
    public static string? ImageUrl { get; private set; }

    public static void SetData(string content, string mood, string suggestion,
                              string keywords, string emotion, string score, string? imageUrl = null)
    {
        Content = content;
        Mood = mood;
        Suggestion = suggestion;
        Keywords = keywords;
        Emotion = emotion;
        Score = score;
        ImageUrl = imageUrl;
    }

    public static void Clear()
    {
        Content = null;
        Mood = null;
        Suggestion = null;
        Keywords = null;
        Emotion = null;
        Score = null;
        ImageUrl = null;
    }
}
