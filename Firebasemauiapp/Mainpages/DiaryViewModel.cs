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
using SkiaSharp;

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

            // Ask user for crop ratio
            string choice = await Shell.Current.DisplayActionSheet(
                "Crop image",
                "Cancel",
                null,
                "Square 1:1",
                "4:3",
                "16:9",
                "Original");

            Stream toUpload = original;
            string fileName = pick.FileName;
            if (!string.IsNullOrEmpty(choice) && choice != "Cancel" && choice != "Original")
            {
                var parts = choice.Split(' ');
                var ratio = parts[1]; // e.g., 1:1
                var ab = ratio.Split(':');
                if (ab.Length == 2 && int.TryParse(ab[0], out var a) && int.TryParse(ab[1], out var b) && a > 0 && b > 0)
                {
                    toUpload = await CropToAspectAsync(original, a, b);
                    fileName = System.IO.Path.GetFileNameWithoutExtension(pick.FileName) + "-c" + System.IO.Path.GetExtension(pick.FileName);
                }
            }

            var uploader = ServiceHelper.Get<GitHubUploadService>();
            var url = await uploader.UploadImageAsync(toUpload, fileName);
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

    private static async Task<Stream> CropToAspectAsync(Stream input, int aspectW, int aspectH)
    {
        // Ensure we can read multiple times
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        ms.Position = 0;

        using var bitmap = SKBitmap.Decode(ms);
        if (bitmap == null)
        {
            ms.Position = 0;
            return ms; // fallback
        }

        // Compute crop rect (center crop to desired aspect)
        double srcW = bitmap.Width;
        double srcH = bitmap.Height;
        double targetRatio = (double)aspectW / aspectH;
        double srcRatio = srcW / srcH;

        double cropW = srcW;
        double cropH = srcH;
        if (srcRatio > targetRatio)
        {
            // too wide -> reduce width
            cropW = srcH * targetRatio;
        }
        else if (srcRatio < targetRatio)
        {
            // too tall -> reduce height
            cropH = srcW / targetRatio;
        }

        var left = (srcW - cropW) / 2.0;
        var top = (srcH - cropH) / 2.0;
        var srcRect = new SKRect((float)left, (float)top, (float)(left + cropW), (float)(top + cropH));

        // Draw to a new bitmap with the cropped size
        using var cropped = new SKBitmap((int)cropW, (int)cropH);
        using (var canvas = new SKCanvas(cropped))
        {
            canvas.Clear(SKColors.Transparent);
            var dest = new SKRect(0, 0, cropped.Width, cropped.Height);
            canvas.DrawBitmap(bitmap, srcRect, dest);
        }

        using var image = SKImage.FromBitmap(cropped);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        var outStream = new MemoryStream();
        data.SaveTo(outStream);
        outStream.Position = 0;
        return outStream;
    }

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
