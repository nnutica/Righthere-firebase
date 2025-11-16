namespace Firebasemauiapp.Mainpages;

public partial class SummaryView_Old : ContentPage
{
	public SummaryView_Old(SummaryViewModel viewModel)
	{
		InitializeComponent();
		NavigationPage.SetHasNavigationBar(this, false);
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is SummaryViewModel viewModel)
		{
			await viewModel.InitializeAsync();
		}
	}
}