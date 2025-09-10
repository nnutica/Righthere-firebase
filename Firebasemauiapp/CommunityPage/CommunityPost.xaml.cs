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

	public ObservableCollection<PostData> Posts { get; } = new();

	private bool _isAdding;
	public bool IsAdding
	{
		get => _isAdding;
		set { if (_isAdding != value) { _isAdding = value; base.OnPropertyChanged(); base.OnPropertyChanged(nameof(NotAdding)); } }
	}

	private bool _isLoading;
	public bool IsLoading
	{
		get => _isLoading;
		set { if (_isLoading != value) { _isLoading = value; base.OnPropertyChanged(); } }
	}

	public bool NotAdding => !IsAdding;
	public bool IsEmpty => Posts.Count == 0;
	public bool NotEmpty => !IsEmpty;

	public ICommand LikeCommand { get; }

	public CommunityPost()
	{
		InitializeComponent();
		BindingContext = this;

		var services = Application.Current?.Handler?.MauiContext?.Services;
		_postDb = services?.GetService(typeof(PostDatabase)) as PostDatabase ?? throw new InvalidOperationException("PostDatabase service not available");
		_auth = services?.GetService(typeof(FirebaseAuthClient)) as FirebaseAuthClient ?? throw new InvalidOperationException("FirebaseAuthClient service not available");

		_userName = _auth.User?.Info?.Email ?? _auth.User?.Info?.DisplayName ?? "Guest";
		UserLabel.Text = $"Posting as: {_userName}";

		LikeCommand = new Command<PostData>(async (post) => await LikeAsync(post));
		_ = LoadPostsAsync();
	}

	public CommunityPost(string userName, PostDatabase postDb, FirebaseAuthClient auth)
	{
		InitializeComponent();
		BindingContext = this;
		_userName = userName;
		_postDb = postDb;
		_auth = auth;
		UserLabel.Text = $"Posting as: {userName}";

		LikeCommand = new Command<PostData>(async (post) => await LikeAsync(post));

		_ = LoadPostsAsync();
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
			await CreatePostAsync(content);
			IsAdding = false;
			PostContentEditor.Text = string.Empty;
		}
		else
		{
			// Show validation message
			await DisplayAlert("Validation", "Please enter some content for your post.", "OK");
		}
	}

	private void OnAddPostToggleClicked(object sender, EventArgs e)
	{
		IsAdding = true;
	}

	private void OnCancelAddPostClicked(object sender, EventArgs e)
	{
		IsAdding = false;
		PostContentEditor.Text = string.Empty;
	}

	private async Task DisplayAlert(string title, string message, string cancel)
	{
		if (Shell.Current != null)
		{
			await Shell.Current.DisplayAlert(title, message, cancel);
		}
	}

	private async Task LoadPostsAsync()
	{
		try
		{
			IsLoading = true;
			Posts.Clear();
			var list = await _postDb.GetAllPostsAsync();
			foreach (var p in list.OrderByDescending(p => p.CreatedAt))
			{
				Posts.Add(p);
			}
			base.OnPropertyChanged(nameof(IsEmpty));
			base.OnPropertyChanged(nameof(NotEmpty));
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to load posts: {ex.Message}", "OK");
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task CreatePostAsync(string content)
	{
		try
		{
			var author = _auth.User?.Info?.DisplayName ?? _userName;
			var newPost = new PostData
			{
				Content = content,
				Author = string.IsNullOrWhiteSpace(author) ? "Anonymous" : author,
				Likes = 0,
				CreatedAt = DateTime.UtcNow
			};
			await _postDb.CreatePostAsync(newPost);
			// Insert on top
			Posts.Insert(0, newPost);
			base.OnPropertyChanged(nameof(IsEmpty));
			base.OnPropertyChanged(nameof(NotEmpty));
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to create post: {ex.Message}", "OK");
		}
	}

	private async Task LikeAsync(PostData? post)
	{
		if (post == null) return;
		try
		{
			var index = Posts.IndexOf(post);
			if (index < 0) return;
			// Clone to trigger UI update
			var updated = new PostData
			{
				PostId = post.PostId,
				Author = post.Author,
				Content = post.Content,
				CreatedAt = post.CreatedAt,
				Likes = post.Likes + 1
			};
			await _postDb.UpdatePostAsync(updated);
			Posts[index] = updated;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to like post: {ex.Message}", "OK");
		}
	}

	// No custom INotifyPropertyChanged needed; we use base.OnPropertyChanged from BindableObject
}