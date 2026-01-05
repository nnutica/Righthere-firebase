using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;

namespace Firebasemauiapp.Mainpages;

public partial class DiaryHistoryViewModel : ObservableObject
{
    private readonly DiaryDatabase _diaryDatabase;
    private readonly FirebaseAuthClient _authClient;
    private List<DiaryData> _allDiaries = new();

    [ObservableProperty]
    private ObservableCollection<DiaryData> _diariesForSelectedDate = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    // Calendar properties
    [ObservableProperty]
    private ObservableCollection<CalendarDay> _calendarDays = new();

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _currentMonth = DateTime.Today;

    [ObservableProperty]
    private string _monthYearDisplay = "";

    public DiaryHistoryViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
    {
        _diaryDatabase = diaryDatabase;
        _authClient = authClient;
        ToggleExpandCommand = new RelayCommand<DiaryData>(ToggleExpand);
        SelectDateCommand = new RelayCommand<CalendarDay>(SelectDate);
        PreviousMonthCommand = new RelayCommand(PreviousMonth);
        NextMonthCommand = new RelayCommand(NextMonth);
        PreviousWeekCommand = new RelayCommand(PreviousWeek);
        NextWeekCommand = new RelayCommand(NextWeek);
        ShowMonthPickerCommand = new AsyncRelayCommand(ShowMonthPicker);

        UpdateMonthYearDisplay();
        GenerateCalendarDays();
    }

