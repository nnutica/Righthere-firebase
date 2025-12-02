namespace Firebasemauiapp.StorePage;

public partial class StorePage : ContentPage
{
	public StorePage(StoreViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

		// Set popup callbacks
		if (viewModel != null)
		{
			viewModel.ShowConfirmationPopup = ShowConfirmationPopup;
			viewModel.ShowAlertPopup = ShowAlertPopup;
		}
	}

	public Task<bool> ShowConfirmationPopup(string title, string message, string confirmText, string cancelText)
	{
		// Use lowercase 'p' to reference the instance from XAML
		return ((PurchasePopup)this.FindByName("PurchasePopup")).ShowConfirmationAsync(title, message, confirmText, cancelText);
	}

	public Task<bool> ShowAlertPopup(string title, string message, string buttonText, bool isError)
	{
		// Use lowercase 'p' to reference the instance from XAML
		return ((PurchasePopup)this.FindByName("PurchasePopup")).ShowAlertAsync(title, message, buttonText, isError);
	}
}