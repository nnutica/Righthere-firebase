namespace Firebasemauiapp.Summary;

public partial class SaveConfirmationPopup : ContentView
{
    private TaskCompletionSource<bool>? _taskCompletionSource;

    public SaveConfirmationPopup()
    {
        InitializeComponent();
    }

    public Task<bool> ShowAsync()
    {
        _taskCompletionSource = new TaskCompletionSource<bool>();
        this.IsVisible = true;
        return _taskCompletionSource.Task;
    }

    private void OnKeepItClicked(object sender, EventArgs e)
    {
        this.IsVisible = false;
        _taskCompletionSource?.TrySetResult(true);
    }

    private void OnLetItGoClicked(object sender, EventArgs e)
    {
        this.IsVisible = false;
        _taskCompletionSource?.TrySetResult(false);
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        // Optional: close popup when tapping outside
        // Uncomment if you want this behavior
        // this.IsVisible = false;
        // _taskCompletionSource?.TrySetResult(false);
    }
}
