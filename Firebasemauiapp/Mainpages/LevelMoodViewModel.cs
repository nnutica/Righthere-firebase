using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Model;
using Firebase.Auth;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firebasemauiapp.Mainpages;

[QueryProperty(nameof(Username), nameof(Username))]
[QueryProperty(nameof(Mood), nameof(Mood))]
[QueryProperty(nameof(Score), nameof(Score))]
public partial class LevelMoodViewModel : ObservableObject, IQueryAttributable
{
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _username = "Daniel";

    [ObservableProperty]
    private MoodOption? _mood = new MoodOption("Happiness", "", "happiness.png"); // Default value to prevent null crash

    [ObservableProperty]
    private int? _score = 5;

    public LevelMoodViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
        LoadUser();
    }

    // Receive navigation parameters reliably
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] ApplyQueryAttributes called with {query.Count} parameters");

            if (query.TryGetValue("Username", out var username) && username is string u)
            {
                Username = u;
                System.Diagnostics.Debug.WriteLine($"[LevelMoodViewModel] Username set to: {u}");
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

    private void LoadUser()
    {
        try
        {
            var user = _authClient.User;
            if (user != null)
            {
                var display = user.Info?.DisplayName;
                if (string.IsNullOrWhiteSpace(display))
                {
                    var email = user.Info?.Email;
                    display = !string.IsNullOrWhiteSpace(email) && email.Contains('@')
                        ? email.Split('@')[0]
                        : "Friend";
                }
                Username = display;
            }
        }
        catch { }
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
            ["MoodScore"] = Score ?? 0
        };
        // Use relative route (registered in AppShell.xaml.cs)
        await Shell.Current.GoToAsync("write", true, navParams);
    }
}
