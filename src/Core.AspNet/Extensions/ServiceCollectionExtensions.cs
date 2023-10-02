﻿using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.AspNet;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using static ExRam.Gremlinq.Core.GremlinQuerySource;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private sealed class GremlinqServicesBuilder : IGremlinqServicesBuilder
        {
            private sealed class SourceTransformation : IGremlinQuerySourceTransformation
            {
                private readonly Func<IGremlinQuerySource, IGremlinQuerySource> _sourceTransformation;

                public SourceTransformation(Func<IGremlinQuerySource, IGremlinQuerySource> sourceTransformation)
                {
                    _sourceTransformation = sourceTransformation;
                }

                public IGremlinQuerySource Transform(IGremlinQuerySource source)
                {
                    return _sourceTransformation(source);
                }
            }

            public GremlinqServicesBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IGremlinqServicesBuilder ConfigureQuerySource(Func<IGremlinQuerySource, IGremlinQuerySource> sourceTranformation)
            {
                Services.AddSingleton<IGremlinQuerySourceTransformation>(new SourceTransformation(sourceTranformation));

                return this;
            }

            public IGremlinqServicesBuilder UseConfigurationSection(string sectionName)
            {
                Services
                    .AddSingleton(s => new GremlinqConfigurationSection(s.GetRequiredService<IConfiguration>(), sectionName))
                    .AddSingleton<IGremlinqConfigurationSection>(s => s.GetRequiredService<GremlinqConfigurationSection>())
                    .TryAddTransient<IEffectiveGremlinqConfigurationSection>(s => s.GetRequiredService<GremlinqConfigurationSection>());

                return this;
            }

            public IGremlinqServicesBuilder ConfigureQuerySource<TTransformation>()
                where TTransformation : class, IGremlinQuerySourceTransformation
            {
                Services.AddTransient<IGremlinQuerySourceTransformation, TTransformation>();

                return this;
            }

            public IServiceCollection Services { get; }
        }

        public static IServiceCollection AddGremlinq(this IServiceCollection serviceCollection, Action<IGremlinqServicesBuilder> configuration)
        {
            serviceCollection
                .TryAddSingleton<IGremlinqConfigurationSection>(s => new GremlinqConfigurationSection(s.GetRequiredService<IConfiguration>()));

            serviceCollection
                .TryAddSingleton(serviceProvider =>
                {
                    var querySource = g
                        .ConfigureEnvironment(environment =>
                        {
                            if (serviceProvider.GetService<ILogger<GremlinqQueries>>() is { } logger)
                            {
                                environment = environment
                                    .UseLogger(logger);
                            }

                            if (serviceProvider.GetService<IGremlinqConfigurationSection>() is { } gremlinConfigSection)
                            {
                                environment = environment
                                    .ConfigureOptions(options =>
                                    {
                                        var loggingSection = gremlinConfigSection
                                            .GetSection("QueryLogging");

                                        if (Enum.TryParse<QueryLogVerbosity>(loggingSection["Verbosity"], out var verbosity))
                                            options = options.SetValue(GremlinqOption.QueryLogVerbosity, verbosity);

                                        if (Enum.TryParse<LogLevel>(loggingSection[$"{nameof(LogLevel)}"], out var logLevel))
                                            options = options.SetValue(GremlinqOption.QueryLogLogLevel, logLevel);

                                        if (Enum.TryParse<QueryLogFormatting>(loggingSection["Formatting"], out var formatting))
                                            options = options.SetValue(GremlinqOption.QueryLogFormatting, formatting);

                                        return options;
                                    });
                            }

                            return environment;
                        });

                    if (serviceProvider.GetService<IEnumerable<IGremlinQuerySourceTransformation>>() is { } transformations)
                    {
                        foreach (var transformation in transformations)
                        {
                            querySource = transformation.Transform(querySource);
                        }
                    }

                    return querySource;
                });

            configuration(new GremlinqServicesBuilder(serviceCollection));

            return serviceCollection;
        }
    }
}
