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
		NavigationPage.SetHasNavigationBar(this, false);
		BindingContext = viewModel;
		_diaryDatabase = diaryDatabase;
		_authClient = authClient;
	}

	private async void OnWriteDiaryClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new DiaryView(_diaryDatabase, _authClient));
	}
}