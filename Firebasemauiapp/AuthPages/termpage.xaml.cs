namespace Firebasemauiapp.Pages;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class TermPage : ContentPage
{
	public TermPage(TermPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}