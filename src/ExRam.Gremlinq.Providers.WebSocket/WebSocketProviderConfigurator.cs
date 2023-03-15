﻿using System.Collections.Concurrent;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Execution;
using ExRam.Gremlinq.Core.Serialization;
using ExRam.Gremlinq.Core.Transformation;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Messages;
using Microsoft.Extensions.Logging;

namespace ExRam.Gremlinq.Providers.WebSocket
{
    public sealed class WebSocketProviderConfigurator : IWebSocketProviderConfigurator<WebSocketProviderConfigurator>
    {
        private sealed class WebSocketGremlinQueryExecutor : IGremlinQueryExecutor, IDisposable
        {
            private readonly GremlinServer _gremlinServer;
            private readonly IGremlinClientFactory _clientFactory;
            private readonly Dictionary<string, string> _aliasArgs;
            private readonly ConcurrentDictionary<IGremlinQueryEnvironment, IGremlinClient> _clients = new();

            public WebSocketGremlinQueryExecutor(
                GremlinServer gremlinServer,
                IGremlinClientFactory clientFactory,
                string alias = "g")
            {
                _gremlinServer = gremlinServer;
                _clientFactory = clientFactory;
                _aliasArgs = new Dictionary<string, string> { { "g", alias } };
            }

            public IAsyncEnumerable<object> Execute(ISerializedGremlinQuery serializedQuery, IGremlinQueryEnvironment environment)
            {
                return AsyncEnumerable.Create(Core);

                async IAsyncEnumerator<object> Core(CancellationToken ct)
                {
                    var client = _clients
                        .GetOrAdd(
                            environment,
                            static (environment, @this) => @this._clientFactory.Create(
                                @this._gremlinServer,
                                new DefaultMessageSerializer(environment),
                                new ConnectionPoolSettings(),
                                static _ => { }),
                            this);

                    if (!Guid.TryParse(serializedQuery.Id, out var requestId))
                    {
                        requestId = Guid.NewGuid();
                        environment.Logger.LogInformation($"Mapping query id {serializedQuery.Id} to request id {requestId}.");
                    }

                    var requestMessage = serializedQuery switch
                    {
                        GroovyGremlinQuery groovyScript => RequestMessage
                            .Build(Tokens.OpsEval)
                            .AddArgument(Tokens.ArgsGremlin, groovyScript.Script)
                            .AddArgument(Tokens.ArgsAliases, _aliasArgs)
                            .AddArgument(Tokens.ArgsBindings, groovyScript.Bindings)
                            .OverrideRequestId(requestId)
                            .Create(),
                        BytecodeGremlinQuery bytecodeQuery => RequestMessage
                            .Build(Tokens.OpsBytecode)
                            .Processor(Tokens.ProcessorTraversal)
                            .AddArgument(Tokens.ArgsGremlin, bytecodeQuery.Bytecode)
                            .AddArgument(Tokens.ArgsAliases, _aliasArgs)
                            .OverrideRequestId(requestId)
                            .Create(),
                        _ => throw new ArgumentException($"Cannot handle serialized query of type {serializedQuery.GetType()}.")
                    };

                    var maybeResults = await client
                        .SubmitAsync<object>(requestMessage)
                        .ConfigureAwait(false);

                    if (maybeResults is { } results)
                        foreach (var obj in results)
                            yield return obj;
                }
            }

            public void Dispose()
            {
                //TODO: Dispose clients ?
            }
        }

        private readonly string _alias;
        private readonly GremlinServer _gremlinServer;
        private readonly IGremlinClientFactory _clientFactory;
        private readonly GremlinqConfigurator _gremlinqConfigurator;

        public WebSocketProviderConfigurator() : this(
            new GremlinqConfigurator(),
            new GremlinServer(),
            GremlinClientFactory.Default,
            "g")
        {
        }

        public WebSocketProviderConfigurator(
            GremlinqConfigurator gremlinqConfigurator,
            GremlinServer gremlinServer,
            IGremlinClientFactory clientFactory,
            string alias)
        {
            _alias = alias;
            _gremlinServer = gremlinServer;
            _clientFactory = clientFactory;
            _gremlinqConfigurator = gremlinqConfigurator;
        }

        public WebSocketProviderConfigurator ConfigureServer(Func<GremlinServer, GremlinServer> transformation) => new (
            _gremlinqConfigurator,
            transformation(_gremlinServer),
            _clientFactory,
            _alias);

        public WebSocketProviderConfigurator ConfigureClientFactory(Func<IGremlinClientFactory, IGremlinClientFactory> transformation) => new (
            _gremlinqConfigurator,
            _gremlinServer,
            transformation(_clientFactory),
            _alias);

        public WebSocketProviderConfigurator ConfigureAlias(Func<string, string> transformation) => new (
            _gremlinqConfigurator,
            _gremlinServer,
            _clientFactory,
            transformation(_alias));

        public WebSocketProviderConfigurator ConfigureDeserialization(Func<ITransformer, ITransformer> deserializerTransformation) => new(
            _gremlinqConfigurator.ConfigureDeserialization(deserializerTransformation),
            _gremlinServer,
            _clientFactory,
            _alias);

        public IGremlinQuerySource Transform(IGremlinQuerySource source) => _gremlinqConfigurator
            .Transform(source)
            .ConfigureEnvironment(environment => environment
                .UseExecutor(Build().Log()));

        private IGremlinQueryExecutor Build() => !"ws".Equals(_gremlinServer.Uri.Scheme, StringComparison.OrdinalIgnoreCase) && !"wss".Equals(_gremlinServer.Uri.Scheme, StringComparison.OrdinalIgnoreCase)
            ? throw new ArgumentException("Expected the Uri-Scheme to be either \"ws\" or \"wss\".")
            : new WebSocketGremlinQueryExecutor(
                _gremlinServer,
                _clientFactory,
                _alias);
    }
}