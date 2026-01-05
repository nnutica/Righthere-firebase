using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;

namespace Firebasemauiapp.QuestPage;

public partial class QuestViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly DiaryDatabase _diaryDatabase;
    private readonly PostDatabase _postDatabase;
    private readonly FirestoreService _firestoreService;
    private System.Timers.Timer? _countdownTimer;

    public QuestViewModel(FirebaseAuthClient authClient, DiaryDatabase diaryDatabase, PostDatabase postDatabase, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _diaryDatabase = diaryDatabase;
        _postDatabase = postDatabase;
        _firestoreService = firestoreService;

        // React to auth state changes
        _authClient.AuthStateChanged += OnAuthStateChanged;

        // Initial load
        RefreshUserInfo();

        // Start countdown timer
        StartCountdownTimer();
    }

    private int _coin;
    public int Coin
    {
        get => _coin;
        set => SetProperty(ref _coin, value);
    }

    private string _userName = "Guest";
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    private string _currentPlant = "plant.png";
    public string CurrentPlant
    {
        get => _currentPlant;
        set => SetProperty(ref _currentPlant, value);
    }

    private string _currentPot = "pot.png";
    public string CurrentPot
    {
        get => _currentPot;
        set => SetProperty(ref _currentPot, value);
    }

    private string _timeUntilReset = "back in 0:00:00";
    public string TimeUntilReset
    {
        get => _timeUntilReset;
        set => SetProperty(ref _timeUntilReset, value);
    }

    private string _progressColor1 = "#E0E0E0";
    public string ProgressColor1
    {
        get => _progressColor1;
        set => SetProperty(ref _progressColor1, value);
    }

    private string _progressColor2 = "#E0E0E0";
    public string ProgressColor2
    {
        get => _progressColor2;
        set => SetProperty(ref _progressColor2, value);
    }

    private string _progressBorder2 = "#E0E0E0";
    public string ProgressBorder2
    {
        get => _progressBorder2;
        set => SetProperty(ref _progressBorder2, value);
    }

    private string _progressBorder3 = "#E0E0E0";
    public string ProgressBorder3
    {
        get => _progressBorder3;
        set => SetProperty(ref _progressBorder3, value);
    }

    public void RefreshUserInfo()
    {
        // ✅ ดึงจาก UserService แทน
        if (UserService.Instance.IsLoaded)
        {
            UserName = UserService.Instance.Username;
        }
        else
        {
            // Fallback: Try Firebase user
            var user = _authClient.User;
            if (user != null)
            {
                UserName = user.Info.DisplayName ?? user.Info.Email ?? "Guest";
            }
            else
            {
                UserName = "Guest";
            }
        }
        
        _ = RefreshCoinAsync();
        _ = LoadUserPlantAndPotAsync();
        _ = LoadUserInfoFromFirestoreAsync();
    }

    private async Task LoadUserInfoFromFirestoreAsync()
    {
        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                UserName = "Guest";
                Coin = 0;
                CurrentPlant = "empty.png";
                CurrentPot = "pot.png";
                return;
            }

            // ดึง username
            var username = await _firestoreService.GetUsernameAsync(uid);
            UserName = string.IsNullOrEmpty(username) ? "Guest" : username;

            // ดึง coin
            Coin = await _firestoreService.GetCoinAsync(uid);

            // ดึง plant และ pot
            var (plant, pot) = await _firestoreService.GetPlantAndPotAsync(uid);
            CurrentPlant = plant;
            CurrentPot = pot;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user info from Firestore: {ex.Message}");
            UserName = "Guest";
            Coin = 0;
            CurrentPlant = "empty.png";
            CurrentPot = "pot.png";
        }
    }

    private async Task LoadUserPlantAndPotAsync()
    {
        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                CurrentPlant = "empty.png";
                CurrentPot = "pot.png";
                return;
            }

            var (plant, pot) = await _firestoreService.GetPlantAndPotAsync(uid);
            CurrentPlant = plant;
            CurrentPot = pot;
        }
        catch
        {
            CurrentPlant = "empty.png";
            CurrentPot = "pot.png";
        }
    }

    private async Task RefreshCoinAsync()
    {
        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                Coin = 0;
                return;
            }

            Coin = await _firestoreService.GetCoinAsync(uid);
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

    private void StartCountdownTimer()
    {
        _countdownTimer = new System.Timers.Timer(1000); // Update every second
        _countdownTimer.Elapsed += (s, e) => UpdateTimeUntilReset();
        _countdownTimer.AutoReset = true;
        _countdownTimer.Start();
        UpdateTimeUntilReset();
    }

    private void UpdateTimeUntilReset()
    {
        // Use UTC+7 timezone (Bangkok/Indochina Time)
        var utcNow = DateTime.UtcNow;
        var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, thaiTimeZone);
        var midnight = now.Date.AddDays(1);
        var timeLeft = midnight - now;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TimeUntilReset = $"back in {timeLeft.Hours}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
        });
    }

    private void UpdateProgressBar()
    {
        // Calculate completed quests
        int completedCount = Quests.Count(q => q.IsCompleted);
        int totalQuests = Quests.Count;

        if (totalQuests == 0)
        {
            // No quests, reset to default
            ProgressColor1 = "#E0E0E0";
            ProgressColor2 = "#E0E0E0";
            ProgressBorder2 = "#E0E0E0";
            ProgressBorder3 = "#E0E0E0";
            return;
        }

        // Calculate percentage: 33.33% per quest (assuming 3 daily quests)
        double completionPercentage = (double)completedCount / totalQuests * 100;

        // Reset all to gray first
        ProgressColor1 = "#E0E0E0";
        ProgressColor2 = "#E0E0E0";
        ProgressBorder2 = "#E0E0E0";
        ProgressBorder3 = "#E0E0E0";

        // Stage 1: >= 33.33% (1 quest completed)
        if (completionPercentage >= 33.33)
        {
            ProgressColor1 = "#90C590";
            ProgressBorder2 = "#90C590";
        }

        // Stage 2: >= 66.67% (2 quests completed)
        if (completionPercentage >= 66.67)
        {
            ProgressColor2 = "#90C590";
            ProgressBorder3 = "#90C590";
        }

        // Stage 3: 100% (all quests completed)
        if (completionPercentage >= 100)
        {
            ProgressColor1 = "#90C590";
            ProgressColor2 = "#90C590";
            ProgressBorder2 = "#90C590";
            ProgressBorder3 = "#90C590";
        }
    }

    public void Dispose()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _authClient.AuthStateChanged -= OnAuthStateChanged;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDailyQuestsAsync();
    }

    public async Task LoadDailyQuestsAsync()
    {
        IsBusy = true;
        Quests.Clear();

        // Use UserService to support both Firebase and Google users
        var uid = UserService.Instance.Uid;
        if (string.IsNullOrEmpty(uid))
        {
            IsBusy = false;
            return;
        }

        // ดึง quest ทั้งหมดจาก Quest.cs
        var dailyQuests = await Quest.GetDailyQuestsAsync(_diaryDatabase, _postDatabase, uid);

        // อัพเดท claimed status และ button state
        foreach (var quest in dailyQuests)
        {
            quest.IsClaimed = await IsQuestClaimedAsync(uid, quest.QuestID);
            quest.UpdateButtonState();
            Quests.Add(quest);
        }

        // Sort quests: unclaimed first, claimed last
        var sortedQuests = Quests.OrderBy(q => q.SortOrder).ToList();
        Quests.Clear();
        foreach (var q in sortedQuests)
        {
            Quests.Add(q);
        }

        // Update progress bar
        UpdateProgressBar();

        IsBusy = false;
    }

    // Claim a quest reward in Firestore
    private async Task ClaimQuestInternalAsync(string uid, QuestDatabase quest)
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

    public async Task ClaimQuestRewardAsync(QuestDatabase quest)
    {
        if (quest.IsClaimed || !quest.IsCompleted)
            return;

        // Use UserService to support both Firebase and Google users
        var uid = UserService.Instance.Uid;
        if (string.IsNullOrEmpty(uid))
            return;

        await ClaimQuestInternalAsync(uid, quest);
        quest.UpdateButtonState();
        await RefreshCoinAsync();
    }
}
