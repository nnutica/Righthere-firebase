using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Microsoft.Maui.ApplicationModel;

namespace Firebasemauiapp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register route mappings
		Routing.RegisterRoute("signin", typeof(SignInView));
		Routing.RegisterRoute("signup", typeof(SignUpView));
		Routing.RegisterRoute("main/starter", typeof(StarterView));
		Routing.RegisterRoute("main/diary", typeof(SelectMoodPage));
		Routing.RegisterRoute("main/history", typeof(DiaryHistory));
		Routing.RegisterRoute("main/summary", typeof(SummaryView));
		Routing.RegisterRoute("main/levelmood", typeof(LevelMoodPage));
		Routing.RegisterRoute("main/write", typeof(DiaryView));
	
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		try
		{
			var auth = ServiceHelper.Get<FirebaseAuthClient>();
			auth.SignOut();
			try { SecureStorage.Default.RemoveAll(); } catch { }
			try { Preferences.Default.Clear(); } catch { }
			await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current.GoToAsync("//signin"));
		}
		catch (Exception ex)
		{
			await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK"));
		}
	}
}
