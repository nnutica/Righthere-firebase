using Firebase.Auth;
using Firebasemauiapp.Data;
using System;

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
		try
		{
			Console.WriteLine("DiaryHistory page appearing, initializing ViewModel...");
			await _viewModel.InitializeAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnAppearing: {ex.Message}");
			await DisplayAlert("Error", $"Failed to initialize diary history: {ex.Message}", "OK");
		}
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//dashboard");
	}
}