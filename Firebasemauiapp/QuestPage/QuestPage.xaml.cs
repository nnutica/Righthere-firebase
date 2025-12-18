using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.QuestPage;

public partial class QuestPage : ContentPage
{
    private readonly QuestViewModel _viewModel;

    public QuestPage(QuestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Refresh quest data when page appears
        _viewModel?.RefreshUserInfo();
        await _viewModel.LoadDailyQuestsAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//starter");
    }

    private async void OnQuestActionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is QuestDatabase quest)
        {
            if (quest.IsClaimed)
            {
                // Already claimed, do nothing
                return;
            }
            else if (quest.IsCompleted || quest.CurrentProgress >= quest.MaxProgress)
            {
                // Claim reward
                await _viewModel.ClaimQuestRewardAsync(quest);
            }
            else
            {
                // Go to quest page (e.g., diary, community)
                if (quest.Title.Contains("diary", StringComparison.OrdinalIgnoreCase))
                {
                    await Shell.Current.GoToAsync("diary"); // Goes to SelectMoodPage
                }
                else if (quest.Title.Contains("love", StringComparison.OrdinalIgnoreCase) ||
                         quest.Title.Contains("share", StringComparison.OrdinalIgnoreCase))
                {
                    await Shell.Current.GoToAsync("//community"); // Goes to CommunityPage
                }
            }
        }
    }
}
