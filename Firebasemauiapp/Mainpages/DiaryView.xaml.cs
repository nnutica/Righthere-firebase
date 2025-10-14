using Firebase.Auth;
using Firebasemauiapp.Data;

namespace Firebasemauiapp.Mainpages;

public partial class DiaryView : ContentPage
{
	private readonly DiaryViewModel _viewModel;

	public DiaryView(DiaryViewModel viewModel)
	{
		InitializeComponent();
		NavigationPage.SetHasNavigationBar(this, false);

		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Run async tasks safely without blocking OnAppearing
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				await _viewModel.CheckUserAuthentication();

				// ตรวจสอบว่า SummaryPageData ถูกเครียร์หรือยัง 
				// ถ้าเครียร์แล้วแสดงว่าเพิ่งเซฟไดอารี่มา ให้รีเซ็ตฟอร์ม
				if (string.IsNullOrEmpty(Firebasemauiapp.Helpers.SummaryPageData.Content))
				{
					_viewModel.ResetDiaryForm();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ DiaryView.OnAppearing error: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
			}
		});
	}
}