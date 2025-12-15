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
    private string _intensityText = "A Little Bit";

    [ObservableProperty]
    private string _moodIntensityLabel = "Happiness A Little Bit";


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

    // Individual keyword properties for Post-it binding
    [ObservableProperty]
    private string _keyword1 = string.Empty;

    [ObservableProperty]
    private string _keyword2 = string.Empty;

    [ObservableProperty]
    private string _keyword3 = string.Empty;

    [ObservableProperty]
    private string _keyword4 = string.Empty;

    [ObservableProperty]
    private string _keyword5 = string.Empty;

    // Dynamic positioned keyword cards
    [ObservableProperty]
    private ObservableCollection<KeywordCard> _keywordCards = new();

    public ICommand GoToStarterCommand { get; }

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
                    SummaryPageData.Score!,
                    SummaryPageData.IntensityText);
            ImageUrl = SummaryPageData.ImageUrl;
        }
        return Task.CompletedTask;
    }

    public void SetData(string content, string mood, string suggestion,
                        string keywords, string emotion, string score, string? intensityText = null)
    {

        Content = content;
        Mood = mood;
        Suggestion = suggestion;
        Keywords = keywords;
        Emotion = emotion;
        Score = score;
        IntensityText = intensityText ?? "A Little Bit";
        MoodIntensityLabel = $"{mood}\n{IntensityText}";
        ImageUrl = SummaryPageData.ImageUrl;
        SetEmotionImage(mood);

        BuildKeywordsList(keywords);
        OnPropertyChanged(nameof(NextButtonText));
    }

    private void BuildKeywordsList(string keywords)
    {
        try
        {
            var parts = (keywords ?? string.Empty)
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            KeywordsList = new ObservableCollection<string>();
            KeywordCards = new ObservableCollection<KeywordCard>();
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (!string.IsNullOrWhiteSpace(t)) KeywordsList.Add(t);
            }

            // Update individual keyword properties
            Keyword1 = KeywordsList.Count > 0 ? KeywordsList[0] : string.Empty;
            Keyword2 = KeywordsList.Count > 1 ? KeywordsList[1] : string.Empty;
            Keyword3 = KeywordsList.Count > 2 ? KeywordsList[2] : string.Empty;
            Keyword4 = KeywordsList.Count > 3 ? KeywordsList[3] : string.Empty;
            Keyword5 = KeywordsList.Count > 4 ? KeywordsList[4] : string.Empty;

            BuildKeywordCards();
        }
        catch { KeywordsList = new ObservableCollection<string>(); }
    }

    private void BuildKeywordCards()
    {
        KeywordCards.Clear();
        if (KeywordsList.Count == 0) return;

        // Predefined palette & layout (normalized Rect: X,Y,Width,Height)
        var colors = new[] { "#A6C9C6", "#F0E7DA", "#8EB7D1", "#25E88C", "#F4F3EF", "#D7C1FF", "#FFD9A1" };
        var rects = new List<Rect>
        {
            // Reference-style overlapping layout
            new Rect(0.62, 0.04, 240,240),
            new Rect(0.38, 0.18, 240,270),
            new Rect(0.62, 0.31, 240,240),
            new Rect(0.35, 0.48, 240,240),
            new Rect(0.65, 0.63, 240,100) // Card 5 (bottom-right small top layer)
        };

        // If fewer than 5, use first N rects.
        int count = Math.Min(KeywordsList.Count, rects.Count);
        for (int i = 0; i < count; i++)
        {
            KeywordCards.Add(new KeywordCard
            {
                Text = KeywordsList[i],
                Bounds = rects[i],
                Color = colors[i % colors.Length]
            });
        }

        // If more than 5, append smaller chips at bottom wrap style (simple horizontal flow)
        if (KeywordsList.Count > rects.Count)
        {
            double startY = 0.78; // begin near bottom area used by last main card
            double rowHeight = 0.10;
            double chipH = 0.08;
            double xStart = 0.05;
            double x = xStart;
            foreach (var extra in KeywordsList.Skip(rects.Count))
            {
                double chipW = 0.28;
                if (x + chipW > 0.95)
                {
                    x = xStart;
                    startY += rowHeight;
                    if (startY > 0.92) break; // avoid overflow off-screen
                }
                KeywordCards.Add(new KeywordCard
                {
                    Text = extra,
                    Bounds = new Rect(x, startY, chipW, chipH),
                    Color = colors[(KeywordCards.Count) % colors.Length]
                });
                x += chipW + 0.035;
            }
        }
    }

    [RelayCommand]
    private async Task SaveAndGoBack()
    {
        // แสดง confirmation dialog
        bool result = await Shell.Current.DisplayAlert("Save Diary", "Do you want to save this diary entry?", "Yes", "No");

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

    [RelayCommand]
    private async Task SaveDiary()
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

            // เครียร์ข้อมูล
            SummaryPageData.Clear();

            // กลับไปหน้า Starter
            await ResetDiaryAndGoToStarter();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save diary: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LeaveWithoutSaving()
    {
        // เครียร์ข้อมูลโดยไม่เซฟ
        SummaryPageData.Clear();

        // กลับไปหน้า Starter
        await ResetDiaryAndGoToStarter();
    }

    private async Task ResetDiaryAndGoToStarter()
    {
        try
        {
            // กลับไปหน้า Starter (absolute path ไปที่ root)
            await Shell.Current.GoToAsync("//starter", false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating to starter: {ex.Message}");
            await Shell.Current.GoToAsync("//starter");
        }
    }

    private async void GoToStarter()
    {
        await Shell.Current.GoToAsync("//starter");
    }

    private void SetEmotionImage(string mood)
    {
        if (string.IsNullOrWhiteSpace(mood))
        {
            EmotionImage = null;
            return;
        }

        // Map mood names to image filenames
        string imageName = mood.ToLower() switch
        {
            "happiness" => "happiness.png",
            "love" => "love.png",
            "angry" => "anger.png",
            "surprise" => "surprise.png",
            "sadness" => "sadness.png",
            "fear" => "fear.png",
            _ => $"{mood.ToLower()}.png" // fallback
        };

        EmotionImage = ImageSource.FromFile(imageName);
    }
}

public class KeywordCard : ObservableObject
{
    public string Text { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public string Color { get; set; } = "#FFFFFF";
}
