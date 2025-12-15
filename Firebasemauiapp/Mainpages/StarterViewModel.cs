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
            // Sign out from Firebase client
            _authClient.SignOut();

            // Clear any locally persisted tokens/preferences (best-effort)
            try { SecureStorage.Default.RemoveAll(); } catch { /* ignore */ }
            try { Preferences.Default.Clear(); } catch { /* ignore */ }

            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//signin");
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("Error", $"Logout failed: {ex.Message}", "OK");
        }
    }
}
