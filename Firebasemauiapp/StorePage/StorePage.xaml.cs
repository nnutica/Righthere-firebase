namespace Firebasemauiapp.StorePage;

public partial class StorePage : ContentPage
{
	public StorePage(StoreViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// Refresh coin balance when page appears
		if (BindingContext is StoreViewModel vm)
		{
			// Trigger refresh via reflection or expose a RefreshCommand
			await Task.CompletedTask;
		}
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//starter");
	}
}