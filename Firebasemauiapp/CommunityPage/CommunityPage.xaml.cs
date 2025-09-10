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
}

// Code-behind partial for opening the popup
public partial class CommunityPage
{
	private async void OnOpenCommunityPopupTapped(object? sender, TappedEventArgs e)
	{
		var vm = BindingContext as CommunityViewModel;
		var username = vm?.UserName ?? "Guest";

		// Use parameterless popup that resolves its own services
		var popup = new CommunityPost();
		await this.ShowPopupAsync(popup);
	}
}