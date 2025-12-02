using Firebasemauiapp.Mainpages;
namespace Firebasemauiapp.Mainpages;

public partial class SummaryView : ContentPage
{
    public SummaryView(SummaryViewModel viewModel)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = viewModel;

        // Set popup callback
        if (viewModel != null)
        {
            viewModel.ShowSavePopup = ShowSaveConfirmationPopup;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SummaryViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    public Task<bool> ShowSaveConfirmationPopup()
    {
        return SavePopup.ShowAsync();
    }
}
