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
        // Placeholder emoji icons; replace `Icon` with asset paths later
        Moods = new ObservableCollection<MoodOption>
        {
            new("Happiness", "😊"),
            new("So-So", "😐"),
            new("Angry", "😡"),
            new("Disgust", "🤢"),
            new("Sadness", "😔"),
            new("Fear", "😱")
        };
    }

    [RelayCommand]
    private async Task Next()
    {
        if (SelectedMood == null) return;
        if (Shell.Current == null) return;

        var navParams = new Dictionary<string, object>
        {
            ["Mood"] = SelectedMood
        };
        await Shell.Current.GoToAsync("//main/levelmood", true, navParams);
    }
}
