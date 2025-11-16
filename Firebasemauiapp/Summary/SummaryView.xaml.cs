namespace Firebasemauiapp.Mainpages;

public partial class SummaryView : ContentPage
{
    public SummaryView(SummaryViewModel viewModel)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SummaryViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
