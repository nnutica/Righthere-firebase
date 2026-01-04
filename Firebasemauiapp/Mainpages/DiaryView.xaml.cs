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

		// Subscribe to ImageDisplayUrl changes to adjust layout
		_viewModel.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(DiaryViewModel.ImageDisplayUrl))
			{
				System.Diagnostics.Debug.WriteLine($"[DiaryView.PropertyChanged] ImageDisplayUrl changed to: {_viewModel.ImageDisplayUrl}");
				UpdateLayoutForImage();
				
				// Also update Image.Source directly
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					System.Diagnostics.Debug.WriteLine($"[DiaryView] Setting Image.Source to: {_viewModel.ImageDisplayUrl}");
					
					if (string.IsNullOrWhiteSpace(_viewModel.ImageDisplayUrl))
					{
						UploadedImage.Source = null;
						System.Diagnostics.Debug.WriteLine($"[DiaryView] Image.Source cleared");
					}
					else
					{
						try
						{
							// Download image via HttpClient to avoid format issues
							using var httpClient = new HttpClient();
							System.Diagnostics.Debug.WriteLine($"[DiaryView] Downloading image from: {_viewModel.ImageDisplayUrl}");
							
							var response = await httpClient.GetAsync(_viewModel.ImageDisplayUrl);
							if (response.IsSuccessStatusCode)
							{
								var imageBytes = await response.Content.ReadAsByteArrayAsync();
								System.Diagnostics.Debug.WriteLine($"[DiaryView] Downloaded {imageBytes.Length} bytes");
								
								var stream = new MemoryStream(imageBytes);
								UploadedImage.Source = ImageSource.FromStream(() => stream);
								System.Diagnostics.Debug.WriteLine($"[DiaryView] Image loaded from stream");
							}
							else
							{
								System.Diagnostics.Debug.WriteLine($"[DiaryView] HTTP error: {response.StatusCode}");
								UploadedImage.Source = null;
							}
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine($"[DiaryView] Error loading image: {ex.Message}");
							UploadedImage.Source = null;
						}
					}
				});
			}
		};
	}

	private void UpdateLayoutForImage()
	{
		var hasImage = !string.IsNullOrEmpty(_viewModel.ImageDisplayUrl);
		System.Diagnostics.Debug.WriteLine($"[DiaryView.UpdateLayoutForImage] hasImage: {hasImage}, ImageDisplayUrl: {_viewModel.ImageDisplayUrl}");

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

				// Only reset form if we came back from Summary (diary was saved)
				// If content is NOT empty, it means we just saved - so reset
				if (!string.IsNullOrEmpty(Firebasemauiapp.Helpers.SummaryPageData.Content))
				{
					System.Diagnostics.Debug.WriteLine("[DiaryView.OnAppearing] Resetting form (came from Summary)");
					_viewModel.ResetDiaryForm();
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[DiaryView.OnAppearing] NOT resetting - preserving image: {_viewModel.ImageUrl}");
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
			if (_viewModel.IsImageAreaEnabled)
			{
				// สถานะที่ 2: ปิด Image Area และลบรูป
				_viewModel.ImageUrl = null;
				_viewModel.IsImageAreaEnabled = false;
				System.Diagnostics.Debug.WriteLine("Image Area disabled and image cleared");
			}
			else
			{
				// สถานะที่ 1: เปิด Image Area
				_viewModel.IsImageAreaEnabled = true;
				System.Diagnostics.Debug.WriteLine("Image Area enabled");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnCancelClicked error: {ex.Message}");
		}
	}
}