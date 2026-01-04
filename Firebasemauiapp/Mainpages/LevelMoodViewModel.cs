using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Model;
using Firebase.Auth;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebasemauiapp.Services;

namespace Firebasemauiapp.Mainpages;

[QueryProperty(nameof(Username), nameof(Username))]
[QueryProperty(nameof(Mood), nameof(Mood))]
[QueryProperty(nameof(Score), nameof(Score))]
public partial class LevelMoodViewModel : ObservableObject, IQueryAttributable
{
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _username = "Loading..."; // ? ???? loading

    [ObservableProperty]
    private MoodOption? _mood = new MoodOption("Happiness", "", "happiness.png");

    [ObservableProperty]
    private int? _score = 5;

    [ObservableProperty]
    private string _intensityText = "A Little Bit";

    [ObservableProperty]
    private string _intensityQuote = "\"Just a little bit\"";

    [ObservableProperty]
    private bool _isLoading = false; // ? ?? set ???? true ??????? load
    [ObservableProperty]
    private Color _moodBackgroundColor = Color.FromArgb("#FBC30A"); // Default Happiness color

    public LevelMoodViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
        // ??????? load ???????????????? Username ??? navigation parameter
    }

    partial void OnMoodChanged(MoodOption? value)
    {
        if (value != null)
        {
            MoodBackgroundColor = GetMoodColor(value.Name);
        }
    }

    private Color GetMoodColor(string moodName)
    {
        return moodName switch
        {
            "Happiness" => Color.FromArgb("#FBC30A"),
            "Love" => Color.FromArgb("#FF60A0"),
            "Angry" => Color.FromArgb("#E4000F"),
            "Disgust" => Color.FromArgb("#1EA064"),
            "Sadness" => Color.FromArgb("#2B638D"),
            "Fear" => Color.FromArgb("#9E9AAB"),
            _ => Color.FromArgb("#FBC30A")
        };
    }

    // Receive navigation parameters reliably
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] ApplyQueryAttributes called with {query.Count} parameters");

            // ? ?????? Username ??? navigation ?????? ??????? load
            if (query.TryGetValue("Username", out var username) && username is string u && !string.IsNullOrWhiteSpace(u))
            {
                Username = u;
                System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] Username set to: {u}");
            }
            else
            {
                // ? ????????? Username ??? load
                _ = LoadUserAsync();
            }

            if (query.TryGetValue("Mood", out var mood))
            {
                System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] Mood parameter found, type: {mood?.GetType().Name}");
                if (mood is MoodOption m)
                {
                    Mood = m;
                    System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] Mood set to: {m.Name} with icon: {m.Icon}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] WARNING: Mood is not MoodOption type!");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] WARNING: No Mood parameter in query!");
            }

            // Accept either 'Score' or 'MoodScore' if provided
            if (query.TryGetValue("Score", out var s) && s is int si)
            {
                Score = si;
                System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] Score set to: {si}");
            }
            else if (query.TryGetValue("MoodScore", out var ms) && ms is int msi)
            {
                Score = msi;
                System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] MoodScore set to: {msi}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] ApplyQueryAttributes error: {ex.Message}");
        }
    }

    private async Task LoadUserAsync()
    {
        IsLoading = true;
        try
        {
            // ? ?????? UserService ??????
            if (!UserService.Instance.IsLoaded)
            {
                await UserService.Instance.LoadUserAsync();
            }
            
            Username = UserService.Instance.Username;
        }
        catch 
        { 
            Username = "Friend";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnScoreChanged(int? value)
    {
        UpdateIntensityText();
    }

    private void UpdateIntensityText()
    {
        if (!Score.HasValue || Score.Value == 0)
        {
            IntensityText = "A Little Bit";
            IntensityQuote = "\"Just a little bit\"";
            return;
        }

        int score = Score.Value;
        if (score >= 1 && score <= 2)
        {
            IntensityText = "A Little Bit";
            IntensityQuote = "\"Just a little bit\"";
        }
        else if (score >= 3 && score <= 4)
        {
            IntensityText = "Somewhat";
            IntensityQuote = "\"Somewhat\"";
        }
        else if (score >= 5 && score <= 6)
        {
            IntensityText = "Quite a Bit";
            IntensityQuote = "\"Quite a bit\"";
        }
        else if (score >= 7 && score <= 8)
        {
            IntensityText = "A Lot";
            IntensityQuote = "\"A lot\"";
        }
        else // 9-10
        {
            IntensityText = "Overwhelming";
            IntensityQuote = "\"Overwhelming\"";
        }
    }

    [RelayCommand]
    private async Task SetMood()
    {
        if (Shell.Current == null)
            return;

        var navParams = new Dictionary<string, object>
        {
            ["Username"] = Username,
            ["Mood"] = Mood!,
            ["MoodScore"] = Score ?? 0,
            ["IntensityText"] = IntensityText
        };
        await Shell.Current.GoToAsync("write", true, navParams);
    }
}
