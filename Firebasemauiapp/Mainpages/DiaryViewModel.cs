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
using Microsoft.Maui.ApplicationModel;

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
    private string? _imageDisplayUrl;

    [ObservableProperty]
    private bool _isUploadButtonVisible = true;

    partial void OnImageUrlChanged(string? value)
    {
        System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] === START === ImageUrl changed to: '{value}'");
        
        // When image URL is set, make sure section is visible
        if (!string.IsNullOrWhiteSpace(value))
        {
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] Image received, setting ImageDisplayUrl: {value}");
            IsImageSectionVisible = true;
            ImageDisplayUrl = value;  // Set display URL
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] ImageDisplayUrl set to: {ImageDisplayUrl}");
            IsUploadButtonVisible = false;  // Hide upload button
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] IsUploadButtonVisible set to: false");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] Image cleared, clearing ImageDisplayUrl");
            ImageDisplayUrl = null;  // Clear display URL
            IsUploadButtonVisible = true;  // Show upload button
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] IsUploadButtonVisible set to: true");
        }
        
        System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.OnImageUrlChanged] === END ===");
    }

    [ObservableProperty]
    private bool _isUploadingImage;

    [ObservableProperty]
    private bool _isImageAreaEnabled = true;

    [ObservableProperty]
    private bool _isImageSectionVisible = false;

    [ObservableProperty]
    private Color _moodBackgroundColor = Colors.Transparent;

    public DiaryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient, GitHubUploadService uploadService)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
        _uploadService = uploadService;
    }

    partial void OnMoodChanged(MoodOption? value)
    {
        if (value != null)
        {
            MoodBackgroundColor = GetMoodColor(value.Name);
        }
    }

    private Color GetMoodColor(string moodName)
    {
        return moodName switch
        {
            "Happiness" => Color.FromArgb("#FBC30A"),
            "Love" => Color.FromArgb("#FF60A0"),
            "Angry" => Color.FromArgb("#E4000F"),
            "Disgust" => Color.FromArgb("#1EA064"),
            "Sadness" => Color.FromArgb("#2B638D"),
            "Fear" => Color.FromArgb("#9E9AAB"),
            _ => Color.FromArgb("#FBC30A")
        };
    }

    [RelayCommand]
    private async Task ToggleImageSection()
    {
        System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Toggle called. Current state - IsVisible: {IsImageSectionVisible}, ImageUrl: {ImageUrl}");

        // If section is currently OPEN and we have an image, DELETE it before closing
        if (IsImageSectionVisible && !string.IsNullOrWhiteSpace(ImageUrl))
        {
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Section is open, deleting image: {ImageUrl}");
            var urlToDelete = ImageUrl; // Store it because we're about to set it to null
            
            try 
            { 
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Starting delete...");
                await _uploadService.DeleteImageAsync(urlToDelete);
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Delete completed");
            } 
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Delete error: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Setting ImageUrl to null");
            ImageUrl = null;
        }

        // Toggle open/close
        System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.ToggleImageSection] Toggling IsImageSectionVisible from {IsImageSectionVisible} to {!IsImageSectionVisible}");
        IsImageSectionVisible = !IsImageSectionVisible;
    }

    [RelayCommand]
    private async Task UploadImage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DiaryViewModel] UploadImage started");
            
            // Request permissions first
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            var photoStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();

            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (photoStatus != PermissionStatus.Granted)
            {
                photoStatus = await Permissions.RequestAsync<Permissions.Photos>();
            }

            var action = await Shell.Current.DisplayActionSheet(
                "Choose photo source",
                "Cancel",
                null,
                "Take Photo",
                "Choose from Gallery");

            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] User selected: {action}");

            FileResult? result = null;

            if (action == "Take Photo")
            {
                if (cameraStatus != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permission Denied", "Camera permission is required to take photos.", "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[DiaryViewModel] Capturing photo...");
                // MediaPicker must run on UI thread - RelayCommand is already on UI thread
                try
                {
                    result = await MediaPicker.Default.CapturePhotoAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] CapturePhotoAsync error: {ex.Message}");
                    throw;
                }
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Capture result: {(result != null ? "File selected" : "Cancelled")}");
            }
            else if (action == "Choose from Gallery")
            {
                if (photoStatus != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permission Denied", "Photo library permission is required to choose photos.", "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[DiaryViewModel] Picking from gallery...");
                // MediaPicker must run on UI thread - RelayCommand is already on UI thread
                try
                {
                    result = await MediaPicker.Default.PickPhotoAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] PickPhotoAsync error: {ex.Message}");
                    throw;
                }
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Gallery result: {(result != null ? "File selected" : "Cancelled")}");
            }

            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine("[DiaryViewModel] No file selected, cancelling upload");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] File selected: {result.FileName}, Path: {result.FullPath}");
            
            IsUploadingImage = true;
            System.Diagnostics.Debug.WriteLine("[DiaryViewModel] IsUploadingImage set to true, showing loading indicator");

            using var stream = File.OpenRead(result.FullPath);
            var fileSize = stream.Length;
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] File size: {fileSize} bytes");

            if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Deleting old image: {ImageUrl}");
                try { await _uploadService.DeleteImageAsync(ImageUrl); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Delete old image error: {ex.Message}"); }
            }

            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Uploading image: {result.FileName}");
            string uploadedUrl = await _uploadService.UploadImageAsync(stream, result.FileName);
            
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Upload successful! URL: {uploadedUrl}");
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Current ImageUrl before setting: {ImageUrl}");
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Current IsImageSectionVisible before setting: {IsImageSectionVisible}");
            
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Setting ImageUrl property to new URL...");
            ImageUrl = uploadedUrl;
            
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] ImageUrl property set to: {ImageUrl}");
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] IsImageSectionVisible is now: {IsImageSectionVisible}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] ❌ Upload error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel] Stack trace: {ex.StackTrace}");
            await Shell.Current.DisplayAlert("Upload Failed", ex.Message, "OK");
        }
        finally
        {
            IsUploadingImage = false;
            System.Diagnostics.Debug.WriteLine("[DiaryViewModel] IsUploadingImage set to false");
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

        // ✅ Check both Firebase and Google user
        var currentUser = _authClient.User;
        var googleUser = await GoogleAuthService.Instance.GetGoogleUserAsync();
        
        if (currentUser == null && googleUser == null)
        {
            await Shell.Current.DisplayAlert("Error", "User session expired. Please log in again.", "OK");
            await Shell.Current.GoToAsync("//signin");
            return;
        }

        // Check if Mood and MoodScore are available
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

            // Prepare mood and intensity text to send to AI in specified format
            string moodName = Mood.Name ?? "Unknown";
            string moodIntensityLabel = IntensityText ?? "A Little Bit";

            // Format mood data as: APP_MOOD_BEGIN\nmood: {mood}\nmoodIntensityLabel: {moodIntensityLabel}
            string moodData = $"APP_MOOD_BEGIN\nmood: {moodName}\nmoodIntensityLabel: {moodIntensityLabel}";

            // Send diary content along with mood and intensity info to AI
            await api.SendData(DiaryContent, moodData);

            string suggestion = api.GetSuggestion();
            string keyword = api.GetKeywords();
            string emotion = api.GetEmotionalReflection();

            if (string.IsNullOrWhiteSpace(suggestion) || string.IsNullOrWhiteSpace(keyword))
            {
                await Shell.Current.DisplayAlert("Error", "Failed to analyze content. Please try again.", "OK");
                return;
            }

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

        // ✅ Check both auth sources
        var hasFirebaseUser = _authClient.User != null;
        var hasGoogleUser = await GoogleAuthService.Instance.IsSignedInAsync();

        if (!hasFirebaseUser && !hasGoogleUser)
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
        IsImageSectionVisible = false;
        System.Diagnostics.Debug.WriteLine("[DiaryViewModel] ResetDiaryForm: cleared all fields");
    }
}
