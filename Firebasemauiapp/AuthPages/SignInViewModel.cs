using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebasemauiapp.Services;
using Firebasemauiapp.Config;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
#if __ANDROID__
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Auth.Api;
using Android.Gms.Common;
#endif

namespace Firebasemauiapp.Pages;

public partial class SignInViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    public string? Username => _authClient.User?.Info?.DisplayName;

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }


    public SignInViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;
    }

    [RelayCommand]
    private async Task SignIn()
    {
        try
        {
            // Clear previous error
            ErrorMessage = string.Empty;
            HasError = false;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both email and password.";
                HasError = true;
                return;
            }

            var result = await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
            OnPropertyChanged(nameof(Username));

            var uid = result?.User?.Uid;
            if (!string.IsNullOrWhiteSpace(uid))
            {
                Preferences.Set("AUTH_UID", uid);
                var db = await _firestoreService.GetDatabaseAsync();
                var userDocRef = db.Collection("users").Document(uid);
                var snapshot = await userDocRef.GetSnapshotAsync();
                var now = Timestamp.FromDateTime(DateTime.UtcNow);
                if (!snapshot.Exists)
                {
                    var displayName = result!.User!.Info?.DisplayName ?? string.Empty;
                    var payload = new Dictionary<string, object>
                    {
                        { "uid", uid },
                        { "email", Email },
                        { "username", string.IsNullOrWhiteSpace(displayName) ? Email : displayName },
                        {"role", "user" },
                        { "coin", 0 },
                        {"status", "active"},
                        { "inventory", new List<string>() },
                        { "currentPlant", "empty.png" },
                        { "currentPot", "pot.png" },
                        { "createdAt", now },
                        { "lastActiveAt", now }
                    };
                    await userDocRef.SetAsync(payload, SetOptions.Overwrite);
                }
                else
                {
                    await userDocRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "lastActiveAt", now },
                        { "email", Email }
                    });
                }
            }

            // Clear error and navigate to Main TabBar after successful login
            ErrorMessage = string.Empty;
            HasError = false;
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//main/starter");
        }
        catch (Exception ex)
        {
            // Handle login error with inline message
            ErrorMessage = $"Login failed: Username or password is incorrect.";
            Console.WriteLine($"SignIn error: {ex.Message}");
            HasError = true;
        }
    }

    [RelayCommand]
    private async Task NavigateSignUp()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//signup");
    }

    [RelayCommand]

    private async Task SignInWithGoogle()
    {
        try
        {
            Console.WriteLine("[SignInViewModel] Starting Google Sign-In");
            ErrorMessage = string.Empty;
            HasError = false;

#if __ANDROID__
        var webClientId = await FirebaseConfig.Instance.GetWebClientIdAsync();
        Console.WriteLine($"[SignInViewModel] Web Client ID: {webClientId}");

        var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            // ถ้าคุณไม่ได้ใช้ server auth code จริงๆ ตัดทิ้งได้
            //.RequestServerAuthCode(webClientId)
            .RequestIdToken(webClientId)
            .RequestEmail()
            .Build();

        var context = Android.App.Application.Context;
        var googleSignInClient = Android.Gms.Auth.Api.SignIn.GoogleSignIn.GetClient(context, signInOptions);

        // ล้าง signed-in account เพื่อให้ผู้ใช้เลือกแอคเคาท์ใหม่ทุกครั้ง
        Console.WriteLine("[SignInViewModel] Signing out from Google first to allow fresh account selection...");
        await googleSignInClient.SignOutAsync();

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) throw new Exception("Activity not available");

        activity.StartActivityForResult(googleSignInClient.SignInIntent, 9001);

        Console.WriteLine("[SignInViewModel] Waiting for account selection...");
        var account = await GoogleSignInResultHandler.Instance.GetAccountAsync(TimeSpan.FromSeconds(15));

        if (account == null) throw new Exception("No account selected");
        if (string.IsNullOrWhiteSpace(account.IdToken))
            throw new Exception("No IdToken returned (check Web Client ID + RequestIdToken)");

        Console.WriteLine($"[SignInViewModel] Got account: {account.Email}, IdToken length: {account.IdToken.Length}");
        
        // ✅ จุดสำคัญ: เอา IdToken มา Sign-in เข้า FirebaseAuthClient "จริง"
        Console.WriteLine("[SignInViewModel] Creating Google credential...");
        var credential = GoogleProvider.GetCredential(account.IdToken);
        
        Console.WriteLine("[SignInViewModel] Attempting Firebase sign-in with Google credential...");
        var result = await _authClient.SignInWithCredentialAsync(credential);

        Console.WriteLine($"[SignInViewModel] Firebase sign-in result - User: {result?.User?.Uid}");
        OnPropertyChanged(nameof(Username));

        var uid = result?.User?.Uid;
        if (string.IsNullOrWhiteSpace(uid))
            throw new Exception("Firebase UID is empty after Google sign-in");

        // --- ส่วนนี้คง behavior เดิมของคุณ: Preferences + Firestore doc ---
        Preferences.Set("AUTH_UID", uid);

        var db = await _firestoreService.GetDatabaseAsync();
        var userDocRef = db.Collection("users").Document(uid);
        var snapshot = await userDocRef.GetSnapshotAsync();
        var now = Timestamp.FromDateTime(DateTime.UtcNow);

        var emailValue = result?.User?.Info?.Email ?? account.Email ?? "";
        var displayName = result?.User?.Info?.DisplayName ?? account.DisplayName ?? account.Email ?? "Google User";

        // ถ้าไม่มี DisplayName ให้ตัดก่อน @
        if (string.IsNullOrWhiteSpace(result?.User?.Info?.DisplayName) &&
            !string.IsNullOrWhiteSpace(emailValue) && emailValue.Contains('@'))
        {
            displayName = emailValue.Split('@')[0];
        }

        if (!snapshot.Exists)
        {
            var payload = new Dictionary<string, object>
            {
                { "uid", uid },
                { "email", emailValue },
                { "username", displayName },
                { "role", "user" },
                { "coin", 0 },
                { "status", "active" },
                { "inventory", new List<string>() },
                { "currentPlant", "empty.png" },
                { "currentPot", "pot.png" },
                { "createdAt", now },
                { "lastActiveAt", now }
            };

            await userDocRef.SetAsync(payload, SetOptions.Overwrite);
            Preferences.Set("USER_DISPLAY_NAME", displayName);

            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("Welcome!", "Your account has been created successfully.", "OK");
        }
        else
        {
            if (snapshot.TryGetValue<string>("username", out var existingUsername))
                Preferences.Set("USER_DISPLAY_NAME", existingUsername);

            await userDocRef.UpdateAsync(new Dictionary<string, object>
            {
                { "lastActiveAt", now },
                { "email", emailValue }
            });
        }

        Console.WriteLine("[SignInViewModel] Navigating to starter page...");
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//starter");

#else
            ErrorMessage = "Google Sign In is only available on Android";
            HasError = true;
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignInViewModel] Google Sign-In error: {ex.Message}\n{ex.StackTrace}");
            ErrorMessage = $"Google sign in failed: {ex.Message}";
            HasError = true;
        }
    }

}
