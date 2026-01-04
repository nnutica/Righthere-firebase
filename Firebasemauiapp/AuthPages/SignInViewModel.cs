using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Firebasemauiapp.Config;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using System.Text.Json;

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
                Preferences.Remove("IS_GOOGLE_USER"); // Clear Google flag for email/password login
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

            // Clear error and navigate to Starter after successful login
            ErrorMessage = string.Empty;
            HasError = false;
            
            // ✅ Load user into UserService
            await UserService.Instance.LoadUserAsync();
            
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//starter");
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
                .RequestIdToken(webClientId)
                .RequestEmail()
                .Build();

            var context = Android.App.Application.Context;
            var googleSignInClient = Android.Gms.Auth.Api.SignIn.GoogleSignIn.GetClient(context, signInOptions);

            var signInIntent = googleSignInClient.SignInIntent;
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (activity != null)
            {
                Console.WriteLine("[SignInViewModel] Launching Google Sign-In activity");
                activity.StartActivityForResult(signInIntent, 9001);

                var account = await GoogleSignInResultHandler.Instance.GetAccountAsync(TimeSpan.FromSeconds(60));
                Console.WriteLine($"[SignInViewModel] Received account: {(account != null ? account.Email : "null")}");

                if (account != null && !string.IsNullOrEmpty(account.IdToken))
                {
                    var idToken = account.IdToken;
                    Console.WriteLine("[SignInViewModel] IdToken received, exchanging for Firebase token...");

                    // Exchange Google ID token for Firebase auth using REST API
                    using var httpClient = new System.Net.Http.HttpClient();
                    var requestData = new
                    {
                        postBody = $"id_token={idToken}&providerId=google.com",
                        requestUri = "https://righthere-backend.firebaseapp.com",
                        returnIdpCredential = true,
                        returnSecureToken = true
                    };

                    var json = JsonSerializer.Serialize(requestData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(
                        $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key=AIzaSyCtqanoTU24UXz82KyZI8phmYae09sIx5U",
                        content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[SignInViewModel] Firebase exchange failed: {error}");
                        throw new Exception($"Google sign-in failed: {error}");
                    }

                    Console.WriteLine("[SignInViewModel] Firebase token exchange successful");
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(resultJson);
                    var firebaseIdToken = doc.RootElement.GetProperty("idToken").GetString();
                    var firebaseRefreshToken = doc.RootElement.GetProperty("refreshToken").GetString();
                    var firebaseUid = doc.RootElement.GetProperty("localId").GetString();
                    var firebaseEmail = doc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : account.Email;
                    var firebaseDisplayName = doc.RootElement.TryGetProperty("displayName", out var nameProp) ? nameProp.GetString() : account.DisplayName;

                    Console.WriteLine($"[SignInViewModel] Got Firebase UID: {firebaseUid}");

                    // ✅ Save Google Sign-In session manually using SecureStorage
                    await SecureStorage.SetAsync("GOOGLE_ID_TOKEN", firebaseIdToken ?? "");
                    await SecureStorage.SetAsync("GOOGLE_REFRESH_TOKEN", firebaseRefreshToken ?? "");
                    await SecureStorage.SetAsync("GOOGLE_UID", firebaseUid ?? "");
                    await SecureStorage.SetAsync("GOOGLE_EMAIL", firebaseEmail ?? "");
                    await SecureStorage.SetAsync("GOOGLE_DISPLAY_NAME", firebaseDisplayName ?? "");
                    
                    Preferences.Set("AUTH_UID", firebaseUid);
                    Preferences.Set("IS_GOOGLE_USER", true); // Mark as Google user

                    Console.WriteLine("[SignInViewModel] Google session saved successfully");

                    if (!string.IsNullOrWhiteSpace(firebaseUid))
                    {
                        var db = await _firestoreService.GetDatabaseAsync();
                        var userDocRef = db.Collection("users").Document(firebaseUid);
                        var snapshot = await userDocRef.GetSnapshotAsync();
                        var now = Timestamp.FromDateTime(DateTime.UtcNow);

                        // Check if user document exists - if not, create it (auto sign up)
                        if (!snapshot.Exists)
                        {
                            Console.WriteLine("[SignInViewModel] User document doesn't exist, creating...");
                            var displayName = account.DisplayName ?? account.Email ?? "Google User";
                            var payload = new Dictionary<string, object>
                            {
                                { "uid", firebaseUid },
                                { "email", account.Email ?? "" },
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
                            Console.WriteLine("[SignInViewModel] User document created successfully");

                            if (Shell.Current != null)
                                await Shell.Current.DisplayAlert("Welcome!", "Your account has been created successfully.", "OK");
                        }
                        else
                        {
                            Console.WriteLine("[SignInViewModel] User document already exists, updating last active...");
                            await userDocRef.UpdateAsync(new Dictionary<string, object>
                            {
                                { "lastActiveAt", now },
                                { "email", account.Email ?? "" }
                            });
                        }

                        Console.WriteLine("[SignInViewModel] Navigating to starter page...");

                        // ✅ Load user into UserService
                        await UserService.Instance.LoadUserAsync();
                        
                        if (Shell.Current != null)
                            await Shell.Current.GoToAsync("//starter");
                    }
                    else
                    {
                        Console.WriteLine("[SignInViewModel] Firebase UID is empty");
                        if (Shell.Current != null)
                            await Shell.Current.DisplayAlert("Error", "Google Sign In failed: Could not get user ID", "OK");
                    }
                }
                else
                {
                    Console.WriteLine("[SignInViewModel] No account selected or no IdToken");
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlert("Error", "Google Sign In failed: No account selected. Please try again.", "OK");
                }
            }
            else
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Google Sign In failed: Activity not available", "OK");
            }
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