using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    public static class ConfiguratorExtensions
    {
        public static TConfigurator UseNewtonsoftJson<TConfigurator>(this TConfigurator configurator)
            where TConfigurator : IGremlinqConfigurator<TConfigurator>
        {
            return configurator
                .ConfigureQuerySource(source => source
                    .ConfigureEnvironment(environment => environment
                        .UseNewtonsoftJson()));
        }
    }
}
