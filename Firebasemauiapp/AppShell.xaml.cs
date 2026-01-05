using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;

namespace Firebasemauiapp;

public partial class AppShell : Shell
{
	private static bool _isProcessingBackButton = false;

	public AppShell()
	{
		InitializeComponent();

		// Register route mappings
		Routing.RegisterRoute("signin", typeof(SignInView));
		Routing.RegisterRoute("signup", typeof(SignUpView));
		Routing.RegisterRoute("starter", typeof(StarterView));
		Routing.RegisterRoute("diary", typeof(SelectMoodPage));
		Routing.RegisterRoute("history", typeof(DiaryHistory));
		Routing.RegisterRoute("SummaryMockView", typeof(SummaryMockView));
		Routing.RegisterRoute("summary", typeof(SummaryView));
		Routing.RegisterRoute("levelmood", typeof(LevelMoodPage));
		Routing.RegisterRoute("write", typeof(DiaryView));
		Routing.RegisterRoute("createpost", typeof(CommunityPage.CommunityCreatPostPage));

	}

	protected override bool OnBackButtonPressed()
	{
		if (_isProcessingBackButton)
		{
			System.Diagnostics.Debug.WriteLine("⏳ Back button already processing...");
			return true;
		}

		_isProcessingBackButton = true;

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				var currentRoute = Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
				System.Diagnostics.Debug.WriteLine($"🔍 Current route: {currentRoute}");

				// Handle specific routes with custom back navigation
				if (currentRoute.Contains("signup"))
				{
					System.Diagnostics.Debug.WriteLine("🔙 SignUp page detected - navigating to SignIn");
					await Shell.Current.GoToAsync("//signin", false);
				}
				else
				{
					var nav = Shell.Current?.Navigation;
					if (nav != null && nav.NavigationStack.Count > 1)
					{
						System.Diagnostics.Debug.WriteLine($"🔙 Popping page. Stack before pop: {nav.NavigationStack.Count}");
						await nav.PopAsync();
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("⚠️  No stack to pop, going to starter");
						await Shell.Current.GoToAsync("//starter", false);
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"❌ OnBackButtonPressed error: {ex.Message}");
				try
				{
					await Shell.Current.GoToAsync("//starter", false);
				}
				catch { }
			}
			finally
			{
				_isProcessingBackButton = false;
			}
		});

		return true;
	}
}
