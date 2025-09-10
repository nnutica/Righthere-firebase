using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Microsoft.Maui.Storage;

namespace Firebasemauiapp.Mainpages;

public partial class StarterViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly DiaryDatabase _diaryDatabase;

    private string _userName = "Guest";
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    private string _userEmail = string.Empty;
    public string UserEmail
    {
        get => _userEmail;
        set => SetProperty(ref _userEmail, value);
    }

    public StarterViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        RefreshUserInfo();
    }

    public void RefreshUserInfo()
    {
        if (_authClient.User != null)
        {
            var displayName = _authClient.User.Info?.DisplayName;


            // Prefer display name; if missing, fall back to the email's local part
            if (string.IsNullOrWhiteSpace(displayName))


                UserName = string.IsNullOrWhiteSpace(displayName) ? "Unknown User" : displayName;

        }
    }

    [RelayCommand]
    private async Task LogOut()
    {
        try
        {
            // Sign out from Firebase client
            _authClient.SignOut();

            // Clear any locally persisted tokens/preferences (best-effort)
            try { SecureStorage.Default.RemoveAll(); } catch { /* ignore */ }
            try { Preferences.Default.Clear(); } catch { /* ignore */ }

            // Reset in-memory user info
            UserName = "Guest";
            UserEmail = string.Empty;

            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//signin");
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
        }
    }
}
