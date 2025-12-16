namespace Firebasemauiapp.CommunityPage;

public partial class CommunityCreatPostPage : ContentPage
{
	public CommunityCreatPostPage(CreatePostViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}