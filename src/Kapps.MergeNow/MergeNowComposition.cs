using System;

namespace MergeNow
{
    internal static class MergeNowComposition
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T Resolve<T>() where T : class
        {
            return _serviceProvider?.GetService(typeof(T)) as T;
        }
    }
}
