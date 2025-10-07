using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Microsoft.Maui.ApplicationModel;

namespace Firebasemauiapp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register route mappings
		Routing.RegisterRoute("signin", typeof(SignInView));
		Routing.RegisterRoute("signup", typeof(SignUpView));
		Routing.RegisterRoute("main/starter", typeof(StarterView));
		Routing.RegisterRoute("main/diary", typeof(DiaryView));
		Routing.RegisterRoute("main/history", typeof(DiaryHistory));
		Routing.RegisterRoute("main/summary", typeof(SummaryView));
		Routing.RegisterRoute("main/history", typeof(DiaryHistory));
	}
}
