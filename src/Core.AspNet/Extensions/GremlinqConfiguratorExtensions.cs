﻿// ReSharper disable HeapView.PossibleBoxingAllocation
using ExRam.Gremlinq.Providers.Core;
using Gremlin.Net.Driver;
using System.Net.WebSockets;

using Microsoft.Extensions.Configuration;

namespace ExRam.Gremlinq.Core.AspNet
{
    internal static class GremlinqConfiguratorExtensions
    {
        private sealed class ConnectionPoolSettingsGremlinClientFactory : IGremlinClientFactory
        {
            private readonly IGremlinClientFactory _factory;
            private readonly IConfigurationSection _section;

            public ConnectionPoolSettingsGremlinClientFactory(IGremlinClientFactory factory, IConfigurationSection section)
            {
                _factory = factory;
                _section = section;
            }

            public IGremlinClient Create(IGremlinQueryEnvironment environment, GremlinServer gremlinServer, IMessageSerializer messageSerializer, ConnectionPoolSettings connectionPoolSettings, Action<ClientWebSocketOptions> webSocketConfiguration, string? sessionId = null)
            {
                if (int.TryParse(_section[$"{nameof(ConnectionPoolSettings.MaxInProcessPerConnection)}"], out var maxInProcessPerConnection))
                    connectionPoolSettings.MaxInProcessPerConnection = maxInProcessPerConnection;

                if (int.TryParse(_section[$"{nameof(ConnectionPoolSettings.PoolSize)}"], out var poolSize))
                    connectionPoolSettings.PoolSize = poolSize;

                return _factory.Create(environment, gremlinServer, messageSerializer, connectionPoolSettings, webSocketConfiguration, sessionId);
            }
        }

        public static TConfigurator ConfigureWebSocket<TConfigurator>(this TConfigurator configurator, IConfigurationSection section)
            where TConfigurator : IWebSocketProviderConfigurator<TConfigurator>
        {
            var connectionPoolSection = section.GetSection("ConnectionPool");

            if (section["Uri"] is { } uri)
                configurator = configurator.At(uri);

            return connectionPoolSection.Exists()
                ? configurator.ConfigureClientFactory(factory => new ConnectionPoolSettingsGremlinClientFactory(factory, connectionPoolSection))
                : configurator;
        }

        public static TConfigurator ConfigureBasicAuthentication<TConfigurator>(this TConfigurator configurator, IConfigurationSection section)
            where TConfigurator : IWebSocketProviderConfigurator<TConfigurator>
        {
            var authenticationSection = section.GetSection("Authentication");

            return authenticationSection["Username"] is { } username && authenticationSection["Password"] is { } password
                ? configurator.AuthenticateBy(username, password)
                : configurator;
        }
    }
}