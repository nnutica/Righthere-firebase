using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using CommunityToolkit.Maui.Views;

namespace Firebasemauiapp.CommunityPage;

public partial class CommunityViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _userName = "Guest";

    [ObservableProperty]
    private bool _isLoggedIn;

    public CommunityViewModel(FirebaseAuthClient authClient)
    {
        _authClient = authClient;
        LoadUserInfoCommand = new AsyncRelayCommand(LoadUserInfo);
        ShowPostPopupCommand = new AsyncRelayCommand(ShowPostPopup);
    }

    public IAsyncRelayCommand LoadUserInfoCommand { get; }
    public IAsyncRelayCommand ShowPostPopupCommand { get; }

    private Task LoadUserInfo()
    {
        var user = _authClient.User;
        if (user != null)
        {
            UserName = user.Info.Email ?? "Unknown User";
            IsLoggedIn = true;
        }
        else
        {
            UserName = "Guest";
            IsLoggedIn = false;
        }
        return Task.CompletedTask;
    }

    private async Task ShowPostPopup()
    {
        // Temporary workaround using DisplayAlert until ShowPopupAsync extension is available
        var result = await Shell.Current.DisplayPromptAsync("Community Post", 
            $"User: {UserName}\n\nWhat's on your mind?", 
            "Post", "Cancel", 
            placeholder: "Share your thoughts...",
            maxLength: 500);
            
        if (!string.IsNullOrWhiteSpace(result))
        {
            // TODO: Save post to backend
            await Shell.Current.DisplayAlert("Success", "Your post has been shared!", "OK");
        }
    }
}
