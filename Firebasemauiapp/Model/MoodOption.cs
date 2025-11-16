namespace Firebasemauiapp.Model;

public class MoodOption
{
    public string Name { get; }
    public string Emoji { get; }
    public string? Icon { get; }

    public MoodOption(string name, string emoji, string? icon = null)
    {
        Name = name;
        Emoji = emoji;
        Icon = icon;
    }
}
