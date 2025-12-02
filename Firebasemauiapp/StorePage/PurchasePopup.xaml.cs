namespace Firebasemauiapp.StorePage;

public partial class PurchasePopup : ContentView
{
    private TaskCompletionSource<bool>? _taskCompletionSource;
    private bool _isAlertMode = false;

    public PurchasePopup()
    {
        InitializeComponent();
    }

    // Show confirmation dialog (2 buttons)
    public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Purchase", string cancelText = "Cancel")
    {
        _isAlertMode = false;
        _taskCompletionSource = new TaskCompletionSource<bool>();

        // Setup UI
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        IconLabel.Text = "🛒";
        IconBorder.BackgroundColor = Color.FromArgb("#50A65D");

        ConfirmButton.Text = confirmText;
        CancelButton.Text = cancelText;

        // Show both buttons
        ButtonsGrid.ColumnDefinitions.Clear();
        ButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        ButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        Grid.SetColumn(CancelButton, 0);
        Grid.SetColumn(ConfirmButton, 1);
        CancelButton.IsVisible = true;
        ConfirmButton.IsVisible = true;

        this.IsVisible = true;
        return _taskCompletionSource.Task;
    }

    // Show alert (1 button only)
    public Task<bool> ShowAlertAsync(string title, string message, string buttonText = "OK", bool isError = false)
    {
        _isAlertMode = true;
        _taskCompletionSource = new TaskCompletionSource<bool>();

        // Setup UI
        TitleLabel.Text = title;
        MessageLabel.Text = message;

        if (isError)
        {
            IconLabel.Text = "❌";
            IconBorder.BackgroundColor = Color.FromArgb("#FF5252");
        }
        else
        {
            IconLabel.Text = "✅";
            IconBorder.BackgroundColor = Color.FromArgb("#50A65D");
        }

        // Show only confirm button (centered)
        ButtonsGrid.ColumnDefinitions.Clear();
        ButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        Grid.SetColumn(ConfirmButton, 0);
        Grid.SetColumnSpan(ConfirmButton, 1);
        CancelButton.IsVisible = false;
        ConfirmButton.IsVisible = true;
        ConfirmButton.Text = buttonText;

        this.IsVisible = true;
        return _taskCompletionSource.Task;
    }

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        this.IsVisible = false;
        _taskCompletionSource?.TrySetResult(true);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        this.IsVisible = false;
        _taskCompletionSource?.TrySetResult(false);
    }
}
