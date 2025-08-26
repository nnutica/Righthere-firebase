using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.Mainpages;

public partial class DiaryHistoryViewModel : ObservableObject
{
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirebaseAuthClient _authClient;

    [ObservableProperty]
    private ObservableCollection<DiaryGroupByDate> _diaryGroups = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    public DiaryHistoryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
        ToggleExpandCommand = new RelayCommand<DiaryData>(ToggleExpand);
    }

    public RelayCommand<DiaryData> ToggleExpandCommand { get; }

    private void ToggleExpand(DiaryData diary)
    {
        if (diary != null)
        {
            diary.IsExpanded = !diary.IsExpanded;
            // ไม่ต้อง Notify UI ทั้งกลุ่ม เพราะ DiaryData แจ้งเตือนเอง
        }
    }

    public async Task InitializeAsync()
    {
        await LoadDiaryHistory();
    }

    [RelayCommand]
    private async Task LoadDiaryHistory()
    {
        try
        {
            IsLoading = true;
            var currentUser = _authClient.User;

            if (currentUser == null)
            {
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//signin");
                return;
            }

            // ลองดึงข้อมูล diary
            var diaries = await _diaryDatabase.GetDiariesByUserAsync(currentUser.Uid);

            // Group diaries by date
            var groupedDiaries = diaries
                .OrderByDescending(d => d.CreatedAtDateTime)
                .GroupBy(d => d.CreatedAtDateTime.Date)
                .Select(g => new DiaryGroupByDate
                {
                    Date = g.Key,
                    DateString = g.Key.ToString("dddd, MMMM dd, yyyy"),
                    Diaries = new ObservableCollection<DiaryData>(g.ToList())
                })
                .ToList();

            DiaryGroups.Clear();
            foreach (var group in groupedDiaries)
            {
                DiaryGroups.Add(group);
            }

            IsEmpty = !DiaryGroups.Any();

            // ถ้าไม่มีข้อมูล อาจเป็นเพราะ user ยังไม่เคยเขียน diary
            if (IsEmpty)
            {
                Console.WriteLine($"No diaries found for user: {currentUser.Uid}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading diary history: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            var errorMessage = "Failed to load diary history";

            // ตรวจสอบประเภทของ error
            if (ex.Message.Contains("FailedPrecondition"))
            {
                errorMessage = "Database configuration error. Please check Firestore settings.";
            }
            else if (ex.Message.Contains("permission"))
            {
                errorMessage = "Permission denied. Please check your login status.";
            }
            else if (ex.Message.Contains("network") || ex.Message.Contains("connection"))
            {
                errorMessage = "Network error. Please check your internet connection.";
            }

            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"{errorMessage}\n\nDetails: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewDiary(DiaryData diary)
    {
        if (diary != null)
        {
            // Navigate to diary detail or summary view
            var summaryViewModel = new SummaryViewModel(_diaryDatabase, _authClient);
            summaryViewModel.SetData(
                diary.Reason ?? "Unknown",
                diary.Content ?? "",
                diary.Mood ?? "Unknown",
                diary.Suggestion ?? "",
                diary.Keywords ?? "",
                diary.EmotionalReflection ?? "",
                diary.SentimentScore.ToString()
            );

            if (Shell.Current != null)
                await Shell.Current.Navigation.PushAsync(new SummaryView(summaryViewModel));
        }
    }

    [RelayCommand]
    private async Task DeleteDiary(DiaryData diary)
    {
        if (diary == null || Shell.Current == null) return;

        bool result = await Shell.Current.DisplayAlert(
            "Delete Diary",
            "Are you sure you want to delete this diary entry?",
            "Yes",
            "No");

        if (result)
        {
            try
            {
                await _diaryDatabase.DeleteDiaryAsync(diary);
                await LoadDiaryHistory(); // Reload the list
                await Shell.Current.DisplayAlert("Success", "Diary entry deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete diary: {ex.Message}", "OK");
            }
        }
    }
}

public class DiaryGroupByDate
{
    public DateTime Date { get; set; }
    public string DateString { get; set; } = string.Empty;
    public ObservableCollection<DiaryData> Diaries { get; set; } = new();
}
