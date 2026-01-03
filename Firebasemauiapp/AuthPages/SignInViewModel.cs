using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
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
            // Android Google Sign-In using Xamarin.GooglePlayServices.Auth
            // โหลด Web Client ID จาก admin-sdk.json
            var webClientId = await FirebaseConfig.Instance.GetWebClientIdAsync();
            Console.WriteLine($"[SignInViewModel] Web Client ID: {webClientId}");
            
            var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestServerAuthCode(webClientId)
                .RequestIdToken(webClientId)
                .RequestEmail()
                .Build();
            
            var context = Android.App.Application.Context;
            var googleSignInClient = Android.Gms.Auth.Api.SignIn.GoogleSignIn.GetClient(context, signInOptions);
            
            // Start the sign-in intent
            var signInIntent = googleSignInClient.SignInIntent;
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            
            Console.WriteLine($"[SignInViewModel] Activity: {(activity != null ? "available" : "null")}");
            
            if (activity != null)
            {
                Console.WriteLine("[SignInViewModel] Launching Google Sign-In activity");
                activity.StartActivityForResult(signInIntent, 9001);
                
                // Wait for OnActivityResult callback to provide the account
                Console.WriteLine("[SignInViewModel] Waiting for account selection...");
                var account = await GoogleSignInResultHandler.Instance.GetAccountAsync(TimeSpan.FromSeconds(5));
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
                        requestUri = "http://localhost",
                        returnIdpCredential = true,
                        returnSecureToken = true
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(requestData);
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
                    var doc = System.Text.Json.JsonDocument.Parse(resultJson);
                    var firebaseToken = doc.RootElement.GetProperty("idToken").GetString();
                    var firebaseUid = doc.RootElement.GetProperty("localId").GetString();
                    
                    Console.WriteLine($"[SignInViewModel] Got Firebase UID: {firebaseUid}");
                    
                    // Store the token for future use
                    OnPropertyChanged(nameof(Username));

                    if (!string.IsNullOrWhiteSpace(firebaseUid))
                    {
                        Preferences.Set("AUTH_UID", firebaseUid);
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
                            
                            // Show welcome message for new users
                            if (Shell.Current != null)
                                await Shell.Current.DisplayAlertAsync("Welcome!", "Your account has been created successfully.", "OK");
                        }
                        else
                        {
                            Console.WriteLine("[SignInViewModel] User document already exists, updating last active...");
                            // Update existing user's last active time
                            await userDocRef.UpdateAsync(new Dictionary<string, object>
                            {
                                { "lastActiveAt", now },
                                { "email", account.Email ?? "" }
                            });
                        }

                        Console.WriteLine("[SignInViewModel] Navigating to starter page...");
                        if (Shell.Current != null)
                            await Shell.Current.GoToAsync("//main/starter");
                    }
                    else
                    {
                        Console.WriteLine("[SignInViewModel] Firebase UID is empty");
                        if (Shell.Current != null)
                            await Shell.Current.DisplayAlertAsync("Error", "Google Sign In failed: Could not get user ID", "OK");
                    }
                }
                else
                {
                    Console.WriteLine("[SignInViewModel] No account selected or no IdToken");
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlertAsync("Error", "Google Sign In failed: No account selected. Please try again.", "OK");
                }
            }
            else
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlertAsync("Error", "Google Sign In failed: Activity not available", "OK");
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
