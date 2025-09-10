using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class StarterView : ContentPage
{
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;
	private readonly StarterViewModel _viewModel;

	public StarterView(StarterViewModel viewModel, DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
		NavigationPage.SetHasNavigationBar(this, true);
		NavigationPage.SetHasBackButton(this, false);
		Title = "";
		// Add Log Out button to Navigation Bar
		ToolbarItems.Clear();
		ToolbarItems.Add(new ToolbarItem
		{
			Text = "Log Out",
			Order = ToolbarItemOrder.Primary,
			Priority = 0,
			Command = viewModel.LogOutCommand
		});
		_diaryDatabase = diaryDatabase;
		_authClient = authClient;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		// Ensure the latest signed-in user's info is reflected when returning to this page
		_viewModel?.RefreshUserInfo();
	}


}