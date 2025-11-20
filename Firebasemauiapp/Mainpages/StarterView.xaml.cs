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

	// Menu animation helpers were used when the plant was a button.
	// The plant is no longer interactive, so these become safe no-ops
	// that only keep internal state consistent to avoid build errors.
	private void ResetMenuInstant()
	{
		_menuOpen = false;
	}

	private void ShowMenuInstant()
	{
		_menuOpen = true;
	}










}