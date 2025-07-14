using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class StarterViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly DiaryDatabase _diaryDatabase;

    [ObservableProperty]
    private string _userName = "Guest";

    [ObservableProperty]
    private string _userEmail = string.Empty;

    public StarterViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        LoadUserInfo();
    }

    private void LoadUserInfo()
    {
        if (_authClient.User != null)
        {
            UserName = _authClient.User.Info?.DisplayName ?? "Unknown User";
            UserEmail = _authClient.User.Info?.Email ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task GoToDiary()
    {
        await Shell.Current.Navigation.PushAsync(new DiaryView(_diaryDatabase, _authClient));
    }

    [RelayCommand]
    private async Task LogOut()
    {
        try
        {
            _authClient.SignOut();
            await Shell.Current.GoToAsync("//signin");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
        }
    }
}
