namespace ATEA_test.API.DependencyInjection
{
    public static class DIFactory
    {
        private static IServiceProvider? serviceProvider;

        public static void Initialize(IServiceProvider provider)
        {
            serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public static TService? GetService<TService>() => serviceProvider.GetService<TService>();
    }
}