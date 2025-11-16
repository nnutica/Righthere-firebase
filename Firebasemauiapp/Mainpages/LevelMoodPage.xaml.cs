namespace Firebasemauiapp.Mainpages;

public partial class LevelMoodPage : ContentPage
{
	public LevelMoodPage(LevelMoodViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}