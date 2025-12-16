using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Firebasemauiapp.Platforms.Android
{
    public static class EditorHandler
    {
        public static void RemoveUnderline()
        {
            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
                handler.PlatformView.BackgroundTintList =
                    global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Transparent);
            });
        }
    }
}
