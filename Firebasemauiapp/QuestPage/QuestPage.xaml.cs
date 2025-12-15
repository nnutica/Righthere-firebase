using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.QuestPage;

public partial class QuestPage : ContentPage
{
    private readonly QuestViewModel _viewModel;

    public QuestPage(QuestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Refresh quest data when page appears
        _viewModel?.RefreshUserInfo();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//starter");
    }
}
