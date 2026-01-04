using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;

namespace Firebasemauiapp;

public partial class AppShell : Shell
{
	private static Stack<string> _navigationHistory = new Stack<string>();
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

		// Track navigation history
		Navigating += static (s, e) =>
		{
			var target = e.Target?.Location?.OriginalString ?? string.Empty;
			if (!string.IsNullOrEmpty(target))
			{
				_navigationHistory.Push(target);
				System.Diagnostics.Debug.WriteLine($"📍 Navigation to: {target} | Stack depth: {_navigationHistory.Count}");
			}
		};
	}

	protected override bool OnBackButtonPressed()
	{
		// ป้องกันการเรียก multiple times
		if (_isProcessingBackButton)
		{
			System.Diagnostics.Debug.WriteLine("⏳ Back button already processing...");
			return true;
		}

		_isProcessingBackButton = true;

		try
		{
			System.Diagnostics.Debug.WriteLine($"🔙 Back button pressed - History stack: {_navigationHistory.Count}");

			// ถ้ามี history > 1 ให้ pop และ navigate กลับไป
			if (_navigationHistory.Count > 1)
			{
				_navigationHistory.Pop(); // ลบ current page
				System.Diagnostics.Debug.WriteLine($"✅ Going back - Stack now has {_navigationHistory.Count} items");

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						await Shell.Current.GoToAsync("..", false);
					}
					finally
					{
						_isProcessingBackButton = false;
					}
				});
				return true;
			}
			else
			{
				// History ว่างหรือมีแค่ 1 item ให้ clear และไปหน้า Starter
				System.Diagnostics.Debug.WriteLine("⚠️  Going to Starter page");
				_navigationHistory.Clear();
				_navigationHistory.Push("//starter");

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						await Shell.Current.GoToAsync("//starter", false);
					}
					finally
					{
						_isProcessingBackButton = false;
					}
				});
				return true;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ OnBackButtonPressed error: {ex.Message}");
			// Error ให้ clear history และไป Starter
			_navigationHistory.Clear();
			_navigationHistory.Push("//starter");

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await Shell.Current.GoToAsync("//starter", false);
				}
				finally
				{
					_isProcessingBackButton = false;
				}
			});
			return true;
		}
	}
}
