using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.CommunityPage;

public partial class CommunityPost : Popup
{
	private readonly PostDatabase _postDb;
	private readonly FirebaseAuthClient _auth;
	private readonly string _userName;

	private PostData? _randomPost;
	public PostData? RandomPost
	{
		get => _randomPost;
		set { if (_randomPost != value) { _randomPost = value; base.OnPropertyChanged(); base.OnPropertyChanged(nameof(HasPost)); base.OnPropertyChanged(nameof(NoPost)); } }
	}

	private bool _isLoading;
	public bool IsLoading
	{
		get => _isLoading;
		set { if (_isLoading != value) { _isLoading = value; base.OnPropertyChanged(); } }
	}

	public bool HasPost => RandomPost != null;
	public bool NoPost => RandomPost == null;

	public ICommand LikeCommand { get; }
	public ICommand RefreshCommand { get; }

	public CommunityPost()
	{
		InitializeComponent();
		BindingContext = this;

		var services = Application.Current?.Handler?.MauiContext?.Services;
		_postDb = services?.GetService(typeof(PostDatabase)) as PostDatabase ?? throw new InvalidOperationException("PostDatabase service not available");
		_auth = services?.GetService(typeof(FirebaseAuthClient)) as FirebaseAuthClient ?? throw new InvalidOperationException("FirebaseAuthClient service not available");

		_userName = _auth.User?.Info?.DisplayName ?? _auth.User?.Info?.Email ?? "Guest";
		UserLabel.Text = $"Viewing as: {_userName}";

		LikeCommand = new Command<PostData>(async (post) => await LikeAsync(post));
		RefreshCommand = new Command(async () => await LoadRandomPostAsync());

		_ = LoadRandomPostAsync();
	}

	public CommunityPost(string userName, PostDatabase postDb, FirebaseAuthClient auth)
	{
		InitializeComponent();
		BindingContext = this;
		_userName = userName;
		_postDb = postDb;
		_auth = auth;
		UserLabel.Text = $"Viewing as: {userName}";

		LikeCommand = new Command<PostData>(async (post) => await LikeAsync(post));
		RefreshCommand = new Command(async () => await LoadRandomPostAsync());

		_ = LoadRandomPostAsync();
	}

	private void OnCloseClicked(object sender, EventArgs e)
	{
		CloseAsync();
	}

	private void OnRefreshClicked(object sender, EventArgs e)
	{
		_ = LoadRandomPostAsync();
	}

	private async void OnLikeClicked(object sender, EventArgs e)
	{
		Console.WriteLine("Like button clicked!");
		await LikeAsync(RandomPost);
	}

	private async Task DisplayAlert(string title, string message, string cancel)
	{
		if (Shell.Current != null)
		{
			await Shell.Current.DisplayAlert(title, message, cancel);
		}
	}

	private async Task LoadRandomPostAsync()
	{
		try
		{
			IsLoading = true;
			RandomPost = null;

			var allPosts = await _postDb.GetAllPostsAsync();
			if (allPosts.Any())
			{
				var random = new Random();
				var randomIndex = random.Next(0, allPosts.Count);
				RandomPost = allPosts[randomIndex];
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to load post: {ex.Message}", "OK");
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LikeAsync(PostData? post)
	{
		post ??= RandomPost;
		if (post == null)
		{
			Console.WriteLine("LikeAsync: No post to like");
			return;
		}

		Console.WriteLine($"LikeAsync: Starting like for post {post.PostId}, current likes: {post.Likes}");

		try
		{
			// Show immediate feedback
			var newLikes = post.Likes + 1;
			RandomPost = new PostData
			{
				PostId = post.PostId,
				Author = post.Author,
				Content = post.Content,
				CreatedAt = post.CreatedAt,
				Likes = newLikes
			};
			Console.WriteLine($"LikeAsync: UI updated to {newLikes} likes");

			// Try to update database
			var success = await _postDb.TryIncrementLikesAsync(post.PostId, 1);
			Console.WriteLine($"LikeAsync: Database update success: {success}");

			if (success)
			{
				// Refresh from database to get accurate count
				var refreshed = await _postDb.GetPostByIdAsync(post.PostId);
				if (refreshed != null)
				{
					Console.WriteLine($"LikeAsync: Refreshed from DB, likes: {refreshed.Likes}");
					RandomPost = refreshed;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"LikeAsync: Error - {ex.Message}");
			await DisplayAlert("Error", $"Failed to like post: {ex.Message}", "OK");
		}
	}
}