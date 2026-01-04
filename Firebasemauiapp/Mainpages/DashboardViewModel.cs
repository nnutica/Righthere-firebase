using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Data;
using Firebase.Auth;
using Firebasemauiapp.Model;
using Microsoft.Maui.Storage; // ✅ [A] added

namespace Firebasemauiapp.Mainpages;

public partial class DashboardViewModel : ObservableObject
{
    public event EventHandler? DataLoaded;
	// Chart property and related logic removed
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;

	private List<DiaryData> _cachedAllDiaries = new();

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
        [NotifyPropertyChangedFor(nameof(ScoreText))]
		private double _score;

		public string ScoreText => Score > 0 ? Score.ToString("F0") : "";
	}

	public class ThemeData
	{
		public string ThemeName { get; set; } = "";
		public int Count { get; set; }
		public double Percentage { get; set; }
        public double PercentageDecimal => Percentage / 100.0;
		public string PercentageText => $"{Percentage:0.##}%";
		public Color BackgroundColor { get; set; } = Colors.LightGray;
        public Color TrackColor { get; set; } = Colors.Gray;
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

	// ✅ [A] only: wait for uid, but fallback to Preferences("AUTH_UID")
	private async Task<string?> WaitForUidAsync(int timeoutMs = 2500)
	{
		var start = Environment.TickCount;

		while (Environment.TickCount - start < timeoutMs)
		{
			// 1) Primary: from auth client
			var uid = _authClient?.User?.Uid;
			if (!string.IsNullOrWhiteSpace(uid))
				return uid;

			// 2) Fallback: from cached uid (saved at sign-in)
			var cachedUid = Preferences.Get("AUTH_UID", null);
			if (!string.IsNullOrWhiteSpace(cachedUid))
				return cachedUid;

			await Task.Delay(150);
		}

		// Final fallback
		var lastCached = Preferences.Get("AUTH_UID", null);
		return string.IsNullOrWhiteSpace(lastCached) ? null : lastCached;
	}

	private int _retryCount = 0;
	private const int MaxRetry = 3;
	private async Task LoadSentimentScores()
	{
		try
		{
			IsLoading = true;

			if (SentimentScores == null) SentimentScores = new ObservableCollection<double>();
			if (ChartData == null) ChartData = new ObservableCollection<ChartDataPoint>();
			if (ResonatingThemes == null) ResonatingThemes = new ObservableCollection<ThemeData>();

			SentimentScores.Clear();
			ChartData.Clear();
			ResonatingThemes.Clear();

			var uid = await WaitForUidAsync();
			if (string.IsNullOrWhiteSpace(uid))
			{
				_retryCount++;
				Console.WriteLine($"Dashboard: UID null. Retry {_retryCount}/{MaxRetry}");
				if (_retryCount <= MaxRetry)
				{
					await Task.Delay(1000);
					await LoadSentimentScores();
					return;
				}
				else
				{
					_retryCount = 0;
					try
					{
						MainThread.BeginInvokeOnMainThread(async () =>
						{
							await Application.Current?.MainPage?.DisplayAlert(
								"เกิดข้อผิดพลาด",
								"ไม่สามารถโหลดข้อมูลผู้ใช้ได้",
								"ตกลง");
						});
					}
					catch { }
					IsLoading = false;
					return;
				}
			}
			_retryCount = 0;

			Console.WriteLine($"Dashboard: Loading diaries for user: {uid} (Offset: {WeekOffset})");
			var diaries = (await _diaryDatabase.GetDiariesByUserAsync(uid))
				?.OrderBy(x => x.CreatedAtDateTime)
				.ToList();

			if (diaries == null) diaries = new List<DiaryData>();

			foreach (var d in diaries)
			{
				SentimentScores.Add(d.SentimentScore);
			}

			// Calculate week range
			var today = DateTime.Today;
			var currentDayOfWeek = (int)today.DayOfWeek; 
			var currentWeekStart = today.AddDays(-currentDayOfWeek);
			var weekStart = currentWeekStart.AddDays(WeekOffset * 7);
			var weekEnd = weekStart.AddDays(6);
            
            Console.WriteLine($"[LoadSentimentScores] Filter Range: {weekStart:d} - {weekEnd:d}");

			var weekDiaries = diaries
				.Where(d => d.CreatedAtDateTime.Date >= weekStart && d.CreatedAtDateTime.Date <= weekEnd)
				.ToList();
            
            Console.WriteLine($"[LoadSentimentScores] Found {weekDiaries.Count} items.");

			CalculateMostFrequentMood(weekDiaries);

			if (weekDiaries.Any())
			{
				AverageSentimentScore = weekDiaries.Average(d => d.SentimentScore);
			}
			else
			{
				AverageSentimentScore = 0;
			}
			OnPropertyChanged(nameof(AverageDisplay));

			for (int i = 0; i < 7; i++)
			{
				var targetDate = weekStart.AddDays(i);
				var diary = weekDiaries.FirstOrDefault(d => d.CreatedAtDateTime.Date == targetDate);

				WeeklyPulseData[i].Day = targetDate.ToString("ddd");
				WeeklyPulseData[i].Score = diary?.SentimentScore ?? 0;
			}
            OnPropertyChanged(nameof(WeeklyPulseData));

			WeekDateRange = $"{weekStart:ddd, dd} - {weekEnd:ddd, dd}";

			CalculateResonatingThemes(weekDiaries);

			foreach (var d in weekDiaries)
			{
				string day = d.CreatedAtDateTime.ToString("ddd");
				ChartData.Add(new ChartDataPoint(day, d.SentimentScore));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Dashboard Error: {ex.Message}");
			MostFrequentMoodImage = "empty.png";
			MostFrequentMoodName = "No Data";
			MoodBackgroundColor = Color.FromArgb("#F8FAED");
			AverageSentimentScore = 0;
			WeekDateRange = "";
		}
		finally
		{
			IsLoading = false;
			DataLoaded?.Invoke(this, EventArgs.Empty);
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
				"happiness" => Color.FromArgb("#FBC30A"), // Light yellow
				"angry" => Color.FromArgb("#E4000F"), // Light red
				"sadness" => Color.FromArgb("#2B638D"), // Light blue
				"fear" => Color.FromArgb("#9E9AAB"), // Light purple
				"love" => Color.FromArgb("#FF60A0"), // Light pink
				"disgust" => Color.FromArgb("#1EA064"), // Light orange
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

			// Extract and count keywords from all diaries in the week
			var keywordCounts = new Dictionary<string, int>();

			foreach (var diary in diaries)
			{
				if (string.IsNullOrWhiteSpace(diary.Keywords))
					continue;

				// Split keywords by comma and clean up
				var keywords = diary.Keywords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(k => k.Trim())
					.Where(k => !string.IsNullOrWhiteSpace(k));

				foreach (var keyword in keywords)
				{
					if (keywordCounts.ContainsKey(keyword))
						keywordCounts[keyword]++;
					else
						keywordCounts[keyword] = 1;
				}
			}

			// Get top 3 keywords by count
			var top3Keywords = keywordCounts
				.OrderByDescending(x => x.Value)
				.Take(3)
				.ToList();

			if (!top3Keywords.Any())
			{
				return;
			}

			int total = keywordCounts.Values.Sum();

			// Define colors for 1st, 2nd, 3rd place (Progress Color, Track Color)
			var rankColors = new[]
			{
				(Progress: Color.FromArgb("#EFDFC7"), Track: Color.FromArgb("#B0BEC5")), // 1st: Beige / BlueGrey
				(Progress: Color.FromArgb("#00E676"), Track: Color.FromArgb("#2E7D32")), // 2nd: Bright Green / Dark Green
				(Progress: Color.FromArgb("#CFD8DC"), Track: Color.FromArgb("#90A4AE"))  // 3rd: Light Grey / BlueGrey
			};

			// Add themes with their respective colors
			for (int i = 0; i < top3Keywords.Count; i++)
			{
				var keyword = top3Keywords[i];
				var percentage = (keyword.Value / (double)total) * 100;
                // Use safe default if more than 3 items (though we Take(3) above)
                var colors = i < rankColors.Length ? rankColors[i] : (Progress: Colors.LightGray, Track: Colors.Gray);

				ResonatingThemes.Add(new ThemeData
				{
					ThemeName = keyword.Key,
					Count = keyword.Value,
					Percentage = percentage,
					BackgroundColor = colors.Progress,
                    TrackColor = colors.Track
				});
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Dashboard CalculateResonatingThemes Error: {ex.Message}");
		}
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
		if (WeekOffset < 0)
		{
			WeekOffset++;
            await LoadSentimentScores();
		}
	}
}
