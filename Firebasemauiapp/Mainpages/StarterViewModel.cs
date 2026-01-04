using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel; // For MainThread
using System.Collections.ObjectModel;

namespace Firebasemauiapp.Mainpages;

public partial class StarterViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirestoreService _firestoreService;

    public StarterViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        _firestoreService = firestoreService;
    }

    [RelayCommand]
    private async Task GoDiary()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//diary");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoForest()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//community");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoQuest()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//quest");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoStore()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//store");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoDashboard()
    {
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//dashboard");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LogOut()
    {
        try
        {
            Console.WriteLine("[StarterViewModel] Starting logout...");
            
            // ? Sign out from Firebase client (with null check)
            try
            {
                _authClient?.SignOut();
                Console.WriteLine("[StarterViewModel] Firebase sign out completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StarterViewModel] Firebase sign out error: {ex.Message}");
            }

            // ? Sign out from Google Auth Service
            try
            {
                await GoogleAuthService.Instance.SignOutAsync();
                Console.WriteLine("[StarterViewModel] Google sign out completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StarterViewModel] Google sign out error: {ex.Message}");
            }
            
            // ? Clear UserService
            try
            {
                UserService.Instance.Clear();
                Console.WriteLine("[StarterViewModel] UserService cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StarterViewModel] UserService clear error: {ex.Message}");
            }

            // ? Clear any locally persisted tokens/preferences (best-effort)
            try 
            { 
                SecureStorage.Default.RemoveAll(); 
                Console.WriteLine("[StarterViewModel] SecureStorage cleared");
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"[StarterViewModel] SecureStorage clear error: {ex.Message}");
            }
            
            try 
            { 
                Preferences.Default.Clear(); 
                Console.WriteLine("[StarterViewModel] Preferences cleared");
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"[StarterViewModel] Preferences clear error: {ex.Message}");
            }

            Console.WriteLine("[StarterViewModel] Navigating to signin...");
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//signin");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StarterViewModel] Logout error: {ex.Message}\n{ex.StackTrace}");
            
            // ? ?????? DisplayAlertAsync ???? DisplayAlert
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
        }
    }
}
