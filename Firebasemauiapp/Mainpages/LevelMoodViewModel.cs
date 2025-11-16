using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Model;
using Firebase.Auth;
using System.Threading.Tasks;

namespace Firebasemauiapp.Mainpages;

[QueryProperty(nameof(Mood), nameof(Mood))]
public partial class LevelMoodViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _username = "Daniel";

    [ObservableProperty]
    private MoodOption? _mood;

    [ObservableProperty]
    private int _score = 50;

    public LevelMoodViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
        LoadUser();
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
