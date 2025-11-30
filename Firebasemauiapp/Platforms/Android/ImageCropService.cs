using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Firebasemauiapp.Platforms.Android
{
    public class ImageCropService
    {
        private static TaskCompletionSource<string>? _tcs;
        private const int CropRequestCode = 9999;
        private static string? _outputPath;

        public static async Task<string?> CropImageAsync(string imagePath)
        {
            try
            {
                var activity = Platform.CurrentActivity;
                if (activity == null) return null;

                _tcs = new TaskCompletionSource<string>();

                // สร้าง output path
                _outputPath = System.IO.Path.Combine(FileSystem.CacheDirectory, $"cropped_{DateTime.Now.Ticks}.jpg");
                
                var sourceUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                    activity,
                    $"{activity.PackageName}.fileprovider",
                    new Java.IO.File(imagePath)
                );

                var outputUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                    activity,
                    $"{activity.PackageName}.fileprovider",
                    new Java.IO.File(_outputPath)
                );

                var cropIntent = new Intent("com.android.camera.action.CROP");
                cropIntent.SetDataAndType(sourceUri, "image/*");
                cropIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                cropIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                cropIntent.PutExtra("crop", "true");
                cropIntent.PutExtra("aspectX", 4);
                cropIntent.PutExtra("aspectY", 3);
                cropIntent.PutExtra("outputX", 800);
                cropIntent.PutExtra("outputY", 600);
                cropIntent.PutExtra("scale", true);
                cropIntent.PutExtra("return-data", false);
                cropIntent.PutExtra(MediaStore.ExtraOutput, outputUri);
                cropIntent.PutExtra("outputFormat", Bitmap.CompressFormat.Jpeg.ToString());

                // ลอง resolve ว่ามี app ที่รองรับหรือไม่
                var packageManager = activity.PackageManager;
                if (packageManager != null && cropIntent.ResolveActivity(packageManager) != null)
                {
                    activity.StartActivityForResult(cropIntent, CropRequestCode);
                    var result = await _tcs.Task;
                    return result;
                }
                else
                {
                    // ถ้าไม่มี app รองรับ ให้ crop เองด้วย Bitmap
                    return await CropImageManually(imagePath, _outputPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Crop error: {ex.Message}");
                // ถ้า error ลองใช้ manual crop
                try
                {
                    return await CropImageManually(imagePath, _outputPath);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static async Task<string> CropImageManually(string sourcePath, string outputPath)
        {
            return await Task.Run(() =>
            {
                using var originalBitmap = BitmapFactory.DecodeFile(sourcePath);
                if (originalBitmap == null) return sourcePath;

                int width = originalBitmap.Width;
                int height = originalBitmap.Height;
                
                // คำนวณขนาดที่ต้องการ (4:3 aspect ratio)
                int targetWidth, targetHeight;
                int x = 0, y = 0;

                if (width * 3 > height * 4)
                {
                    // รูปกว้างเกินไป ตัดซ้ายขวา
                    targetHeight = height;
                    targetWidth = height * 4 / 3;
                    x = (width - targetWidth) / 2;
                }
                else
                {
                    // รูปสูงเกินไป ตัดบนล่าง
                    targetWidth = width;
                    targetHeight = width * 3 / 4;
                    y = (height - targetHeight) / 2;
                }

                using var croppedBitmap = Bitmap.CreateBitmap(originalBitmap, x, y, targetWidth, targetHeight);
                
                // Scale ลงถ้าใหญ่เกิน 800x600
                Bitmap finalBitmap;
                if (targetWidth > 800 || targetHeight > 600)
                {
                    float scale = Math.Min(800f / targetWidth, 600f / targetHeight);
                    int newWidth = (int)(targetWidth * scale);
                    int newHeight = (int)(targetHeight * scale);
                    finalBitmap = Bitmap.CreateScaledBitmap(croppedBitmap, newWidth, newHeight, true);
                }
                else
                {
                    finalBitmap = croppedBitmap;
                }

                using var stream = new FileStream(outputPath, FileMode.Create);
                finalBitmap.Compress(Bitmap.CompressFormat.Jpeg, 90, stream);
                
                if (finalBitmap != croppedBitmap)
                    finalBitmap.Dispose();

                return outputPath;
            });
        }

        public static void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode == CropRequestCode && _tcs != null)
            {
                if (resultCode == Result.Ok && !string.IsNullOrEmpty(_outputPath))
                {
                    _tcs.SetResult(_outputPath);
                }
                else
                {
                    _tcs.SetResult(null!);
                }
                _tcs = null;
                _outputPath = null;
            }
        }
    }
}
