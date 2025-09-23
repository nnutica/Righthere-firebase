using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel; // For MainThread

namespace Firebasemauiapp.Mainpages;

public partial class StarterViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirestoreService _firestoreService;

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

    public StarterViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        _firestoreService = firestoreService;
        // React to auth state changes so UI updates after sign-in/sign-out.
        _authClient.AuthStateChanged += OnAuthStateChanged;
        RefreshUserInfo();
        _ = RefreshCoinAsync();
    }

    public void RefreshUserInfo()
    {
        var user = _authClient.User;
        if (user == null)
        {
            UserName = "Guest";
            UserEmail = string.Empty;
            Coin = 0;
            return;
        }

        var info = user.Info; // may be null if not loaded yet
        var displayName = info?.DisplayName;
        var email = info?.Email;

        // Resolve a username preference order:
        // 1. Non-empty display name
        // 2. Local-part of email (before @)
        // 3. "Unknown User"
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            UserName = displayName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var at = email.IndexOf('@');
            UserName = at > 0 ? email.Substring(0, at) : email;
        }
        else
        {
            UserName = "Unknown User";
        }

        UserEmail = email ?? string.Empty;
        _ = RefreshCoinAsync();
    }

    private int _coin;
    public int Coin
    {
        get => _coin;
        set => SetProperty(ref _coin, value);
    }

    private async Task RefreshCoinAsync()
    {
        try
        {
            var user = _authClient.User;
            if (user?.Uid == null)
            {
                Coin = 0;
                return;
            }

            var db = await _firestoreService.GetDatabaseAsync();
            var snap = await db.Collection("users").Document(user.Uid).GetSnapshotAsync();
            if (snap.Exists && snap.TryGetValue("coin", out int coin))
            {
                Coin = coin;
            }
            else
            {
                Coin = 0;
            }
        }
        catch
        {
            Coin = 0;
        }
    }

    private void OnAuthStateChanged(object? sender, UserEventArgs e)
    {
        // Ensure UI updates on main thread
        if (MainThread.IsMainThread)
            RefreshUserInfo();
        else
            MainThread.BeginInvokeOnMainThread(RefreshUserInfo);
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
