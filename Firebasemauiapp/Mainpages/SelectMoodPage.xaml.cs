namespace Firebasemauiapp.Mainpages;

public partial class SelectMoodPage : ContentPage
{
	public SelectMoodPage(MoodViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//starter");
	}
}