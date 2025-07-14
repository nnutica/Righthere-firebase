using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.Mainpages;

public partial class SummaryView : ContentPage
{
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;

	private string mood;
	private string Sugges;
	private string keyword;
	private string Emotion;
	private string content;
	private string score;
	private string reason;
	public SummaryView(DiaryDatabase diaryDatabase, FirebaseAuthClient authClient,
		string reason, string content, string mood, string suggestion,
		string keywords, string emotion, string score)
	{
		InitializeComponent();
		NavigationPage.SetHasNavigationBar(this, false);

		_diaryDatabase = diaryDatabase;
		_authClient = authClient;

		this.reason = reason;
		this.content = content;
		this.mood = mood;
		this.Sugges = suggestion;
		this.keyword = keywords;
		this.Emotion = emotion;
		this.score = score;

		SetEmotionImage(mood);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (Moodtex != null)
			Moodtex.Text = this.mood;  // ✅ อัปเดตค่า Label
		Advicetext.Text = this.Sugges;
		Keywordtext.Text = this.keyword;
		Emotiontext.Text = this.Emotion;
		scoretext.Text = this.score;

	}
	private async void GoMainPage(object sender, EventArgs e)
	{
		// แสดง confirmation dialog
		bool result = await DisplayAlert("Save Diary", "Do you want to save this diary entry?", "Yes", "No");

		if (result)
		{
			try
			{
				var currentUser = _authClient.User;
				if (currentUser == null)
				{
					await DisplayAlert("Error", "User session expired. Please log in again.", "OK");
					await Shell.Current.GoToAsync("//signin");
					return;
				}

				var diary = new DiaryData
				{
					UserId = currentUser.Uid,
					Content = content,
					Reason = reason,
					Mood = mood,
					SentimentScore = double.TryParse(score, out var scoreValue) ? scoreValue : 0.0,
					Suggestion = Sugges,
					Keywords = keyword,
					EmotionalReflection = Emotion,
					CreatedAtDateTime = DateTime.Now
				};

				string diaryId = await _diaryDatabase.SaveDiaryAsync(diary);
				await DisplayAlert("Saved", "Diary entry saved successfully.", "OK");
			}
			catch (Exception ex)
			{
				await DisplayAlert("Error", $"Failed to save diary: {ex.Message}", "OK");
			}
		}

		// Navigate back to starter page
		await Shell.Current.GoToAsync("//starter");
	}
	private void SetEmotionImage(string mood)
	{
		if (string.IsNullOrWhiteSpace(mood))
		{
			EmotionImage.Source = null;
			return;
		}

		// สมมติไฟล์รูปภาพอยู่ในโฟลเดอร์ Resources/Images ของโปรเจกต์ และชื่อไฟล์ตาม mood เช่น joy.png
		string imageName = $"{mood.ToLower()}.png";

		EmotionImage.Source = ImageSource.FromFile(imageName);
	}


}