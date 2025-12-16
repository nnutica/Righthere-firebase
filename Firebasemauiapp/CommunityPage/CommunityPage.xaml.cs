using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
namespace Firebasemauiapp.CommunityPage;

public partial class CommunityPage : ContentPage
{
	public CommunityPage(CommunityViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is CommunityViewModel vm)
		{
			await vm.LoadUserInfoCommand.ExecuteAsync(null);
		}
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//starter");
	}

	private async void OnShareLoveClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("createpost");
	}
}

