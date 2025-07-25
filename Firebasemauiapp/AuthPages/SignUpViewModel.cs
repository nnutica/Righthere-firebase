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
        await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

        await Shell.Current.GoToAsync("//SignIn");
    }

    [RelayCommand]
    private async Task NavigateSignIn()
    {
        await Shell.Current.GoToAsync("//signin");
    }

}
