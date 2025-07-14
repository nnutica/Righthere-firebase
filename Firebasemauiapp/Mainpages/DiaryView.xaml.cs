using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class DiaryView : ContentPage
{
	private string selectedReason = "friend";
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;

	public DiaryView(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
	{
		InitializeComponent();
		NavigationPage.SetHasNavigationBar(this, false);
		_diaryDatabase = diaryDatabase;
		_authClient = authClient;
	}
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		Console.WriteLine("📍 DiaryPage Appeared");

		if (_authClient.User == null)
		{
			await DisplayAlert("Error", "User not logged in. Redirecting to login...", "OK");
			await Shell.Current.GoToAsync("//signin");
		}
	}

	private void OnReasonButtonClicked(object sender, EventArgs e)
	{
		if (sender is Button button)
		{
			selectedReason = button.Text;
			UpdateReasonButtonStyles();
		}
	}
	private void UpdateReasonButtonStyles()
	{
		FriendButton.BackgroundColor = selectedReason == "friend" ? Colors.DarkGreen : Colors.LightGray;
		FriendButton.TextColor = selectedReason == "friend" ? Colors.White : Colors.DarkGray;

		WorkButton.BackgroundColor = selectedReason == "work" ? Colors.DarkGreen : Colors.LightGray;
		WorkButton.TextColor = selectedReason == "work" ? Colors.White : Colors.DarkGray;

		FamilyButton.BackgroundColor = selectedReason == "family" ? Colors.DarkGreen : Colors.LightGray;
		FamilyButton.TextColor = selectedReason == "family" ? Colors.White : Colors.DarkGray;

		SchoolButton.BackgroundColor = selectedReason == "school" ? Colors.DarkGreen : Colors.LightGray;
		SchoolButton.TextColor = selectedReason == "school" ? Colors.White : Colors.DarkGray;
	}



	private async void OnAnalyzeClicked(object sender, EventArgs e)
	{
		string content = DiaryEntry.Text;
		if (string.IsNullOrWhiteSpace(content))
		{
			await DisplayAlert("Error", "Please write something before analyzing.", "OK");
			return;
		}

		AnalyzeButton.IsEnabled = false;
		AnalyzeButton.Text = "Analyzing...";
		LoadingIndicator.IsVisible = true;
		LoadingIndicator.IsRunning = true;

		var api = new Services.API();
		await api.SendData(content);

		string mood = api.GetMood();
		string suggestion = api.GetSuggestion();
		string keyword = api.GetKeywords();
		string emotion = api.GetEmotionalReflection();
		double score = api.GetScore();
		string reason = selectedReason;

		if (string.IsNullOrWhiteSpace(mood) || string.IsNullOrWhiteSpace(suggestion) || string.IsNullOrWhiteSpace(keyword))
		{
			await DisplayAlert("Error", "Failed to analyze content. Please try again.", "OK");
			AnalyzeButton.IsEnabled = true;
			AnalyzeButton.Text = "Next ➤";
			LoadingIndicator.IsVisible = false;
			LoadingIndicator.IsRunning = false;
			return;
		}

		// ตรวจสอบ user authentication
		var currentUser = _authClient.User;
		if (currentUser == null)
		{
			await DisplayAlert("Error", "User session expired. Please log in again.", "OK");
			await Shell.Current.GoToAsync("//signin");
			return;
		}

		AnalyzeButton.IsEnabled = true;
		AnalyzeButton.Text = "Next ➤";
		LoadingIndicator.IsVisible = false;
		LoadingIndicator.IsRunning = false;

		await Navigation.PushAsync(new SummaryView(_diaryDatabase, _authClient, reason, content, mood, suggestion, keyword, emotion, score.ToString()));
	}
}