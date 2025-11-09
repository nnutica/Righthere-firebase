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
	}

	public IAsyncRelayCommand LoadSentimentScoresCommand { get; }
	public IAsyncRelayCommand GoToDiaryHistoryCommand { get; }

	partial void OnSelectedPeriodChanged(string value)
	{
		_ = LoadSentimentScores();
	}

	private async Task LoadSentimentScores()
	{
		try
		{
			IsLoading = true;
			SentimentScores.Clear();
			ChartData.Clear();
			var user = _authClient.User;
			if (user == null) return;
			var diaries = (await _diaryDatabase.GetDiariesByUserAsync(user.Uid))
				.OrderBy(x => x.CreatedAtDateTime)
				.ToList();
			foreach (var d in diaries)
			{
				SentimentScores.Add(d.SentimentScore);
			}

			// Show last 7 scores (or less) with day label
			var last7 = diaries.TakeLast(7).ToList();
			foreach (var d in last7)
			{
				string day = d.CreatedAtDateTime.ToString("ddd");
				ChartData.Add(new ChartDataPoint(day, d.SentimentScore));
			}

			// Calculate average based on selected period
			CalculateAverageSentiment(diaries);
			OnPropertyChanged(nameof(AverageDisplay));
		}
		finally
		{
			IsLoading = false;
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
			await Shell.Current.GoToAsync("//main/history");
	}
}
