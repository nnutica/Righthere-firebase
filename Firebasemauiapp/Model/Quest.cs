using System;
using System.Collections.Generic;
using System.Linq;
using Firebasemauiapp.Data;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.Model;

public static class Quest
{
    private static DateTime TodayStartUtc() => DateTime.UtcNow.Date;
    private static DateTime TodayEndUtc() => DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

    /// <summary>
    /// สร้าง Daily Check In quest
    /// </summary>
    public static QuestDatabase CreateDailyCheckInQuest()
    {
        return new QuestDatabase
        {
            QuestID = $"login-{DateTime.UtcNow:yyyyMMdd}",
            Title = "Daily Check In",
            Type = "Daily",
            StartAt = Timestamp.FromDateTime(TodayStartUtc()),
            EndAt = Timestamp.FromDateTime(TodayEndUtc()),
            Reward = 10,
            Status = "completed", // Always completed when logged in
            Progress = 100,
            CurrentProgress = 1,
            MaxProgress = 1,
            Icon = "diaryquest.png" // Icon for check-in quest
        };
    }

    /// <summary>
    /// สร้าง Write Diary quest โดยตรวจสอบว่าเขียนไดอารี่วันนี้แล้วหรือยัง
    /// </summary>
    public static async System.Threading.Tasks.Task<QuestDatabase> CreateWriteDiaryQuestAsync(
        DiaryDatabase diaryDatabase,
        string userId)
    {
        bool hasToday = false;

        try
        {
            var diaries = await diaryDatabase.GetDiariesByUserAsync(userId);
            hasToday = diaries.Any(d =>
            {
                var t = d.CreatedAt.ToDateTime().ToUniversalTime();
                return t >= TodayStartUtc() && t <= TodayEndUtc();
            });
        }
        catch
        {
            hasToday = false;
        }

        return new QuestDatabase
        {
            QuestID = $"diary-{DateTime.UtcNow:yyyyMMdd}",
            Title = "Write one diary today",
            Type = "Daily",
            StartAt = Timestamp.FromDateTime(TodayStartUtc()),
            EndAt = Timestamp.FromDateTime(TodayEndUtc()),
            Reward = 10,
            Status = hasToday ? "completed" : "pending",
            Progress = hasToday ? 100 : 0,
            CurrentProgress = hasToday ? 1 : 0,
            MaxProgress = 1,
            Icon = "diaryquest.png" // Icon for diary quest
        };
    }

    /// <summary>
    /// สร้าง Share Your Love quest โดยตรวจสอบว่าโพสวันนี้แล้วหรือยัง
    /// </summary>
    public static async System.Threading.Tasks.Task<QuestDatabase> CreateShareYourLoveQuestAsync(
        PostDatabase postDatabase,
        string userId)
    {
        bool hasPostedToday = false;

        try
        {
            Console.WriteLine($"[Quest] Checking if user '{userId}' has posted today for 'Share Your Love' quest");
            hasPostedToday = await postDatabase.HasUserPostedTodayAsync(userId);
            Console.WriteLine($"[Quest] Share Your Love quest check result: {hasPostedToday}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Quest] Error checking posts for Share Your Love quest: {ex.Message}");
            hasPostedToday = false;
        }

        return new QuestDatabase
        {
            QuestID = $"sharepost-{DateTime.UtcNow:yyyyMMdd}",
            Title = "Share your love",
            Type = "Daily",
            StartAt = Timestamp.FromDateTime(TodayStartUtc()),
            EndAt = Timestamp.FromDateTime(TodayEndUtc()),
            Reward = 10,
            Status = hasPostedToday ? "completed" : "pending",
            Progress = hasPostedToday ? 100 : 0,
            CurrentProgress = hasPostedToday ? 1 : 0,
            MaxProgress = 1,
            Icon = "treequest.png" // Icon for share post quest
        };
    }

    /// <summary>
    /// ดึง quest ทั้งหมดสำหรับวันนี้
    /// </summary>
    public static async System.Threading.Tasks.Task<List<QuestDatabase>> GetDailyQuestsAsync(
        DiaryDatabase diaryDatabase,
        PostDatabase postDatabase,
        string userId)
    {
        var quests = new List<QuestDatabase>();

        // Daily Check In
        var loginQuest = CreateDailyCheckInQuest();
        quests.Add(loginQuest);

        // Write Diary
        var diaryQuest = await CreateWriteDiaryQuestAsync(diaryDatabase, userId);
        quests.Add(diaryQuest);

        // Share Your Love
        var shareQuest = await CreateShareYourLoveQuestAsync(postDatabase, userId);
        quests.Add(shareQuest);

        // เพิ่ม quest ใหม่ตรงนี้ได้เลย
        // var newQuest = CreateNewQuest();
        // quests.Add(newQuest);

        return quests;
    }
}
