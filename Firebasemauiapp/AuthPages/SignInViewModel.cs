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

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    public string? Username => _authClient.User?.Info?.DisplayName;

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }


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
            // Clear previous error
            ErrorMessage = string.Empty;
            HasError = false;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both email and password.";
                HasError = true;
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
                        {"role", "user" },
                        { "coin", 0 },
                        { "inventory", new List<string>() },
                        { "currentPlant", "empty.png" },
                        { "currentPot", "pot.png" },
                        { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                    };
                    await userDocRef.SetAsync(payload, SetOptions.Overwrite);
                }
            }

            // Clear error and navigate to Main TabBar after successful login
            ErrorMessage = string.Empty;
            HasError = false;
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//main/starter");
        }
        catch (Exception ex)
        {
            // Handle login error with inline message
            ErrorMessage = $"Login failed: Username or password is incorrect.";
            Console.WriteLine($"SignIn error: {ex.Message}");
            HasError = true;
        }
    }

    [RelayCommand]
    private async Task NavigateSignUp()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//signup");
    }

}
