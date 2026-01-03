using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;

namespace Firebasemauiapp.Mainpages;

public partial class MoodViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;

    [ObservableProperty]
    private string _username = "Daniel";

    [ObservableProperty]
    private ObservableCollection<MoodOption> _moods = new();

    [ObservableProperty]
    private MoodOption _selectedMood;

    [ObservableProperty]
    private bool _isNextEnabled;

    public MoodViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;
        LoadMoods();
        InitializeUserAsync();
    }

    private void InitializeUserAsync()
    {
        // Fire and forget, but log if it fails
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await LoadUserAsync();
        });
    }

    partial void OnSelectedMoodChanged(MoodOption value)
    {
        IsNextEnabled = value != null;
    }

    private async Task LoadUserAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MoodViewModel] LoadUserAsync starting...");

            // Try to get username from Preferences first (set during sign-in)
            var savedUsername = Preferences.Get("USER_DISPLAY_NAME", string.Empty);
            if (!string.IsNullOrWhiteSpace(savedUsername))
            {
                System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Found saved username in Preferences: {savedUsername}");
                Username = savedUsername;
                return;
            }

            // If not in Preferences, try Firebase Auth
            var user = _authClient.User;
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Firebase User: {(user != null ? "exists" : "null")}");

            if (user != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MoodViewModel] User UID: {user.Uid}");
                System.Diagnostics.Debug.WriteLine($"[MoodViewModel] User DisplayName: {user.Info?.DisplayName}");
                System.Diagnostics.Debug.WriteLine($"[MoodViewModel] User Email: {user.Info?.Email}");

                var display = user.Info?.DisplayName;
                if (string.IsNullOrWhiteSpace(display))
                {
                    var email = user.Info?.Email;
                    display = !string.IsNullOrWhiteSpace(email) && email.Contains('@')
                        ? email.Split('@')[0]
                        : "Friend";
                    System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Display name from email: {display}");
                }

                // ถ้ายังไม่มี display name และมี UID ให้ดึงจาก Firestore
                if (string.IsNullOrWhiteSpace(display) && !string.IsNullOrWhiteSpace(user.Uid))
                {
                    System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Fetching username from Firestore for UID: {user.Uid}");
                    var firestoreUsername = await GetUsernameFromFirestoreAsync(user.Uid);
                    System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Firestore username: {firestoreUsername}");
                    display = firestoreUsername ?? "Friend";
                }

                System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Setting username to: {display}");
                Username = display;

                // Save to Preferences for future use
                Preferences.Set("USER_DISPLAY_NAME", display);
            }
            else
            {
                // Try to get username from Firestore using UID from Preferences
                var uid = Preferences.Get("AUTH_UID", string.Empty);
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Firebase User is null, but found UID in Preferences: {uid}");
                    System.Diagnostics.Debug.WriteLine("[MoodViewModel] Fetching username from Firestore...");
                    var firestoreUsername = await GetUsernameFromFirestoreAsync(uid);
                    System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Firestore username: {firestoreUsername}");

                    if (!string.IsNullOrWhiteSpace(firestoreUsername))
                    {
                        Username = firestoreUsername;
                        Preferences.Set("USER_DISPLAY_NAME", firestoreUsername);
                        return;
                    }
                }
                System.Diagnostics.Debug.WriteLine("[MoodViewModel] No user found, keeping default username");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] LoadUserAsync error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Stack trace: {ex.StackTrace}");
        }
    }

    private async Task<string> GetUsernameFromFirestoreAsync(string uid)
    {
        try
        {
            var db = await _firestoreService.GetDatabaseAsync();
            var userDocRef = db.Collection("users").Document(uid);
            var snapshot = await userDocRef.GetSnapshotAsync();

            if (snapshot.Exists && snapshot.TryGetValue<string>("username", out var username))
            {
                return username ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Error fetching username from Firestore: {ex.Message}");
        }
        return string.Empty;
    }

    private void LoadMoods()
    {
        // Supply image asset filenames via the Icon property (Resources/Images)
        Moods = new ObservableCollection<MoodOption>
        {
            new(name: "Happiness", emoji: string.Empty, icon: "happiness.png"),
            new(name: "Love",      emoji: string.Empty, icon: "love.png"),
            new(name: "Angry",     emoji: string.Empty, icon: "angry.png"),
            new(name: "Disgust",  emoji: string.Empty, icon: "disgust.png"),
            new(name: "Sadness",   emoji: string.Empty, icon: "sadness.png"),
            new(name: "Fear",      emoji: string.Empty, icon: "fear.png")
        };
    }

    [RelayCommand]
    private async Task Next()
    {
        if (SelectedMood == null)
        {
            System.Diagnostics.Debug.WriteLine("[MoodViewModel] SelectedMood is null!");
            return;
        }
        if (Shell.Current == null)
        {
            System.Diagnostics.Debug.WriteLine("[MoodViewModel] Shell.Current is null!");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Navigating with Mood: {SelectedMood.Name}, Icon: {SelectedMood.Icon}");

            var navParams = new Dictionary<string, object>
            {
                ["Mood"] = SelectedMood,
                ["Username"] = Username
            };

            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] NavParams count: {navParams.Count}");

            // Navigate using relative route (registered in AppShell.xaml.cs)
            await Shell.Current.GoToAsync("levelmood", false, navParams);

            System.Diagnostics.Debug.WriteLine("[MoodViewModel] Navigation completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Navigation error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MoodViewModel] Stack trace: {ex.StackTrace}");
        }
    }
}
