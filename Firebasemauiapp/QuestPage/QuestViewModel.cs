using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.QuestPage;

public partial class QuestViewModel : ObservableObject
{
	private readonly FirebaseAuthClient _auth;
	private readonly FirestoreService _firestoreService;
	private readonly DiaryDatabase _diaryDb;

	public ObservableCollection<QuestDatabase> Quests { get; } = new();

	[ObservableProperty]
	private bool isBusy;

	public QuestViewModel(FirebaseAuthClient auth, FirestoreService firestoreService, DiaryDatabase diaryDb)
	{
		_auth = auth;
		_firestoreService = firestoreService;
		_diaryDb = diaryDb;

		_ = LoadDailyQuestsAsync();
	}

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

		var user = _auth.User;
		if (user == null)
			return;

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
		Quests.Add(loginQuest);

		// Daily Write Diary quest: check if user has any diary created today
		try
		{
			var diaries = await _diaryDb.GetDiariesByUserAsync(user.Uid);
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

		IsBusy = false;
	}

	[RelayCommand]
	private async Task ClaimAsync(QuestDatabase? quest)
	{
		if (quest == null || !quest.CanClaim) return;

		var user = _auth.User;
		if (user == null) return;

		try
		{
			IsBusy = true;
			var db = await _firestoreService.GetDatabaseAsync();

			// Update user's coin atomically and write quest claim marker
			var userRef = db.Collection("users").Document(user.Uid);
			var claimRef = userRef.Collection("questClaims").Document(quest.QuestID);

			await db.RunTransactionAsync(async tx =>
			{
				var userSnap = await tx.GetSnapshotAsync(userRef);
				int currentCoin = 0;
				if (userSnap.Exists && userSnap.TryGetValue("coin", out int c)) currentCoin = c;

				// if already claimed, no-op
				var claimSnap = await tx.GetSnapshotAsync(claimRef);
				if (claimSnap.Exists) return;

				// write claim doc
				tx.Set(claimRef, new { questId = quest.QuestID, claimedAt = Timestamp.FromDateTime(DateTime.UtcNow) });

				// update coin
				tx.Update(userRef, new Dictionary<string, object> { ["coin"] = currentCoin + quest.Reward });
			});

			quest.IsClaimed = true;
		}
		finally
		{
			IsBusy = false;
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
}

