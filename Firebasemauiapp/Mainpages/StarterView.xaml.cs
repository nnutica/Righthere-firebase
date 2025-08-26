using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class StarterView : ContentPage
{
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;

	public StarterView(StarterViewModel viewModel, DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
	{
		InitializeComponent();
		BindingContext = viewModel;
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


}