using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Services;
using Firebasemauiapp.Helpers;

namespace Firebasemauiapp.Mainpages;

public partial class DiaryViewModel : ObservableObject
{
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private string _selectedReason = "friend";

    [ObservableProperty]
    private string _diaryContent = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private bool _isLoadingVisible = false;

    [ObservableProperty]
    private string _analyzeButtonText = "Next ➤";

    // Button colors for UI
    [ObservableProperty]
    private Color _friendButtonBackground = Colors.DarkGreen;

    [ObservableProperty]
    private Color _workButtonBackground = Colors.LightGray;

    [ObservableProperty]
    private Color _familyButtonBackground = Colors.LightGray;

    [ObservableProperty]
    private Color _schoolButtonBackground = Colors.LightGray;

    [ObservableProperty]
    private Color _friendButtonText = Colors.White;

    [ObservableProperty]
    private Color _workButtonText = Colors.DarkGray;

    [ObservableProperty]
    private Color _familyButtonText = Colors.DarkGray;

    [ObservableProperty]
    private Color _schoolButtonText = Colors.DarkGray;

    public DiaryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
    }

    [RelayCommand]
    private void SelectReason(string reason)
    {
        SelectedReason = reason;
        UpdateReasonButtonStyles();
    }

    private void UpdateReasonButtonStyles()
    {
        // Reset all buttons
        FriendButtonBackground = Colors.LightGray;
        WorkButtonBackground = Colors.LightGray;
        FamilyButtonBackground = Colors.LightGray;
        SchoolButtonBackground = Colors.LightGray;

        FriendButtonText = Colors.DarkGray;
        WorkButtonText = Colors.DarkGray;
        FamilyButtonText = Colors.DarkGray;
        SchoolButtonText = Colors.DarkGray;

        // Set selected button
        switch (SelectedReason)
        {
            case "friend":
                FriendButtonBackground = Colors.DarkGreen;
                FriendButtonText = Colors.White;
                break;
            case "work":
                WorkButtonBackground = Colors.DarkGreen;
                WorkButtonText = Colors.White;
                break;
            case "family":
                FamilyButtonBackground = Colors.DarkGreen;
                FamilyButtonText = Colors.White;
                break;
            case "school":
                SchoolButtonBackground = Colors.DarkGreen;
                SchoolButtonText = Colors.White;
                break;
        }
    }

    [RelayCommand]
    private async Task AnalyzeContent()
    {
        if (string.IsNullOrWhiteSpace(DiaryContent))
        {
            await Shell.Current.DisplayAlert("Error", "Please write something before analyzing.", "OK");
            return;
        }

        // Check user authentication
        var currentUser = _authClient.User;
        if (currentUser == null)
        {
            await Shell.Current.DisplayAlert("Error", "User session expired. Please log in again.", "OK");
            await Shell.Current.GoToAsync("//signin");
            return;
        }

        // Start loading
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

            // Store data in static class for transfer
            SummaryPageData.SetData(SelectedReason, DiaryContent, mood, suggestion, keyword, emotion, score.ToString());
            
            // Navigate to Summary
            await Shell.Current.GoToAsync("//summary");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Analysis failed: {ex.Message}");
            await Shell.Current.DisplayAlert("Analysis Failed", $"Failed to analyze content: {ex.Message}\n\nPlease check:\n1. Network connection\n2. API server is running\n3. Try again", "OK");
        }
        finally
        {
            // Stop loading
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
}
