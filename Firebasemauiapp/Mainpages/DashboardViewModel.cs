using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Data;
using Firebase.Auth;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.Mainpages;

public partial class DashboardViewModel : ObservableObject
{
	// Chart property and related logic removed
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;

	[ObservableProperty]
	private ObservableCollection<double> _sentimentScores = new();

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private double _averageSentimentScore;

	public string AverageDisplay => $"AVG {AverageSentimentScore:F1}";

	[ObservableProperty]
	private string _selectedPeriod = "ทั้งหมด";

	public ObservableCollection<string> PeriodOptions { get; } = new()
	{
		"3 วันล่าสุด",
		"5 วันล่าสุด",
		"สัปดาห์ที่แล้ว",
		"ทั้งหมด"
	};

	// Most frequent mood tree in last 7 days
	[ObservableProperty]
	private string _mostFrequentMoodImage = "empty.png";

	[ObservableProperty]
	private string _mostFrequentMoodName = "No Data";

	// Weekly Pulse data (last 7 days)
	private ObservableCollection<PulseItem> _weeklyPulseData = new();
	public ObservableCollection<PulseItem> WeeklyPulseData
	{
		get => _weeklyPulseData;
		set => SetProperty(ref _weeklyPulseData, value);
	}

	// Week offset (0 = current week, -1 = previous week, etc.)
	[ObservableProperty]
	private int _weekOffset = 0;

	// Date range display
	[ObservableProperty]
	private string _weekDateRange = "";

	// Resonating Themes
	[ObservableProperty]
	private ObservableCollection<ThemeData> _resonatingThemes = new();

	// Background color based on mood
	[ObservableProperty]
	private Color _moodBackgroundColor = Colors.LightGray;

	// For Syncfusion chart
	public class ChartDataPoint
	{
		public string Day { get; set; }
		public double Score { get; set; }
		public ChartDataPoint(string day, double score)
		{
			Day = day;
			Score = score;
		}
	}

	public partial class PulseItem : ObservableObject
	{
		[ObservableProperty]
		private string _day = "";

		[ObservableProperty]
		private double _score;

		public string ScoreText => Score > 0 ? Score.ToString("F0") : "";
	}

	public class ThemeData
	{
		public string ThemeName { get; set; } = "";
		public double Percentage { get; set; }
		public string PercentageText => $"{Percentage:F1}%";
		public Color BackgroundColor { get; set; } = Colors.LightGray;
	}

	// For Syncfusion chart binding
	[ObservableProperty]
	private ObservableCollection<ChartDataPoint> _chartData = new();

	public string TodayString => DateTime.Now.ToString("ddd, dd MMM");

