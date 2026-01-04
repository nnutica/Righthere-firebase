using Firebasemauiapp.Mainpages;


namespace Firebasemauiapp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// Force the app to stay in light mode regardless of the device theme
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}