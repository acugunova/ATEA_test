using AutoMapper;
using AutoMapper.EquivalencyExpression;
using ATEA_test.API.DependencyInjection;

namespace ATEA_test.API.AutoMapperSetup
{
    public static class AutoMapperConfig
    {

        public static class MapperWrapper
        {
            public static IMapper Mapper => DIFactory.GetService<IMapper>();

        }
        public static void AddAutoMapper(this IServiceCollection services, Type[] assemblies)
        {
            var config = new MapperConfiguration( cfg =>
            {
                cfg.AddCollectionMappers();
                cfg.AllowNullCollections = true;
                cfg.AddMaps(assemblies);
            },
            loggerFactory: new LoggerFactory()
            );

            config.AssertConfigurationIsValid();

            services.AddSingleton<AutoMapper.IConfigurationProvider>(config);
            services.AddTransient<IMapper>(sp =>
                new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService));
        }
    }
}
