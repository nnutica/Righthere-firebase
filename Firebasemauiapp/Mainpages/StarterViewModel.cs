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

    private string _userName = "Guest";
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    private string _userEmail = string.Empty;
    public string UserEmail
    {
        get => _userEmail;
        set => SetProperty(ref _userEmail, value);
    }

    public StarterViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        _firestoreService = firestoreService;
        // React to auth state changes so UI updates after sign-in/sign-out.
        _authClient.AuthStateChanged += OnAuthStateChanged;
        RefreshUserInfo();
        _ = RefreshCoinAsync();
        _ = LoadDailyQuestsAsync();
    }

    [ObservableProperty]
    private bool _isMenuOpen;

    // Visual assets for the composed plant
    [ObservableProperty]
    private string _plantImage = "plant.png";

    [ObservableProperty]
    private string _potImage = "pot.png";

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }

    [RelayCommand]
    private void OpenFlyout()
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
    }

    [RelayCommand]
    private async Task GoWriteDiary()
    {
        Console.WriteLine("🔍 GoWriteDiary called!");
        try
        {
            Console.WriteLine($"🔍 User: {_authClient.User?.Uid ?? "null"}");
            if (_authClient.User == null)
            {
                await Shell.Current.DisplayAlert("Login required", "Please sign in again.", "OK");
                await Shell.Current.GoToAsync("//signin");
                return;
            }
            if (Shell.Current != null)
            {
                Console.WriteLine("🔍 Navigating to //main/diary");
                await Shell.Current.GoToAsync("//main/diary");
                Console.WriteLine("🔍 Navigation complete");
                // Close menu after navigation to avoid visual issues
                IsMenuOpen = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GoWriteDiary error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Navigation error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task GoYourDiary()
    {
        IsMenuOpen = false;
        try
        {
            if (_authClient.User == null)
            {
                await Shell.Current.DisplayAlert("Login required", "Please sign in again.", "OK");
                await Shell.Current.GoToAsync("//signin");
                return;
            }
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//main/history");
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Navigation error", ex.Message, "OK");
        }
    }

    public void RefreshUserInfo()
    {
        var user = _authClient.User;
        if (user == null)
        {
            UserName = "Guest";
            UserEmail = string.Empty;
            Coin = 0;
            return;
        }

        var info = user.Info; // may be null if not loaded yet
        var displayName = info?.DisplayName;
        var email = info?.Email;

        // Resolve a username preference order:
        // 1. Non-empty display name
        // 2. Local-part of email (before @)
        // 3. "Unknown User"
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            UserName = displayName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var at = email.IndexOf('@');
            UserName = at > 0 ? email.Substring(0, at) : email;
        }
        else
        {
            UserName = "Unknown User";
        }

        UserEmail = email ?? string.Empty;
        _ = RefreshCoinAsync();
    }

    private int _coin;
    public int Coin
    {
        get => _coin;
        set => SetProperty(ref _coin, value);
    }

    private async Task RefreshCoinAsync()
    {
        try
        {
            var user = _authClient.User;
            if (user?.Uid == null)
            {
                Coin = 0;
                return;
            }

            var db = await _firestoreService.GetDatabaseAsync();
            var snap = await db.Collection("users").Document(user.Uid).GetSnapshotAsync();
            if (snap.Exists && snap.TryGetValue("coin", out int coin))
            {
                Coin = coin;
            }
            else
            {
                Coin = 0;
            }
        }
        catch
        {
            Coin = 0;
        }
    }

    private void OnAuthStateChanged(object? sender, UserEventArgs e)
    {
        // Ensure UI updates on main thread
        if (MainThread.IsMainThread)
            RefreshUserInfo();
        else
            MainThread.BeginInvokeOnMainThread(RefreshUserInfo);
    }

    // ----- Quests -----
    public ObservableCollection<QuestDatabase> Quests { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDailyQuestsAsync();
    }

    private static DateTime TodayStartUtc() => DateTime.UtcNow.Date;
    private static DateTime TodayEndUtc() => DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

    private async Task LoadDailyQuestsAsync()
    {
        IsBusy = true;
        Quests.Clear();

        var user = _authClient.User;
        if (user == null)
        {
            IsBusy = false;
            return;
        }

        // Daily Login quest: completed if simply authenticated
        var loginQuest = new QuestDatabase
        {
            QuestID = $"login-{DateTime.UtcNow:yyyyMMdd}",
            Title = "Daily Check In",
            Type = "Daily",
            StartAt = Timestamp.FromDateTime(TodayStartUtc()),
            EndAt = Timestamp.FromDateTime(TodayEndUtc()),
            Reward = 10,
            Status = "completed",
            Progress = 100
        };
        loginQuest.IsClaimed = await IsQuestClaimedAsync(user.Uid, loginQuest.QuestID);
        // Auto claim if completed but not yet claimed
        if (loginQuest.IsCompleted && !loginQuest.IsClaimed)
        {
            await ClaimInternallyIfNeededAsync(user.Uid, loginQuest);
        }
        Quests.Add(loginQuest);

        // Daily Write Diary quest: check if user has any diary created today
        try
        {
            var diaries = await _diaryDatabase.GetDiariesByUserAsync(user.Uid);
            var hasToday = diaries.Any(d =>
            {
                var t = d.CreatedAt.ToDateTime().ToUniversalTime();
                return t >= TodayStartUtc() && t <= TodayEndUtc();
            });

            var diaryQuest = new QuestDatabase
            {
                QuestID = $"diary-{DateTime.UtcNow:yyyyMMdd}",
                Title = "Write one diary today",
                Type = "Daily",
                StartAt = Timestamp.FromDateTime(TodayStartUtc()),
                EndAt = Timestamp.FromDateTime(TodayEndUtc()),
                Reward = 10,
                Status = hasToday ? "completed" : "pending",
                Progress = hasToday ? 100 : 0
            };
            diaryQuest.IsClaimed = await IsQuestClaimedAsync(user.Uid, diaryQuest.QuestID);
            if (diaryQuest.IsCompleted && !diaryQuest.IsClaimed)
            {
                await ClaimInternallyIfNeededAsync(user.Uid, diaryQuest);
            }
            Quests.Add(diaryQuest);
        }
        catch
        {
            var diaryQuest = new QuestDatabase
            {
                QuestID = $"diary-{DateTime.UtcNow:yyyyMMdd}",
                Title = "Write one diary today",
                Type = "Daily",
                StartAt = Timestamp.FromDateTime(TodayStartUtc()),
                EndAt = Timestamp.FromDateTime(TodayEndUtc()),
                Reward = 10,
                Status = "pending",
                Progress = 0
            };
            diaryQuest.IsClaimed = await IsQuestClaimedAsync(user.Uid, diaryQuest.QuestID);
            Quests.Add(diaryQuest);
        }
        // Refresh coin in case any auto-claim happened
        await RefreshCoinAsync();
        IsBusy = false;
    }

    // Automatically claim a quest in Firestore if it's completed but not yet claimed.
    private async Task ClaimInternallyIfNeededAsync(string uid, QuestDatabase quest)
    {
        try
        {
            var db = await _firestoreService.GetDatabaseAsync();
            var userRef = db.Collection("users").Document(uid);
            var claimRef = userRef.Collection("questClaims").Document(quest.QuestID);

            await db.RunTransactionAsync(async tx =>
            {
                // If already claimed, do nothing
                var claimSnap = await tx.GetSnapshotAsync(claimRef);
                if (claimSnap.Exists) return;

                // Get current coin
                var userSnap = await tx.GetSnapshotAsync(userRef);
                int currentCoin = 0;
                if (userSnap.Exists && userSnap.TryGetValue("coin", out int c)) currentCoin = c;

                // Mark claim
                tx.Set(claimRef, new { questId = quest.QuestID, claimedAt = Timestamp.FromDateTime(DateTime.UtcNow) });

                // Update coin
                tx.Update(userRef, new Dictionary<string, object> { ["coin"] = currentCoin + quest.Reward });
            });

            quest.IsClaimed = true;
        }
        catch
        {
            // Swallow errors to avoid blocking UI; will retry in next refresh
        }
    }

    // ClaimAsync removed: quests are auto-claimed when completed in LoadDailyQuestsAsync.

    private async Task<bool> IsQuestClaimedAsync(string uid, string questId)
    {
        try
        {
            var db = await _firestoreService.GetDatabaseAsync();
            var claimRef = db.Collection("users").Document(uid).Collection("questClaims").Document(questId);
            var snap = await claimRef.GetSnapshotAsync();
            return snap.Exists;
        }
        catch
        {
            return false;
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

            // Reset in-memory user info
            UserName = "Guest";
            UserEmail = string.Empty;

            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//signin");
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
        }
    }
}
