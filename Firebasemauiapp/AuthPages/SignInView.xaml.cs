namespace Firebasemauiapp.Pages;

public partial class SignInView : ContentPage
{
	public SignInView(SignInViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}

	private async void OnGoogleSignInClicked(object sender, EventArgs e)
	{
		await DisplayAlert("Google Sign-In", "ยังไม่พร้อมใช้งาน", "OK");
	}
}