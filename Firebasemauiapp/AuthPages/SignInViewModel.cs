using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;

namespace Firebasemauiapp.Pages;

public partial class SignInViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public string? Username => _authClient.User?.Info?.DisplayName;


    public SignInViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
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

            await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
            OnPropertyChanged(nameof(Username));

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
