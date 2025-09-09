using CommunityToolkit.Maui.Views;

namespace Firebasemauiapp.CommunityPage;

public partial class CommunityPost : Popup
{
	public CommunityPost(string userName)
	{
		InitializeComponent();
		UserLabel.Text = $"Posting as: {userName}";
	}

	private void OnCloseClicked(object sender, EventArgs e)
	{
		CloseAsync();
	}

	private void OnCancelClicked(object sender, EventArgs e)
	{
		CloseAsync();
	}

	private async void OnPostClicked(object sender, EventArgs e)
	{
		var content = PostContentEditor.Text;
		if (!string.IsNullOrWhiteSpace(content))
		{
			// TODO: Save post to backend
			await CloseAsync();
		}
		else
		{
			// Show validation message
			await DisplayAlert("Validation", "Please enter some content for your post.", "OK");
		}
	}

	private async Task DisplayAlert(string title, string message, string cancel)
	{
		if (Shell.Current != null)
		{
			await Shell.Current.DisplayAlert(title, message, cancel);
		}
	}
}