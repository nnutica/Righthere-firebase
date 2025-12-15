namespace Firebasemauiapp.StorePage;

public partial class StorePage : ContentPage
{
	private readonly StoreViewModel _viewModel;

	public StorePage(StoreViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
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

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_viewModel?.Dispose();
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//starter");
	}
}