using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.Mainpages;

public partial class MoodViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _username = "Daniel";

    [ObservableProperty]
    private ObservableCollection<MoodOption> _moods = new();

    [ObservableProperty]
    private MoodOption _selectedMood;

    [ObservableProperty]
    private bool _isNextEnabled;

    public MoodViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
        LoadUser();
        LoadMoods();
    }

    partial void OnSelectedMoodChanged(MoodOption value)
    {
        IsNextEnabled = value != null;
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
        catch { /* keep default */ }
    }

    private void LoadMoods()
    {
        // Supply image asset filenames via the Icon property (Resources/Images)
        Moods = new ObservableCollection<MoodOption>
        {
            new(name: "Happiness", emoji: string.Empty, icon: "happiness.png"),
            new(name: "Love",      emoji: string.Empty, icon: "love.png"),
            new(name: "Angry",     emoji: string.Empty, icon: "anger.png"),
            new(name: "Surprise",  emoji: string.Empty, icon: "surprise.png"),
            new(name: "Sadness",   emoji: string.Empty, icon: "sadness.png"),
            new(name: "Fear",      emoji: string.Empty, icon: "fear.png")
        };
    }

    [RelayCommand]
    private async Task Next()
    {
        if (SelectedMood == null) return;
        if (Shell.Current == null) return;

        var navParams = new Dictionary<string, object>
        {
            ["Mood"] = SelectedMood,
            ["Username"] = Username
        };
        await Shell.Current.GoToAsync("//main/levelmood", true, navParams);
    }
}
