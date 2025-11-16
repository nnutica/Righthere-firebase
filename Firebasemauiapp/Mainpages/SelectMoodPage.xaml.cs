namespace Firebasemauiapp.Mainpages;

public partial class SelectMoodPage : ContentPage
{
	public SelectMoodPage(MoodViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}