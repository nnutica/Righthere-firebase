using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class StarterView : ContentPage
{
	private readonly DiaryDatabase _diaryDatabase;
	private readonly FirebaseAuthClient _authClient;
	private readonly StarterViewModel _viewModel;

	public StarterView(StarterViewModel viewModel, DiaryDatabase diaryDatabase, FirebaseAuthClient authClient)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
		NavigationPage.SetHasNavigationBar(this, true);
		NavigationPage.SetHasBackButton(this, false);
		Title = "";
		_diaryDatabase = diaryDatabase;
		_authClient = authClient;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		// Ensure the latest signed-in user's info is reflected when returning to this page
		_viewModel?.RefreshUserInfo();
		// Subscribe to property changes to drive animations
		if (_viewModel != null)
			_viewModel.PropertyChanged += ViewModel_PropertyChanged;
		ResetMenuInstant();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		if (_viewModel != null)
			_viewModel.PropertyChanged -= ViewModel_PropertyChanged;
	}

	private bool _menuOpen;
	private bool _animating;

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(StarterViewModel.IsMenuOpen)) return;
		if (_viewModel.IsMenuOpen && !_menuOpen)
		{
			ShowMenuInstant();
		}
		else if (!_viewModel.IsMenuOpen && _menuOpen)
		{
			ResetMenuInstant();
		}
	}

	private async Task OpenMenuAsync()
	{
		// Cancel any ongoing animations to avoid conflicts
		Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(YourDiaryStack);
		Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(WriteDiaryStack);

		// Set initial hidden state to ensure consistent start
		YourDiaryStack.Opacity = 0;
		YourDiaryStack.Scale = 0.85;
		YourDiaryStack.TranslationX = 0;
		YourDiaryStack.TranslationY = 12;
		WriteDiaryStack.Opacity = 0;
		WriteDiaryStack.Scale = 0.85;
		WriteDiaryStack.TranslationX = 0;
		WriteDiaryStack.TranslationY = 12;

		// Make items visible before animating
		YourDiaryStack.IsVisible = true;
		WriteDiaryStack.IsVisible = true;

		// Let taps go through plant button while menu open
		PlantButton.InputTransparent = true;

		_menuOpen = true;

		var tasks = new List<Task>(6)
		{
			YourDiaryStack.FadeTo(1, 120, Easing.CubicOut),
			YourDiaryStack.ScaleTo(1, 140, Easing.CubicOut),
			YourDiaryStack.TranslateTo(0, -48, 140, Easing.CubicOut),
			WriteDiaryStack.FadeTo(1, 120, Easing.CubicOut),
			WriteDiaryStack.ScaleTo(1, 140, Easing.CubicOut),
			WriteDiaryStack.TranslateTo(-64, 0, 140, Easing.CubicOut)
		};
		await Task.WhenAll(tasks);
	}

	private async Task CloseMenuAsync()
	{
		// Cancel any ongoing animations
		Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(YourDiaryStack);
		Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(WriteDiaryStack);

		var tasks = new List<Task>(6)
		{
			YourDiaryStack.FadeTo(0, 90, Easing.CubicIn),
			YourDiaryStack.ScaleTo(.85, 90, Easing.CubicIn),
			YourDiaryStack.TranslateTo(0, 12, 90, Easing.CubicIn),
			WriteDiaryStack.FadeTo(0, 90, Easing.CubicIn),
			WriteDiaryStack.ScaleTo(.85, 90, Easing.CubicIn),
			WriteDiaryStack.TranslateTo(0, 12, 90, Easing.CubicIn)
		};
		await Task.WhenAll(tasks);
		YourDiaryStack.IsVisible = false;
		WriteDiaryStack.IsVisible = false;
		PlantButton.InputTransparent = false;
		_menuOpen = false;
	}


	private void ResetMenuInstant()
	{
		YourDiaryStack.IsVisible = false;
		WriteDiaryStack.IsVisible = false;
		YourDiaryStack.Opacity = 0;
		WriteDiaryStack.Opacity = 0;
		YourDiaryStack.Scale = .85;
		WriteDiaryStack.Scale = .85;
		YourDiaryStack.TranslationX = 0;
		WriteDiaryStack.TranslationX = 0;
		YourDiaryStack.TranslationY = 12;
		WriteDiaryStack.TranslationY = 12;
		PlantButton.InputTransparent = false;
		_menuOpen = false;
		_animating = false;
	}

	private void ShowMenuInstant()
	{
		YourDiaryStack.IsVisible = true;
		WriteDiaryStack.IsVisible = true;
		YourDiaryStack.Opacity = 1;
		WriteDiaryStack.Opacity = 1;
		YourDiaryStack.Scale = 1;
		WriteDiaryStack.Scale = 1;
		YourDiaryStack.TranslationX = 0;
		WriteDiaryStack.TranslationX = -64; // position like before
		YourDiaryStack.TranslationY = -48;
		WriteDiaryStack.TranslationY = 0;
		// Keep plant button tappable so user can close the menu easily
		PlantButton.InputTransparent = false;
		_menuOpen = true;
	}

}