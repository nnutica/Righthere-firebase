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
[QueryProperty(nameof(IntensityText), nameof(IntensityText))]
public partial class DiaryViewModel : ObservableObject
{
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirebaseAuthClient _authClient;
    private readonly GitHubUploadService _uploadService;

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
    private string _intensityText = "A Little Bit";

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private bool _isImageAreaEnabled = false;

    [ObservableProperty]
    private bool _isUploadingImage;


    private bool _allowUpload = false;

    public DiaryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient, GitHubUploadService uploadService)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
        _uploadService = uploadService;
        _allowUpload = true; // ให้ผู้ใช้กดเพิ่มรูปได้ทันที
    }

    [RelayCommand]

    private async Task UploadImage()
    {

        try
        {
            var action = await Shell.Current.DisplayActionSheet(
                "Choose photo source",
                "Cancel",
                null,
                "Take Photo",
                "Choose from Gallery");

            FileResult? result = null;

            if (action == "Take Photo")
                result = await MediaPicker.Default.CapturePhotoAsync();
            else if (action == "Choose from Gallery")
                result = await MediaPicker.Default.PickPhotoAsync();

            if (result == null) return;

            IsUploadingImage = true;

            using var stream = File.OpenRead(result.FullPath);

            if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                try { await _uploadService.DeleteImageAsync(ImageUrl); } catch { }
            }

            ImageUrl = await _uploadService.UploadImageAsync(stream, result.FileName);
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

        // Check if Mood and MoodScore are available (from SelectMood and LevelMood pages)
        if (Mood == null)
        {
            await Shell.Current.DisplayAlert("Error", "Mood information is missing. Please select your mood first.", "OK");
            return;
        }

        IsAnalyzing = true;
        IsLoadingVisible = true;
        AnalyzeButtonText = "Analyzing...";

        try
        {
            var api = new API();
            await api.SendData(DiaryContent);

            string suggestion = api.GetSuggestion();
            string keyword = api.GetKeywords();
            string emotion = api.GetEmotionalReflection();

            if (string.IsNullOrWhiteSpace(suggestion) || string.IsNullOrWhiteSpace(keyword))
            {
                await Shell.Current.DisplayAlert("Error", "Failed to analyze content. Please try again.", "OK");
                return;
            }

            // Use Mood from SelectMood page and MoodScore from LevelMood page
            string moodName = Mood.Name ?? "Unknown";

            SummaryPageData.SetData(DiaryContent, moodName, suggestion, keyword, emotion, MoodScore.ToString(), ImageUrl, IntensityText);

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
        _allowUpload = true; // reset ให้กดเพิ่มรูปใหม่ได้
    }
}
