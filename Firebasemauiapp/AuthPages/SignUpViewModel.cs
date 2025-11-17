using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace Firebasemauiapp.Pages;

public partial class SignUpViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isTermsAccepted;

    public SignUpViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;
    }

    [RelayCommand]
    private async Task SignUp()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            if (!string.Equals(Password, ConfirmPassword))
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            if (!IsTermsAccepted)
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Terms", "Please agree to the Terms & Privacy Policy.", "OK");
                return;
            }

            if (Password.Length < 6)
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Weak password", "Password should be at least 6 characters.", "OK");
                return;
            }

            var credential = await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

            var user = credential?.User;
            var uid = user?.Uid;

            // Immediately sign out to avoid auth router jumping to Starter
            _authClient.SignOut();

            if (!string.IsNullOrWhiteSpace(uid))
            {
                var db = await _firestoreService.GetDatabaseAsync();
                var userDoc = db.Collection("users").Document(uid);
                var payload = new Dictionary<string, object>
                {
                    { "uid", uid },
                    { "email", Email },
                    { "username", Username },
                    { "coin", 0 },
                    {"role", "user" },
                    { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };
                await userDoc.SetAsync(payload, SetOptions.Overwrite);
            }

            // Success message then navigate to Sign In
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert("Success", "Your account has been created. Please sign in.", "OK");
                await Shell.Current.GoToAsync("//signin");
            }
        }
        catch (Exception ex)
        {
            // Handle registration error
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Registration failed: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task NavigateSignIn()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//signin");
    }

}
