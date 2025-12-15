using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;

namespace Firebasemauiapp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register route mappings
		Routing.RegisterRoute("signin", typeof(SignInView));
		Routing.RegisterRoute("signup", typeof(SignUpView));
		Routing.RegisterRoute("starter", typeof(StarterView));
		Routing.RegisterRoute("diary", typeof(SelectMoodPage));
		Routing.RegisterRoute("history", typeof(DiaryHistory));
		Routing.RegisterRoute("summary", typeof(SummaryView));
		Routing.RegisterRoute("levelmood", typeof(LevelMoodPage));
		Routing.RegisterRoute("write", typeof(DiaryView));

	}

}
