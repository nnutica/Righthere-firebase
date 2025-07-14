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
            await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
            OnPropertyChanged(nameof(Username));

            // Navigate to Starter page after successful login
            await Shell.Current.GoToAsync("//starter");
        }
        catch (Exception ex)
        {
            // Handle login error
            await Shell.Current.DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
    }

    [RelayCommand]

    private async Task NavigateSignUp()
    {
        await Shell.Current.GoToAsync("//signup");
    }

}