    public RelayCommand<DiaryData> ToggleExpandCommand { get; }
    public RelayCommand<CalendarDay> SelectDateCommand { get; }
    public RelayCommand PreviousMonthCommand { get; }
    public RelayCommand NextMonthCommand { get; }
    public RelayCommand PreviousWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }
    public IAsyncRelayCommand ShowMonthPickerCommand { get; }

    private void ToggleExpand(DiaryData? diary)
    {
        if (diary != null)
        {
            diary.IsExpanded = !diary.IsExpanded;
        }
    }

    private void SelectDate(CalendarDay? day)
    {
        if (day != null && day.Date.HasValue)
        {
            // Update selected date
            foreach (var d in CalendarDays)
            {
                d.IsSelected = false;
            }
            day.IsSelected = true;

            SelectedDate = day.Date.Value;
            FilterDiariesBySelectedDate();
        }
    }

    private void PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        UpdateMonthYearDisplay();
        GenerateCalendarDays();
    }

    private void NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        UpdateMonthYearDisplay();
        GenerateCalendarDays();
    }

    private void PreviousWeek()
    {
        // Move selected date back 7 days
        SelectedDate = SelectedDate.AddDays(-7);

        // Update month if needed
        if (SelectedDate.Month != CurrentMonth.Month || SelectedDate.Year != CurrentMonth.Year)
        {
            CurrentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
            UpdateMonthYearDisplay();
        }

        GenerateCalendarDays();
        FilterDiariesBySelectedDate();
    }

    private void NextWeek()
    {
        // Move selected date forward 7 days
        SelectedDate = SelectedDate.AddDays(7);

        // Update month if needed
        if (SelectedDate.Month != CurrentMonth.Month || SelectedDate.Year != CurrentMonth.Year)
        {
            CurrentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
            UpdateMonthYearDisplay();
        }

        GenerateCalendarDays();
        FilterDiariesBySelectedDate();
    }

    private async Task ShowMonthPicker()
    {
        if (Shell.Current == null) return;

        // Create month options
        var months = new List<string>();
        for (int i = 1; i <= 12; i++)
        {
            months.Add(new DateTime(2025, i, 1).ToString("MMMM"));
        }

        var selectedMonth = await Shell.Current.DisplayActionSheet(
            "Select Month",
            "Cancel",
            null,
            months.ToArray());

        if (!string.IsNullOrEmpty(selectedMonth) && selectedMonth != "Cancel")
        {
            var monthIndex = months.IndexOf(selectedMonth) + 1;
            CurrentMonth = new DateTime(CurrentMonth.Year, monthIndex, 1);
            UpdateMonthYearDisplay();
            GenerateCalendarDays();
        }
    }

    private void UpdateMonthYearDisplay()
    {
        MonthYearDisplay = CurrentMonth.ToString("MMMM yyyy");
    }

    private void GenerateCalendarDays()
    {
        CalendarDays.Clear();

        // Get first and last day of the month
        var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Generate 7 days centered around selected date or today
        var centerDate = SelectedDate.Month == CurrentMonth.Month ? SelectedDate : DateTime.Today;

        // If centerDate is not in current month, use middle of the month
        if (centerDate.Month != CurrentMonth.Month || centerDate.Year != CurrentMonth.Year)
        {
            centerDate = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 15);
        }

        var startDate = centerDate.AddDays(-3);

        for (int i = 0; i < 7; i++)
        {
            var date = startDate.AddDays(i);
            CalendarDays.Add(new CalendarDay
            {
                Date = date,
                Day = date.Day.ToString(),
                DayOfWeek = date.ToString("ddd"),
                IsSelected = date.Date == SelectedDate.Date,
                IsCurrentMonth = date.Month == CurrentMonth.Month
            });
        }
    }

    private void FilterDiariesBySelectedDate()
    {
        DiariesForSelectedDate.Clear();

        var filtered = _allDiaries
            .Where(d => d.CreatedAtDateTime.Date == SelectedDate.Date)
            .OrderBy(d => d.CreatedAtDateTime)
            .ToList();

        foreach (var diary in filtered)
        {
            DiariesForSelectedDate.Add(diary);
        }

        IsEmpty = !DiariesForSelectedDate.Any();
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

            // Check Firebase user first
            if (currentUser == null)
            {
                // Check Google user
                var googleUser = await GoogleAuthService.Instance.GetGoogleUserAsync();
                if (googleUser == null || string.IsNullOrWhiteSpace(googleUser.Uid))
                {
                    if (Shell.Current != null)
                        await Shell.Current.GoToAsync("//signin");
                    return;
                }
                // Use Google user's UID
                var diaries = await _diaryDatabase.GetDiariesByUserAsync(googleUser.Uid);

                if (diaries == null)
                {
                    Console.WriteLine("GetDiariesByUserAsync returned null");
                    diaries = new List<DiaryData>();
                }

                Console.WriteLine($"Loaded {diaries.Count} diaries");

                // Store all diaries
                _allDiaries = diaries.OrderByDescending(x => x.CreatedAtDateTime).ToList();

                // Filter by selected date
                FilterDiariesBySelectedDate();

                // Update calendar to show if days have diaries
                foreach (var day in CalendarDays)
                {
                    if (day.Date.HasValue)
                    {
                        day.HasDiary = _allDiaries.Any(d => d.CreatedAtDateTime.Date == day.Date.Value.Date);
                    }
                }

                if (IsEmpty)
                {
                    Console.WriteLine($"No diaries found for Google user: {googleUser.Uid}");
                }
                return;
            }

            // Load all diaries
            var diariesFirebase = await _diaryDatabase.GetDiariesByUserAsync(currentUser.Uid);

            if (diariesFirebase == null)
            {
                Console.WriteLine("GetDiariesByUserAsync returned null");
                diariesFirebase = new List<DiaryData>();
            }

            Console.WriteLine($"Loaded {diariesFirebase.Count} diaries");

            // Store all diaries
            _allDiaries = diariesFirebase.OrderByDescending(x => x.CreatedAtDateTime).ToList();

            // Filter by selected date
            FilterDiariesBySelectedDate();

            // Update calendar to show if days have diaries
            foreach (var day in CalendarDays)
            {
                if (day.Date.HasValue)
                {
                    day.HasDiary = _allDiaries.Any(d => d.CreatedAtDateTime.Date == day.Date.Value.Date);
                }
            }

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
        if (diary != null && Shell.Current != null)
        {
            // Navigate to HistoryDetailPage with diary data
            var parameters = new Dictionary<string, object>
            {
                { "Diary", diary }
            };
            await Shell.Current.GoToAsync("//historydetail", parameters);
        }
    }





    public partial class CalendarDay : ObservableObject
    {
        [ObservableProperty]
        private DateTime? _date;

        [ObservableProperty]
        private string _day = "";

        [ObservableProperty]
        private string _dayOfWeek = "";

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isCurrentMonth = true;

        [ObservableProperty]
        private bool _hasDiary;
    }

    public class DiaryGroupByDate
    {
        public DateTime Date { get; set; }
        public string DateString { get; set; } = string.Empty;
        public ObservableCollection<DiaryData> Diaries { get; set; } = new();
    }
}
