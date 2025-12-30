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
            new(name: "Disgust",  emoji: string.Empty, icon: "disgust.png"),
            new(name: "Sadness",   emoji: string.Empty, icon: "sadness.png"),
            new(name: "Fear",      emoji: string.Empty, icon: "fear.png")
        };
    }

    [RelayCommand]
    private async Task Next()
    {
        if (SelectedMood == null)
        {
            System.Diagnostics.Debug.WriteLine("[MoodViewModel] SelectedMood is null!");
            return;
        }
        if (Shell.Current == null)
        {
            System.Diagnostics.Debug.WriteLine("[MoodViewModel] Shell.Current is null!");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Navigating with Mood: {SelectedMood.Name}, Icon: {SelectedMood.Icon}");

            var navParams = new Dictionary<string, object>
            {
                ["Mood"] = SelectedMood,
                ["Username"] = Username
            };

            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] NavParams count: {navParams.Count}");

            // Navigate using relative route (registered in AppShell.xaml.cs)
            await Shell.Current.GoToAsync("levelmood", false, navParams);

            System.Diagnostics.Debug.WriteLine("[MoodViewModel] Navigation completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Navigation error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Stack trace: {ex.StackTrace}");
        }
    }
}
