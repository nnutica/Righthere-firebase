using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Helpers;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;

// Namespace reverted to Firebasemauiapp.Mainpages to match existing view and DI registrations.
namespace Firebasemauiapp.Mainpages;

public partial class SummaryViewModel : ObservableObject
{
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _mood = string.Empty;

    [ObservableProperty]
    private string _suggestion = string.Empty;

    [ObservableProperty]
    private string _keywords = string.Empty;

    [ObservableProperty]
    private string _emotion = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _score = string.Empty;

    [ObservableProperty]
    private string? _imageUrl;


    [ObservableProperty]
    private ImageSource? _emotionImage;

    // Paging state for 3-step summary
    [ObservableProperty]
    private int _pageIndex = 0; // 0: KeyThemes, 1: Reflection, 2: Suggestion

    public bool IsFirstPage => PageIndex == 0;
    public bool IsLastPage => PageIndex == 2;
    public string NextButtonText => IsLastPage ? "Save" : "Next";

    [ObservableProperty]
    private ObservableCollection<string> _keywordsList = new();

    // Dynamic positioned keyword cards
    [ObservableProperty]
    private ObservableCollection<KeywordCard> _keywordCards = new();

    public ICommand GoToStarterCommand { get; }

    // Callback for showing custom popup
    public Func<Task<bool>>? ShowSavePopup { get; set; }

    public SummaryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
        GoToStarterCommand = new RelayCommand(GoToStarter);
    }

    public Task InitializeAsync()
    {
        // Load data from static helper
        if (!string.IsNullOrEmpty(SummaryPageData.Content))
        {
            SetData(SummaryPageData.Content!,
                    SummaryPageData.Mood!,
                    SummaryPageData.Suggestion!,
                    SummaryPageData.Keywords!,
                    SummaryPageData.Emotion!,
                    SummaryPageData.Score!);
            ImageUrl = SummaryPageData.ImageUrl;
        }
        return Task.CompletedTask;
    }

    public void SetData(string content, string mood, string suggestion,
                        string keywords, string emotion, string score)
    {

        Content = content;
        Mood = mood;
        Suggestion = suggestion;
        Keywords = keywords;
        Emotion = emotion;
        Score = score;
        ImageUrl = SummaryPageData.ImageUrl;
        SetEmotionImage(mood);

        // Debug logs
        Console.WriteLine($"[SummaryViewModel] SetData called:");
        Console.WriteLine($"  Content length: {content?.Length ?? 0}");
        Console.WriteLine($"  Mood: {mood}");
        Console.WriteLine($"  Emotion (AI Reflection): {emotion}");
        Console.WriteLine($"  Keywords: {keywords}");
        Console.WriteLine($"  Suggestion: {suggestion}");

        BuildKeywordsList(keywords);
        OnPropertyChanged(nameof(NextButtonText));
    }

    private void BuildKeywordsList(string keywords)
    {
        try
        {
            var parts = (keywords ?? string.Empty)
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            KeywordsList.Clear();

            foreach (var p in parts)
            {
                var t = p.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                {
                    KeywordsList.Add(t);
                }
            }

            // Notify UI that KeywordsList has changed
            OnPropertyChanged(nameof(KeywordsList));
        }
        catch
        {
            KeywordsList.Clear();
            OnPropertyChanged(nameof(KeywordsList));
        }
    }



    [RelayCommand]
    private async Task SaveAndGoBack()
    {
        // แสดง custom popup
        bool result = false;
        if (ShowSavePopup != null)
        {
            result = await ShowSavePopup();
        }
        else
        {
            // Fallback to standard alert if popup not available
            result = await Shell.Current.DisplayAlert("Save Diary", "Do you want to save this diary entry?", "Yes", "No");
        }

        if (result)
        {
            try
            {
                var currentUser = _authClient.User;
                if (currentUser == null)
                {
                    await Shell.Current.DisplayAlert("Error", "User session expired. Please log in again.", "OK");
                    await Shell.Current.GoToAsync("//signin");
                    return;
                }

                var diary = new DiaryData
                {
                    UserId = currentUser.Uid,
                    Content = Content,
                    Mood = Mood,
                    SentimentScore = double.TryParse(Score, out var scoreValue) ? scoreValue : 0.0,
                    Suggestion = Suggestion,
                    Keywords = Keywords,
                    EmotionalReflection = Emotion,
                    ImageUrl = ImageUrl,
                    ImageUrls = string.IsNullOrWhiteSpace(ImageUrl) ? null : new List<string> { ImageUrl },
                    CreatedAtDateTime = DateTime.Now
                };

                string diaryId = await _diaryDatabase.SaveDiaryAsync(diary);
                await Shell.Current.DisplayAlert("Saved", "Diary entry saved successfully.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to save diary: {ex.Message}", "OK");
                return;
            }
        }

        // เครียร์ข้อมูลทั้งหมดไม่ว่าจะกด Yes หรือ No
        SummaryPageData.Clear();

        // ไปหน้า Starter ทั้งหน้าและ Tab พร้อมรีเซ็ตแท็บ Diary
        await ResetDiaryAndGoToStarter();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (IsLastPage)
        {
            await SaveAndGoBack();
            return;
        }
        PageIndex = Math.Min(PageIndex + 1, 2);
        OnPropertyChanged(nameof(IsFirstPage));
        OnPropertyChanged(nameof(IsLastPage));
        OnPropertyChanged(nameof(NextButtonText));
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            OnPropertyChanged(nameof(IsFirstPage));
            OnPropertyChanged(nameof(IsLastPage));
            OnPropertyChanged(nameof(NextButtonText));
        }
    }

    private async Task ResetDiaryAndGoToStarter()
    {
        try
        {
            // รีเซ็ตแท็บ Diary ให้กลับไปที่ SelectMood (root ของแท็บ)
            await Shell.Current.GoToAsync("//main/diary", false);
            // แล้วกลับไปหน้า Starter บนแท็บ Home
            await Shell.Current.GoToAsync("//main/home/starter", false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating to starter: {ex.Message}");
            await Shell.Current.GoToAsync("//main/home/starter");
        }
    }

    private async void GoToStarter()
    {
        await Shell.Current.GoToAsync("//main/home/starter");
    }

    private void SetEmotionImage(string mood)
    {
        if (string.IsNullOrWhiteSpace(mood))
        {
            EmotionImage = null;
            return;
        }

        // สมมติไฟล์รูปภาพอยู่ในโฟลเดอร์ Resources/Images ของโปรเจกต์ และชื่อไฟล์ตาม mood เช่น joy.png
        string imageName = $"{mood.ToLower()}.png";
        EmotionImage = ImageSource.FromFile(imageName);
    }
}

public class KeywordCard : ObservableObject
{
    public string Text { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public string Color { get; set; } = "#FFFFFF";
}
