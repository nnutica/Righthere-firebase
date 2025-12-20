using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Firebasemauiapp.Services
{
    public static class AppLogger
    {
        private static readonly string LogDirectory = FileSystem.AppDataDirectory;
        private static readonly string LogFileName = $"AppLog_{DateTime.Now:yyyyMMdd}.txt";
        private static readonly string LogFilePath = Path.Combine(LogDirectory, LogFileName);

        public static async Task LogAsync(string message, Exception ex = null)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                if (ex != null)
                {
                    logEntry += $"\nException: {ex.GetType().FullName}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}";
                }
                logEntry += "\n";
                await File.AppendAllTextAsync(LogFilePath, logEntry);
            }
            catch
            {
                // If logging fails, do not throw further exceptions
            }
        }

        public static async Task LogUnhandledExceptionAsync(Exception ex)
        {
            await LogAsync("Unhandled Exception", ex);
        }
    }
}
