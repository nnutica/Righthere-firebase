namespace Firebasemauiapp.Mainpages;

public partial class LevelMoodPage : ContentPage
{
	public LevelMoodPage(LevelMoodViewModel viewModel)
	{
		try
		{
			// Set binding context BEFORE InitializeComponent to ensure data is ready
			BindingContext = viewModel;
			InitializeComponent();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LevelMoodPage] Exception: {ex}\nStack: {ex.StackTrace}");
			if (ex.InnerException != null)
				System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException}\nInner Stack: {ex.InnerException.StackTrace}");
			throw;
		}
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}