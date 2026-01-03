namespace Firebasemauiapp.Pages;

public partial class SignInView : ContentPage
{
	public SignInView(SignInViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Clear user authentication when entering Sign In page
		Preferences.Remove("AUTH_UID");
		Preferences.Remove("USER_DISPLAY_NAME");

		System.Diagnostics.Debug.WriteLine("[SignInView] Cleared user authentication data");
	}

	private async void OnGoogleSignInClicked(object sender, EventArgs e)
	{
		await DisplayAlert("Google Sign-In", "ยังไม่พร้อมใช้งาน", "OK");
	}
}