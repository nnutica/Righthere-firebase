using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace Firebasemauiapp.Pages;

public partial class SignInViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public string? Username => _authClient.User?.Info?.DisplayName;


    public SignInViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;
    }

    [RelayCommand]
    private async Task SignIn()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Please enter both email and password.", "OK");
                return;
            }

            var result = await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
            OnPropertyChanged(nameof(Username));

            var uid = result?.User?.Uid;
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var db = await _firestoreService.GetDatabaseAsync();
                var userDocRef = db.Collection("users").Document(uid);
                var snapshot = await userDocRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    var displayName = result!.User!.Info?.DisplayName ?? string.Empty;
                    var payload = new Dictionary<string, object>
                    {
                        { "uid", uid },
                        { "email", Email },
                        { "username", string.IsNullOrWhiteSpace(displayName) ? Email : displayName },
                        { "coin", 0 },
                        { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                    };
                    await userDocRef.SetAsync(payload, SetOptions.Overwrite);
                }
            }

            // Navigate to Main TabBar after successful login
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//main/starter");
        }
        catch (Exception ex)
        {
            // Handle login error
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task NavigateSignUp()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//signup");
    }

}
