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