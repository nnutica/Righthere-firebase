using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class DiaryHistory : ContentPage
{
	private readonly DiaryHistoryViewModel _viewModel;

	public DiaryHistory(DiaryHistoryViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeAsync();
	}
}