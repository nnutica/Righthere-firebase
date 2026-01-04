

namespace Firebasemauiapp.Mainpages;


public partial class Dashboard : ContentPage
{
	private readonly DashboardViewModel viewModel;

	public Dashboard(DashboardViewModel viewModel)
	{
		InitializeComponent();
		this.viewModel = viewModel;
		BindingContext = viewModel;
        viewModel.DataLoaded += OnDataLoaded;
	}

    private void OnDataLoaded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Delay slightly to ensure layout / loading overlay is gone
            await Task.Delay(250);
            try
            {
                await MainScrollView.ScrollToAsync(WellnessCard, ScrollToPosition.Start, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-scroll error: {ex.Message}");
            }
        });
    }

	protected override async void OnAppearing()
	{
		base.OnAppearing();



		var uid = Preferences.Get("AUTH_UID", null);
		if (string.IsNullOrWhiteSpace(uid))
			return;

		await viewModel.LoadSentimentScoresCommand.ExecuteAsync(null);
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//starter");
	}
}