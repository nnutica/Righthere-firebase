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

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isLightMode = true;

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SetLightTheme()
    {
        IsLightMode = true;
        if (Application.Current != null) Application.Current.UserAppTheme = AppTheme.Light;
    }

    [RelayCommand]
    private void SetDarkTheme()
    {
        IsLightMode = false;
        if (Application.Current != null) Application.Current.UserAppTheme = AppTheme.Dark;
    }

    [ObservableProperty]
    private string _username = "Friend";

    public string WelcomeMessage => Username;

    // ✅ Change Password Properties
    public bool IsGoogleUser => UserService.Instance.IsGoogleUser;

    [ObservableProperty]
    private bool _isChangePasswordOpen;

    [ObservableProperty]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    public StarterViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        _firestoreService = firestoreService;

        // Load username asynchronously
        _ = LoadUsernameAsync();
    }

    // ... (Existing GoDiary Command) ...

    [RelayCommand]
    private void OpenChangePassword()
    {
        OldPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        IsSettingsOpen = false; // Close settings menu
        IsChangePasswordOpen = true;
    }

    [RelayCommand]
    private void CloseChangePassword()
    {
        IsChangePasswordOpen = false;
        IsSettingsOpen = true; // Re-open settings menu
    }

    [RelayCommand]
    private async Task SubmitChangePassword()
    {
        if (string.IsNullOrWhiteSpace(OldPassword) || 
            string.IsNullOrWhiteSpace(NewPassword) || 
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            await Shell.Current.DisplayAlert("Error", "Please fill in all fields.", "OK");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            await Shell.Current.DisplayAlert("Error", "New passwords do not match.", "OK");
            return;
        }

        if (OldPassword == NewPassword)
        {
            await Shell.Current.DisplayAlert("Error", "New password cannot be the same as the old password.", "OK");
            return;
        }

        try
        {
            IsLoading = true;
            // 1. Re-authenticate
            var email = UserService.Instance.Email;
            if (string.IsNullOrEmpty(email)) throw new Exception("No email found.");

            await _authClient.SignInWithEmailAndPasswordAsync(email, OldPassword);
            
            // 2. Change Password
            await _authClient.User.ChangePasswordAsync(NewPassword);

            IsChangePasswordOpen = false;
            IsLoading = false;
            await Shell.Current.DisplayAlert("Success", "Password changed successfully.", "OK");
        }
        catch (Exception ex)
        {
            IsLoading = false;
            await Shell.Current.DisplayAlert("Error", $"Failed to change password. Please check your old password.\n{ex.Message}", "OK");
        }
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

    private async Task LoadUsernameAsync()
    {
        try
        {
            IsLoading = true;
            
            // Check if user is already loaded in UserService
            if (!UserService.Instance.IsLoaded)
            {
                await UserService.Instance.LoadUserAsync();
            }

            // Sync from UserService
            Username = UserService.Instance.Username;
            OnPropertyChanged(nameof(IsGoogleUser));
            System.Diagnostics.Debug.WriteLine($"[StarterViewModel] Loaded username via UserService: {Username}");

            // Load Plant and Pot
             if (!string.IsNullOrEmpty(UserService.Instance.Uid))
            {
                var (plant, pot) = await _firestoreService.GetPlantAndPotAsync(UserService.Instance.Uid);
                CurrentPlantImage = plant;
                CurrentPotImage = pot;
                System.Diagnostics.Debug.WriteLine($"[StarterViewModel] Loaded plant: {plant}, pot: {pot}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StarterViewModel] Error loading user data: {ex.Message}");
            Username = "Friend";
            CurrentPlantImage = "plant.png"; // Default fallback
            CurrentPotImage = "pot.png"; // Default fallback
        }
        finally
        {
            IsLoading = false;
        }
    }

    [ObservableProperty]
    private string _currentPlantImage = "plant.png"; // Default

    [ObservableProperty]
    private string _currentPotImage = "pot.png"; // Default

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

    public async Task RefreshUserDataAsync()
    {
        // Force refresh capability
        await UserService.Instance.RefreshAsync();
        Username = UserService.Instance.Username;
        
        // Also refresh plant/pot
        if (!string.IsNullOrEmpty(UserService.Instance.Uid))
        {
            var (plant, pot) = await _firestoreService.GetPlantAndPotAsync(UserService.Instance.Uid);
            CurrentPlantImage = plant;
            CurrentPotImage = pot;
        }
    }

    [RelayCommand]
    private async Task LogOut()
    {
        try
        {
            Console.WriteLine("[StarterViewModel] Starting logout...");

            // 1. Clear local data FIRST to prevent race condition with AuthRoutingService
            
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

            // ? Clear any locally persisted tokens/preferences
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

            // 2. Sign out from Firebase client LAST
            // This triggers AuthStateChanged, but now local data is gones so AuthRoutingService should route to //signin
            try
            {
                _authClient?.SignOut();
                Console.WriteLine("[StarterViewModel] Firebase sign out completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StarterViewModel] Firebase sign out error: {ex.Message}");
            }

            Console.WriteLine("[StarterViewModel] Navigating to signin...");
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//signin");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StarterViewModel] Logout error: {ex.Message}\n{ex.StackTrace}");
            
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
        }
    }

    // ==========================================
    // DELETE ACCOUNT LOGIC
    // ==========================================

    [ObservableProperty]
    private bool _isDeleteConfirmationOpen;

    [ObservableProperty]
    private bool _isFinalDeleteConfirmationOpen;

    [ObservableProperty]
    private string _deleteConfirmationInput = "";

    [ObservableProperty]
    private string _deleteErrorMessage = "";

    [ObservableProperty]
    private bool _hasDeleteError;

    [RelayCommand]
    private void ShowDeleteConfirmation()
    {
        // Close settings, open first confirmation
        IsSettingsOpen = false;
        IsDeleteConfirmationOpen = true;
    }

    [RelayCommand]
    private void CloseDeleteConfirmation()
    {
        IsDeleteConfirmationOpen = false;
        IsFinalDeleteConfirmationOpen = false;
        DeleteConfirmationInput = "";
        DeleteErrorMessage = "";
        HasDeleteError = false;
    }

    [RelayCommand]
    private void ShowFinalDeleteConfirmation()
    {
        IsDeleteConfirmationOpen = false;
        IsFinalDeleteConfirmationOpen = true;
        DeleteConfirmationInput = ""; // Reset input
        HasDeleteError = false;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAccount()
    {
        if (string.IsNullOrWhiteSpace(DeleteConfirmationInput) || 
            !DeleteConfirmationInput.Trim().Equals("delete", StringComparison.OrdinalIgnoreCase))
        {
            DeleteErrorMessage = "Incorrect. Please try again";
            HasDeleteError = true;
            return;
        }

        // Correct input, proceed to delete
        try
        {
            IsLoading = true;
            HasDeleteError = false;

            // Close popup immediately or keep it showing loading state? 
            // Better to show loading.
            
            await UserService.Instance.DeleteAccountAsync();

            // After successful deletion, navigate to sign in
            // Use MainThread to ensure UI update
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsFinalDeleteConfirmationOpen = false;
                IsSettingsOpen = false;
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//signin");
            });
        }
        catch (Exception ex)
        {
            DeleteErrorMessage = "Error deleting account. Please try again later.";
            HasDeleteError = true;
            System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [ObservableProperty]
    private bool _isLogoutConfirmationOpen;

    [RelayCommand]
    private void ShowLogoutConfirmation()
    {
        IsSettingsOpen = false;
        IsLogoutConfirmationOpen = true;
    }

    [RelayCommand]
    private void CloseLogoutConfirmation()
    {
        IsLogoutConfirmationOpen = false;
    }
}
