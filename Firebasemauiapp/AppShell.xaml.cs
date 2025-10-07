using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Microsoft.Maui.ApplicationModel;

namespace Firebasemauiapp;

public partial class AppShell : Shell
{
	private bool _initialNavDone;
	public AppShell()
	{
		InitializeComponent();

		// Register route mappings
		Routing.RegisterRoute("signin", typeof(SignInView));
		Routing.RegisterRoute("signup", typeof(SignUpView));
		Routing.RegisterRoute("main/starter", typeof(StarterView));
		Routing.RegisterRoute("main/diary", typeof(DiaryView));
		Routing.RegisterRoute("main/history", typeof(DiaryHistory));
		Routing.RegisterRoute("main/summary", typeof(SummaryView));
		Routing.RegisterRoute("main/history", typeof(DiaryHistory));

		// Try navigate based on persisted current user
		TryNavigateInitialAsync();
	}

	private async void TryNavigateInitialAsync()
	{
		try
		{
			var auth = ServiceHelper.Get<FirebaseAuthClient>();

			// Wait briefly for a persisted session to load (if any)
			for (int i = 0; i < 10; i++)
			{
				var user = auth.User;
				if (user != null)
				{
					await MainThread.InvokeOnMainThreadAsync(() => GoToAsync("//main/starter"));
					_initialNavDone = true;
					break;
				}
				await Task.Delay(150);
			}

			// Also listen for state changes to redirect appropriately if needed
			auth.AuthStateChanged += async (s, e) =>
			{
				try
				{
					if (auth.User != null)
					{
						if (!_initialNavDone)
							await MainThread.InvokeOnMainThreadAsync(() => GoToAsync("//main/starter"));
						_initialNavDone = true;
					}
					else
					{
						await MainThread.InvokeOnMainThreadAsync(() => GoToAsync("//signin"));
						_initialNavDone = true;
					}
				}
				catch { }
			};
		}
		catch { }
	}
}
