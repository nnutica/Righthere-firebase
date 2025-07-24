using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Helpers;
using System.Windows.Input;

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
    private string _reason = string.Empty;

    [ObservableProperty]
    private ImageSource? _emotionImage;

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
        if (!string.IsNullOrEmpty(SummaryPageData.Reason))
        {
            SetData(SummaryPageData.Reason, SummaryPageData.Content!,
                   SummaryPageData.Mood!, SummaryPageData.Suggestion!,
                   SummaryPageData.Keywords!, SummaryPageData.Emotion!,
                   SummaryPageData.Score!);
        }
        return Task.CompletedTask;
    }

    public void SetData(string reason, string content, string mood, string suggestion,
                       string keywords, string emotion, string score)
    {
        Reason = reason;
        Content = content;
        Mood = mood;
        Suggestion = suggestion;
        Keywords = keywords;
        Emotion = emotion;
        Score = score;
        SetEmotionImage(mood);
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
                    Reason = Reason,
                    Mood = Mood,
                    SentimentScore = double.TryParse(Score, out var scoreValue) ? scoreValue : 0.0,
                    Suggestion = Suggestion,
                    Keywords = Keywords,
                    EmotionalReflection = Emotion,
                    CreatedAtDateTime = DateTime.Now
                };

                string diaryId = await _diaryDatabase.SaveDiaryAsync(diary);
                await Shell.Current.DisplayAlert("Saved", "Diary entry saved successfully.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to save diary: {ex.Message}", "OK");
            }
        }

        // Navigate back to starter page
        await Shell.Current.GoToAsync("//starter");
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

        // สมมติไฟล์รูปภาพอยู่ในโฟลเดอร์ Resources/Images ของโปรเจกต์ และชื่อไฟล์ตาม mood เช่น joy.png
        string imageName = $"{mood.ToLower()}.png";
        EmotionImage = ImageSource.FromFile(imageName);
    }
}
