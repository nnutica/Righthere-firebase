using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Microsoft.Maui.ApplicationModel;

namespace Firebasemauiapp.Services
{
    public class AuthRoutingService
    {
        private readonly FirebaseAuthClient _auth;
        private bool _started;

        public AuthRoutingService(FirebaseAuthClient auth)
        {
            _auth = auth;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            _auth.AuthStateChanged += OnAuthStateChanged;

            // Route once on startup based on current state
            _ = RouteForCurrentStateAsync();
        }

        private async void OnAuthStateChanged(object? sender, UserEventArgs e)
        {
            await RouteForCurrentStateAsync();
        }

        private async Task RouteForCurrentStateAsync()
        {
            try
            {
                // Ensure Shell is ready
                for (int i = 0; i < 40 && Shell.Current == null; i++)
                    await Task.Delay(50);

                var route = _auth.User != null ? "//main/starter" : "//signin";

                if (Shell.Current != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
                }
            }
            catch
            {
                // no-op
            }
        }
    }
}
