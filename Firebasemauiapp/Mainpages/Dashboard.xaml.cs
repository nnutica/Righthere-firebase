

namespace Firebasemauiapp.Mainpages;

public partial class Dashboard : ContentPage
{
	public Dashboard(DashboardViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is DashboardViewModel vm)
		{
			await vm.LoadSentimentScoresCommand.ExecuteAsync(null);
		}
	}



}