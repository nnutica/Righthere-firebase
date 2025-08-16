using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;

namespace Firebasemauiapp.Pages;

public partial class SignUpViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _email;

    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;

    public SignUpViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
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

            await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);
            
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
