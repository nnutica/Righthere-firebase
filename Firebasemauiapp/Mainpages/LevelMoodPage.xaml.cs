namespace Firebasemauiapp.Mainpages;

public partial class LevelMoodPage : ContentPage
{
	public LevelMoodPage(LevelMoodViewModel viewModel)
	{
		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LevelMoodPage] XAML load exception: {ex}\nInner: {ex.InnerException}");
			throw;
		}
		BindingContext = viewModel;
	}
}