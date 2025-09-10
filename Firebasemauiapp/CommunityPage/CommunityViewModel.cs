using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using CommunityToolkit.Maui.Views;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.CommunityPage;

public partial class CommunityViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly PostDatabase _postDb;

    private string _userName = "Guest";
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => SetProperty(ref _isLoggedIn, value);
    }

    public CommunityViewModel(FirebaseAuthClient authClient, PostDatabase postDb)
    {
        _authClient = authClient;
        _postDb = postDb;
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

    private Task ShowPostPopup() => Task.CompletedTask;
}
