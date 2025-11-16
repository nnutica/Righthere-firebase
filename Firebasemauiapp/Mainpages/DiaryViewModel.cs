using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Services;
using Firebasemauiapp.Helpers;
using Firebasemauiapp.Model;
using Microsoft.Maui.Storage;
// Crop removed per request

namespace Firebasemauiapp.Mainpages;

[QueryProperty(nameof(Username), nameof(Username))]
[QueryProperty(nameof(Mood), nameof(Mood))]
[QueryProperty(nameof(MoodScore), nameof(MoodScore))]
public partial class DiaryViewModel : ObservableObject
{
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _diaryContent = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private bool _isLoadingVisible = false;

    [ObservableProperty]
    private string _analyzeButtonText = "Next";

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private MoodOption? _mood;

    [ObservableProperty]
    private int _moodScore;

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private bool _isUploadingImage;

    public DiaryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
    }

    [RelayCommand]
    private async Task UploadImage()
    {
        try
        {
            var pick = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select an image",
                FileTypes = FilePickerFileType.Images
            });
            if (pick == null) return;

            IsUploadingImage = true;
            using var original = await pick.OpenReadAsync();
            var uploader = ServiceHelper.Get<GitHubUploadService>();
            var url = await uploader.UploadImageAsync(original, pick.FileName);
            ImageUrl = url;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Upload Failed", ex.Message, "OK");
        }
        finally
        {
            IsUploadingImage = false;
        }
    }

    // Crop function removed

    [RelayCommand]
    private async Task AnalyzeContent()
    {
        if (string.IsNullOrWhiteSpace(DiaryContent))
        {
            await Shell.Current.DisplayAlert("Error", "Please write something before analyzing.", "OK");
            return;
        }

        var currentUser = _authClient.User;
        if (currentUser == null)
        {
            await Shell.Current.DisplayAlert("Error", "User session expired. Please log in again.", "OK");
            await Shell.Current.GoToAsync("//signin");
            return;
        }

        IsAnalyzing = true;
        IsLoadingVisible = true;
        AnalyzeButtonText = "Analyzing...";

        try
        {
            var api = new API();
            await api.SendData(DiaryContent);

            string mood = api.GetMood();
            string suggestion = api.GetSuggestion();
            string keyword = api.GetKeywords();
            string emotion = api.GetEmotionalReflection();
            double score = api.GetScore();

            if (string.IsNullOrWhiteSpace(mood) || string.IsNullOrWhiteSpace(suggestion) || string.IsNullOrWhiteSpace(keyword))
            {
                await Shell.Current.DisplayAlert("Error", "Failed to analyze content. Please try again.", "OK");
                return;
            }

            SummaryPageData.SetData(DiaryContent, mood, suggestion, keyword, emotion, score.ToString(), ImageUrl);

            await Shell.Current.GoToAsync("//summary");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Analysis failed: {ex.Message}");
            await Shell.Current.DisplayAlert("Analysis Failed", $"Failed to analyze content: {ex.Message}\n\nPlease check:\n1. Network connection\n2. API server is running\n3. Try again", "OK");
        }
        finally
        {
            IsAnalyzing = false;
            IsLoadingVisible = false;
            AnalyzeButtonText = "Next ➤";
        }
    }

    public async Task CheckUserAuthentication()
    {
        Console.WriteLine("📍 DiaryPage Appeared");

        if (_authClient.User == null)
        {
            await Shell.Current.DisplayAlert("Error", "User not logged in. Redirecting to login...", "OK");
            await Shell.Current.GoToAsync("//signin");
        }
    }

    public void ResetDiaryForm()
    {
        DiaryContent = string.Empty;
        IsAnalyzing = false;
        IsLoadingVisible = false;
        AnalyzeButtonText = "Next";
        ImageUrl = null;
    }
}
