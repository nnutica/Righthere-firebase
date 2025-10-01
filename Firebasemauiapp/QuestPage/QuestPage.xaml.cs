namespace Firebasemauiapp.QuestPage;

public partial class QuestPage : ContentPage
{
	public QuestPage()
	{
		InitializeComponent();

		// Resolve QuestViewModel via DI
		var services = Application.Current?.Handler?.MauiContext?.Services;
		if (services is not null)
		{
			var vm = services.GetService(typeof(QuestViewModel)) as QuestViewModel;
			if (vm != null)
				BindingContext = vm;
		}
	}
}