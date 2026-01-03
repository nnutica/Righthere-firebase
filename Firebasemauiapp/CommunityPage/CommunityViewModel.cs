using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using CommunityToolkit.Maui.Views;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Microsoft.Maui.Storage;

namespace Firebasemauiapp.CommunityPage;

public partial class CommunityViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly PostDatabase _postDb;

    private string _userName = "Guest";
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => SetProperty(ref _isLoggedIn, value);
    }

    public CommunityViewModel(FirebaseAuthClient authClient, PostDatabase postDb)
    {
        _authClient = authClient;
        _postDb = postDb;
        LoadUserInfoCommand = new AsyncRelayCommand(LoadUserInfo);
        ShowPostOverlayCommand = new AsyncRelayCommand(ShowPostOverlay);
        ClosePostOverlayCommand = new RelayCommand(() => ClosePostOverlay());
        LikeCommand = new AsyncRelayCommand(LikeAsync);
        RefreshPostCommand = new AsyncRelayCommand(LoadRandomPostAsync);
        ShowCreatePostOverlayCommand = new RelayCommand(ShowCreatePostOverlay);
        CloseCreatePostOverlayCommand = new RelayCommand(() => IsCreatePostOverlayVisible = false);
        CreatePostCommand = new AsyncRelayCommand(CreatePostAsync);
    }

    public IAsyncRelayCommand LoadUserInfoCommand { get; }
    public IAsyncRelayCommand ShowPostOverlayCommand { get; }
    public IRelayCommand ClosePostOverlayCommand { get; }
    public IAsyncRelayCommand LikeCommand { get; }
    public IAsyncRelayCommand RefreshPostCommand { get; }
    public IRelayCommand ShowCreatePostOverlayCommand { get; }
    public IRelayCommand CloseCreatePostOverlayCommand { get; }
    public IAsyncRelayCommand CreatePostCommand { get; }

    // Overlay state
    private bool _isPostOverlayVisible;
    public bool IsPostOverlayVisible
    {
        get => _isPostOverlayVisible;
        set => SetProperty(ref _isPostOverlayVisible, value);
    }

    // Create Post Overlay state
    private bool _isCreatePostOverlayVisible;
    public bool IsCreatePostOverlayVisible
    {
        get => _isCreatePostOverlayVisible;
        set => SetProperty(ref _isCreatePostOverlayVisible, value);
    }

    private string _newPostContent = string.Empty;
    public string NewPostContent
    {
        get => _newPostContent;
        set => SetProperty(ref _newPostContent, value);
    }

    // Random post data
    private PostData? _randomPost;
    public PostData? RandomPost
    {
        get => _randomPost;
        set
        {
            if (SetProperty(ref _randomPost, value))
            {
                OnPropertyChanged(nameof(HasPost));
                OnPropertyChanged(nameof(NoPost));
            }
        }
    }
    public bool HasPost => RandomPost != null;
    public bool NoPost => RandomPost == null;

    private Task LoadUserInfo()
    {
        var user = _authClient.User;
        if (user != null)
        {
            UserName = user.Info.DisplayName ?? user.Info.Email ?? "Unknown User";
            IsLoggedIn = true;
        }
        else
        {
            UserName = "Guest";
            IsLoggedIn = false;
        }
        return Task.CompletedTask;
    }

    private async Task ShowPostOverlay()
    {
        IsPostOverlayVisible = true;
        await LoadRandomPostAsync();
    }

    private Task ClosePostOverlay()
    {
        IsPostOverlayVisible = false;
        RandomPost = null;
        return Task.CompletedTask;
    }

    private async Task LoadRandomPostAsync()
    {
        try
        {
            var allPosts = await _postDb.GetAllPostsAsync();
            var list = allPosts;
            if (IsLoggedIn && !string.Equals(UserName, "Guest", StringComparison.OrdinalIgnoreCase))
            {
                list = allPosts
                    .Where(p => !string.Equals(p.Author, UserName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (list.Any())
            {
                var random = new Random();
                var currentId = RandomPost?.PostId;
                var filtered = list;
                if (list.Count > 1 && currentId != null)
                {
                    filtered = list.Where(p => p.PostId != currentId).ToList();
                }
                RandomPost = filtered[random.Next(filtered.Count)];
            }
            else
            {
                RandomPost = null;
            }
        }
        catch (Exception ex)
        {
            // Optionally log ex
        }
    }

    private string GetCurrentUserId()
    {
        var uid = _authClient?.User?.Uid;
        if (!string.IsNullOrWhiteSpace(uid))
            return uid;
        const string key = "guest_user_id";
        var local = Preferences.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(local))
        {
            local = Guid.NewGuid().ToString();
            Preferences.Set(key, local);
        }
        return local;
    }

    private async Task LikeAsync()
    {
        if (RandomPost == null) return;
        try
        {
            var userId = GetCurrentUserId();
            var hasLiked = await _postDb.HasUserLikedAsync(RandomPost.PostId, userId);

            if (hasLiked)
            {
                // Unlike: remove like
                var success = await _postDb.UnlikePostAsync(RandomPost.PostId, userId);
                if (success)
                {
                    var refreshed = await _postDb.GetPostByIdAsync(RandomPost.PostId);
                    if (refreshed != null)
                        RandomPost = refreshed;
                    else
                        RandomPost.Likes = Math.Max(0, RandomPost.Likes - 1); // optimistic
                }
            }
            else
            {
                // Like: add like
                var success = await _postDb.TryLikeOnceAsync(RandomPost.PostId, userId);
                if (success)
                {
                    var refreshed = await _postDb.GetPostByIdAsync(RandomPost.PostId);
                    if (refreshed != null)
                        RandomPost = refreshed;
                    else
                        RandomPost.Likes += 1; // optimistic
                }
            }
        }
        catch { }
    }

    private void ShowCreatePostOverlay()
    {
        NewPostContent = string.Empty;
        IsCreatePostOverlayVisible = true;
    }

    private async Task CreatePostAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPostContent))
            return;

        try
        {
            var user = _authClient.User;
            var userId = user?.Uid ?? "Guest";
            
            var newPost = new PostData
            {
                Content = NewPostContent,
                Author = userId,
                Likes = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _postDb.CreatePostAsync(newPost);
            NewPostContent = string.Empty;
            IsCreatePostOverlayVisible = false;
        }
        catch { }
    }
}
