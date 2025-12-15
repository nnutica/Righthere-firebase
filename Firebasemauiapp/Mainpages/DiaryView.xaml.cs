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

		// Subscribe to ImageUrl changes to adjust layout
		_viewModel.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(DiaryViewModel.ImageUrl))
			{
				UpdateLayoutForImage();
			}
		};
	}

	private void UpdateLayoutForImage()
	{
		var hasImage = !string.IsNullOrEmpty(_viewModel.ImageUrl);

		if (hasImage)
		{
			// มีรูป: Editor สั้น, Image Border สูงและขยับขึ้น
			AbsoluteLayout.SetLayoutBounds(EditorBorder, new Rect(0, 58, 1, 230));
			AbsoluteLayout.SetLayoutBounds(ImageBorder, new Rect(0, 298, 1, 340));
		}
		else
		{
			// ไม่มีรูป: Editor ยาว, Image Border เตี้ยและอยู่ล่าง
			AbsoluteLayout.SetLayoutBounds(EditorBorder, new Rect(0, 58, 1, 420));
			AbsoluteLayout.SetLayoutBounds(ImageBorder, new Rect(0, 488, 1, 150));
		}
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

				// Update current date label
				try
				{
					var now = DateTime.Now;
					// Format like "16 Nov, 11:23"
					DateLabel.Text = now.ToString("dd MMM, HH:mm", System.Globalization.CultureInfo.InvariantCulture);
				}
				catch { }
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ DiaryView.OnAppearing error: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
			}
		});
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		try
		{
			// Clear ViewModel data
			_viewModel.ResetDiaryForm();

			// Clear SummaryPageData
			Firebasemauiapp.Helpers.SummaryPageData.Clear();

			// Navigate to Starter page
			await Shell.Current.GoToAsync("//starter");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnBackClicked error: {ex.Message}");
		}
	}

	private void OnCancelClicked(object? sender, EventArgs e)
	{
		try
		{
			// ถ้ามีรูป ให้ลบรูปออก (เพื่อเลือกรูปใหม่)
			if (!string.IsNullOrEmpty(_viewModel.ImageUrl))
			{
				_viewModel.ImageUrl = null;
				System.Diagnostics.Debug.WriteLine("Image cleared");
			}
			// ถ้าไม่มีรูป ไม่ทำอะไร (ให้ใช้ปุ่ม Back ด้านบนแทน)
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnCancelClicked error: {ex.Message}");
		}
	}
}