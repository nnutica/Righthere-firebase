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
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Username))
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            var credential = await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

            var user = credential?.User;
            var uid = user?.Uid;
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
            
            // Navigate to sign in page after successful registration
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//signin");
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