	public DashboardViewModel(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
	{
		_diaryDatabase = diaryDatabase;
		_authClient = authClient;
		LoadSentimentScoresCommand = new AsyncRelayCommand(LoadSentimentScores);
		GoToDiaryHistoryCommand = new AsyncRelayCommand(GoToDiaryHistory);
		PreviousWeekCommand = new AsyncRelayCommand(PreviousWeek);
		NextWeekCommand = new AsyncRelayCommand(NextWeek);

		// Initialize WeeklyPulseData with 7 empty items
		for (int i = 0; i < 7; i++)
		{
			WeeklyPulseData.Add(new PulseItem { Day = "", Score = 0 });
		}
	}

	public IAsyncRelayCommand LoadSentimentScoresCommand { get; }
	public IAsyncRelayCommand GoToDiaryHistoryCommand { get; }
	public IAsyncRelayCommand PreviousWeekCommand { get; }
	public IAsyncRelayCommand NextWeekCommand { get; }

	partial void OnSelectedPeriodChanged(string value)
	{
		_ = LoadSentimentScores();
	}

	private async Task<string?> WaitForUidAsync(int timeoutMs = 2500)
	{
		var start = Environment.TickCount;

		while (Environment.TickCount - start < timeoutMs)
		{
			var uid = _authClient?.User?.Uid;
			if (!string.IsNullOrWhiteSpace(uid))
				return uid;

			await Task.Delay(150);
		}

		return null;
	}


	private async Task LoadSentimentScores()
	{
		try
		{
			IsLoading = true;

			// Initialize collections if null
			if (SentimentScores == null) SentimentScores = new ObservableCollection<double>();
			if (ChartData == null) ChartData = new ObservableCollection<ChartDataPoint>();
			if (ResonatingThemes == null) ResonatingThemes = new ObservableCollection<ThemeData>();

			SentimentScores.Clear();
			ChartData.Clear();
			ResonatingThemes.Clear();

			var uid = await WaitForUidAsync();
			if (string.IsNullOrWhiteSpace(uid))
			{
				Console.WriteLine("Dashboard: UID still null after waiting.");
				return;
			}


			Console.WriteLine($"Dashboard: Loading diaries for user: {uid}");
			var diaries = (await _diaryDatabase.GetDiariesByUserAsync(uid))
				?.OrderBy(x => x.CreatedAtDateTime)
				.ToList();

			if (diaries == null)
			{
				Console.WriteLine("Dashboard: GetDiariesByUserAsync returned null");
				diaries = new List<DiaryData>();
			}

			Console.WriteLine($"Dashboard: Loaded {diaries.Count} diaries"); foreach (var d in diaries)
			{
				SentimentScores.Add(d.SentimentScore);
			}

			// Calculate week range based on offset
			// Week starts on Sunday (0) and ends on Saturday (6)
			var today = DateTime.Today;
			var currentDayOfWeek = (int)today.DayOfWeek; // Sunday = 0, Saturday = 6

			// Calculate the start of current week (Sunday)
			var currentWeekStart = today.AddDays(-currentDayOfWeek);

			// Apply week offset
			var weekStart = currentWeekStart.AddDays(WeekOffset * 7);
			var weekEnd = weekStart.AddDays(6);

			// Filter diaries for the selected week
			var weekDiaries = diaries
				.Where(d => d.CreatedAtDateTime.Date >= weekStart && d.CreatedAtDateTime.Date <= weekEnd)
				.ToList();

			// Calculate most frequent mood in selected week
			CalculateMostFrequentMood(weekDiaries);

			// Calculate Overall Wellness Pulse (average of selected week)
			if (weekDiaries.Any())
			{
				AverageSentimentScore = weekDiaries.Average(d => d.SentimentScore);
			}
			else
			{
				AverageSentimentScore = 0;
			}
			OnPropertyChanged(nameof(AverageDisplay));

			// Update Weekly Pulse data (Column Chart)
			for (int i = 0; i < 7; i++)
			{
				var targetDate = weekStart.AddDays(i);
				var diary = weekDiaries.FirstOrDefault(d => d.CreatedAtDateTime.Date == targetDate);

				WeeklyPulseData[i].Day = targetDate.ToString("ddd").Substring(0, 3);
				WeeklyPulseData[i].Score = diary?.SentimentScore ?? 0;
			}

			// Update date range display
			WeekDateRange = $"{weekStart:ddd, dd} - {weekEnd:ddd, dd}";

			// Notify changes
			OnPropertyChanged(nameof(WeeklyPulseData));

			// Calculate Resonating Themes from selected week
			CalculateResonatingThemes(weekDiaries);

			// Show week scores for line chart
			foreach (var d in weekDiaries)
			{
				string day = d.CreatedAtDateTime.ToString("ddd");
				ChartData.Add(new ChartDataPoint(day, d.SentimentScore));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Dashboard LoadSentimentScores Error: {ex.Message}");
			Console.WriteLine($"StackTrace: {ex.StackTrace}");

			// Reset to safe defaults on error
			MostFrequentMoodImage = "empty.png";
			MostFrequentMoodName = "No Data";
			MoodBackgroundColor = Color.FromArgb("#F8FAED");
			AverageSentimentScore = 0;
			WeekDateRange = "";
		}
		finally
		{
			IsLoading = false;
		}
	}

	private void CalculateMostFrequentMood(List<DiaryData> diaries)
	{
		if (!diaries.Any())
		{
			MostFrequentMoodImage = "empty.png";
			MostFrequentMoodName = "No Data";
			MoodBackgroundColor = Color.FromArgb("#F8FAED");
			return;
		}

		// Normalize mood names (Joy/Happiness -> Happiness)
		var moodCounts = diaries
			.Select(d => d.Mood?.ToLower() == "joy" ? "happiness" : d.Mood?.ToLower())
			.Where(m => !string.IsNullOrEmpty(m))
			.GroupBy(m => m)
			.Select(g => new { Mood = g.Key, Count = g.Count() })
			.OrderByDescending(x => x.Count)
			.FirstOrDefault();

		if (moodCounts != null && !string.IsNullOrEmpty(moodCounts.Mood))
		{
			var mood = moodCounts.Mood;
			MostFrequentMoodName = char.ToUpper(mood[0]) + mood.Substring(1);
			MostFrequentMoodImage = mood switch
			{
				"happiness" => "happiness.png",
				"anger" => "anger.png",
				"sadness" => "sadness.png",
				"fear" => "fear.png",
				"love" => "love.png",
				"surprise" => "surprise.png",
				_ => "empty.png"
			};

			// Set background color based on mood
			MoodBackgroundColor = mood switch
			{
				"happiness" => Color.FromArgb("#F5C857"), // Light yellow
				"anger" => Color.FromArgb("#FF5555"), // Light red
				"sadness" => Color.FromArgb("#2B638D"), // Light blue
				"fear" => Color.FromArgb("#662B8D"), // Light purple
				"love" => Color.FromArgb("#FF82E8"), // Light pink
				"surprise" => Color.FromArgb("#DFBC31"), // Light orange
				_ => Color.FromArgb("#F8FAED")
			};
		}
		else
		{
			MostFrequentMoodImage = "empty.png";
			MostFrequentMoodName = "No Data";
			MoodBackgroundColor = Color.FromArgb("#F8FAED");
		}
	}

	private void CalculateResonatingThemes(List<DiaryData> diaries)
	{
		try
		{
			if (ResonatingThemes == null) ResonatingThemes = new ObservableCollection<ThemeData>();

			if (!diaries?.Any() ?? true)
			{
				return;
			}

			// Group by Reason (use "ETC" if empty)
			var reasonGroups = diaries
				.Select(d => string.IsNullOrWhiteSpace(d.Reason) ? "ETC" : d.Reason)
				.GroupBy(r => r)
				.Select(g => new { Reason = g.Key, Count = g.Count() })
				.OrderByDescending(x => x.Count)
				.ToList();

			var total = reasonGroups.Sum(x => x.Count);

			foreach (var group in reasonGroups)
			{
				var percentage = (group.Count / (double)total) * 100;
				ResonatingThemes.Add(new ThemeData
				{
					ThemeName = group.Reason ?? "Unknown",
					Percentage = percentage,
					BackgroundColor = MoodBackgroundColor // Use same color as mood
				});
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Dashboard CalculateResonatingThemes Error: {ex.Message}");
		}
	}

	private void CalculateAverageSentiment(List<DiaryData> diaries)
	{
		if (!diaries.Any())
		{
			AverageSentimentScore = 0;
			return;
		}

		List<DiaryData> filteredDiaries = SelectedPeriod switch
		{
			"3 วันล่าสุด" => diaries.TakeLast(3).ToList(),
			"5 วันล่าสุด" => diaries.TakeLast(5).ToList(),
			"สัปดาห์ที่แล้ว" => diaries.TakeLast(7).ToList(),
			"ทั้งหมด" => diaries,
			_ => diaries
		};

		if (filteredDiaries.Any())
		{
			AverageSentimentScore = filteredDiaries.Average(d => d.SentimentScore);
		}
		else
		{
			AverageSentimentScore = 0;
		}

		OnPropertyChanged(nameof(AverageDisplay));
	}

	private async Task GoToDiaryHistory()
	{
		if (Shell.Current != null)
			await Shell.Current.GoToAsync("//history");
	}

	private async Task PreviousWeek()
	{
		WeekOffset--;
		await LoadSentimentScores();
	}

	private async Task NextWeek()
	{
		// Don't allow going beyond current week
		if (WeekOffset < 0)
		{
			WeekOffset++;
			await LoadSentimentScores();
		}
	}
}
