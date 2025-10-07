using System;

namespace Firebasemauiapp.Services
{
    // Simple service locator for scenarios where Shell or other framework constructs
    // make constructor injection difficult (use DI directly where possible).
    public static class ServiceHelper
    {
        private static IServiceProvider? _services;
        public static IServiceProvider Services => _services ?? throw new InvalidOperationException("ServiceHelper not initialized");

        public static void Initialize(IServiceProvider services)
        {
            _services = services;
        }

        public static T Get<T>() where T : notnull => (T)Services.GetService(typeof(T))!;
    }
}
