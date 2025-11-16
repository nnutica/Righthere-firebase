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
    private MoodOption? _mood;

    [ObservableProperty]
    private int _score = 10;

    public LevelMoodViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
        LoadUser();
    }

    // Receive navigation parameters reliably
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Username", out var username) && username is string u)
            Username = u;

        if (query.TryGetValue("Mood", out var mood) && mood is MoodOption m)
            Mood = m;

        // Accept either 'Score' or 'MoodScore' if provided
        if (query.TryGetValue("Score", out var s) && s is int si)
            Score = si;
        else if (query.TryGetValue("MoodScore", out var ms) && ms is int msi)
            Score = msi;
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
            ["MoodScore"] = Score
        };
        await Shell.Current.GoToAsync("//main/write", true, navParams);
    }
}
