using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Firebasemauiapp.Config;
using Google.Cloud.Firestore;
using System.Collections.Generic;
#if __ANDROID__
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Auth.Api;
using Android.Gms.Common;
#endif

namespace Firebasemauiapp.Pages;

public partial class SignUpViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isTermsAccepted;

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible = false;

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    public SignUpViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;
    }

    [RelayCommand]
    private async Task SignUp()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            if (!string.Equals(Password, ConfirmPassword))
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            if (!IsTermsAccepted)
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Terms", "Please agree to the Terms & Privacy Policy.", "OK");
                return;
            }

            if (Password.Length < 6)
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Weak password", "Password should be at least 6 characters.", "OK");
                return;
            }

            var credential = await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

            var user = credential?.User;
            var uid = user?.Uid;

            // Immediately sign out to avoid auth router jumping to Starter
            _authClient.SignOut();

            if (!string.IsNullOrWhiteSpace(uid))
            {
                var db = await _firestoreService.GetDatabaseAsync();
                var userDoc = db.Collection("users").Document(uid);
                var payload = new Dictionary<string, object>
                {
                    { "uid", uid },
                    { "email", Email },
                    { "username", Username },
                    { "coin", 0 },
                    {"role", "user" },
                    {"status", "active"},
                    { "inventory", new List<string>() },
                    { "currentPlant", "empty.png" },
                    { "currentPot", "pot.png" },
                    { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };
                await userDoc.SetAsync(payload, SetOptions.Overwrite);
            }

            // Success message then navigate to Sign In
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert("Success", "Your account has been created. Please sign in.", "OK");
                await Shell.Current.GoToAsync("//signin");
            }
        }
        catch (Exception ex)
        {
            // Handle registration error
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Registration failed: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task NavigateSignIn()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//signin");
    }

    [RelayCommand]
    private async Task SignUpWithGoogle()
    {
        try
        {
            Console.WriteLine("[SignUpViewModel] Starting Google Sign-Up");

#if __ANDROID__
            // Android Google Sign-In using Xamarin.GooglePlayServices.Auth
            // โหลด Web Client ID จาก admin-sdk.json
            var webClientId = await FirebaseConfig.Instance.GetWebClientIdAsync();
            Console.WriteLine($"[SignUpViewModel] Web Client ID: {webClientId}");
            
            var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken(webClientId)
                .RequestEmail()
                .Build();
            
            var context = Android.App.Application.Context;
            var googleSignInClient = GoogleSignIn.GetClient(context, signInOptions);
            
            var signInIntent = googleSignInClient.SignInIntent;
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            
            if (activity != null)
            {
                Console.WriteLine("[SignUpViewModel] Launching Google Sign-In activity");
                activity.StartActivityForResult(signInIntent, 9001);
                
                var account = await GoogleSignInResultHandler.Instance.GetAccountAsync(TimeSpan.FromSeconds(60));
                Console.WriteLine($"[SignUpViewModel] Received account: {(account != null ? account.Email : "null")}");
                
                if (account != null && !string.IsNullOrEmpty(account.IdToken))
                {
                    var idToken = account.IdToken;
                    Console.WriteLine("[SignUpViewModel] IdToken received, exchanging for Firebase token...");
                    
                    // Exchange Google ID token for Firebase auth using REST API
                    using var httpClient = new System.Net.Http.HttpClient();
                    var requestData = new
                    {
                        postBody = $"id_token={idToken}&providerId=google.com",
                        requestUri = "https://righthere-backend.firebaseapp.com",
                        returnIdpCredential = true,
                        returnSecureToken = true
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(
                        $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key=AIzaSyANQwJ8crU43o1ytP3X5RQrmJ-_eZX1pu0",
                        content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[SignUpViewModel] Firebase exchange failed: {error}");
                        throw new Exception($"Google sign-in failed: {error}");
                    }
                    
                    Console.WriteLine("[SignUpViewModel] Firebase token exchange successful");
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(resultJson);
                    var firebaseIdToken = doc.RootElement.GetProperty("idToken").GetString();
                    var firebaseRefreshToken = doc.RootElement.GetProperty("refreshToken").GetString();
                    var firebaseUid = doc.RootElement.GetProperty("localId").GetString();
                    var firebaseEmail = doc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : account.Email;
                    var firebaseDisplayName = doc.RootElement.TryGetProperty("displayName", out var nameProp) ? nameProp.GetString() : account.DisplayName;

                    Console.WriteLine($"[SignUpViewModel] Got Firebase UID: {firebaseUid}");

                    // ✅ Save Google Sign-In session manually using SecureStorage
                    await SecureStorage.SetAsync("GOOGLE_ID_TOKEN", firebaseIdToken ?? "");
                    await SecureStorage.SetAsync("GOOGLE_REFRESH_TOKEN", firebaseRefreshToken ?? "");
                    await SecureStorage.SetAsync("GOOGLE_UID", firebaseUid ?? "");
                    await SecureStorage.SetAsync("GOOGLE_EMAIL", firebaseEmail ?? "");
                    await SecureStorage.SetAsync("GOOGLE_DISPLAY_NAME", firebaseDisplayName ?? "");
                    
                    Preferences.Set("AUTH_UID", firebaseUid);
                    Preferences.Set("IS_GOOGLE_USER", true);

                    Console.WriteLine("[SignUpViewModel] Google session saved successfully");

                    if (!string.IsNullOrWhiteSpace(firebaseUid))
                    {
                        var db = await _firestoreService.GetDatabaseAsync();
                        var userDocRef = db.Collection("users").Document(firebaseUid);
                        var snapshot = await userDocRef.GetSnapshotAsync();
                        var now = Timestamp.FromDateTime(DateTime.UtcNow);

                        if (!snapshot.Exists)
                        {
                            Console.WriteLine("[SignUpViewModel] User document doesn't exist, creating...");
                            var googleUsername = account.DisplayName ?? account.Email ?? "Google User";
                            var payload = new Dictionary<string, object>
                            {
                                { "uid", firebaseUid },
                                { "email", account.Email ?? "" },
                                { "username", googleUsername },
                                { "coin", 0 },
                                { "role", "user" },
                                { "status", "active" },
                                { "inventory", new List<string>() },
                                { "currentPlant", "empty.png" },
                                { "currentPot", "pot.png" },
                                { "createdAt", now },
                                { "lastActiveAt", now }
                            };
                            await userDocRef.SetAsync(payload, SetOptions.Overwrite);
                            Console.WriteLine("[SignUpViewModel] User document created successfully");
                            
                            if (Shell.Current != null)
                                await Shell.Current.DisplayAlert("Welcome!", "Your account has been created successfully.", "OK");
                        }
                        else
                        {
                            Console.WriteLine("[SignUpViewModel] User document already exists, updating last active...");
                            await userDocRef.UpdateAsync(new Dictionary<string, object>
                            {
                                { "lastActiveAt", now },
                                { "email", account.Email ?? "" }
                            });
                        }

                        Console.WriteLine("[SignUpViewModel] Loading user into UserService...");
                        // ✅ Load user into UserService
                        await UserService.Instance.LoadUserAsync();
                        
                        Console.WriteLine("[SignUpViewModel] Navigating to starter page...");
                        if (Shell.Current != null)
                            await Shell.Current.GoToAsync("//starter");
                    }
                    else
                    {
                        Console.WriteLine("[SignUpViewModel] Firebase UID is empty");
                        if (Shell.Current != null)
                            await Shell.Current.DisplayAlert("Error", "Google Sign Up failed: Could not get user ID", "OK");
                    }
                }
                else
                {
                    Console.WriteLine("[SignUpViewModel] No account selected or no IdToken");
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlert("Error", "Google Sign Up failed: No account selected. Please try again.", "OK");
                }
            }
            else
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Error", "Google Sign Up failed: Activity not available", "OK");
            }
#else
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", "Google Sign Up is only available on Android", "OK");
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignUpViewModel] Google Sign-Up error: {ex.Message}\n{ex.StackTrace}");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Google sign up failed: {ex.Message}", "OK");
        }
    }

}