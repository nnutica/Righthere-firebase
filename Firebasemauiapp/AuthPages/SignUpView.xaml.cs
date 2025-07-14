namespace Firebasemauiapp.Pages;

public partial class SignUpView : ContentPage
{
	public SignUpView(SignUpViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}