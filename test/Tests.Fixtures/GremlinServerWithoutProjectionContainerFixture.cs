﻿using DotNet.Testcontainers.Containers;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Providers.Core;

namespace ExRam.Gremlinq.Tests.Fixtures
{
    public sealed class GremlinServerWithoutProjectionContainerFixture : TestContainerFixture
    {
        public GremlinServerWithoutProjectionContainerFixture() : base("tinkerpop/gremlin-server:3.7.0")
        {
        }

        protected override async Task<IGremlinQuerySource> TransformQuerySource(IContainer container, IConfigurableGremlinQuerySource g) => g
            .UseGremlinServer(_ => _
                .At(new UriBuilder("ws", container.Hostname, container.GetMappedPublicPort(8182)).Uri)
                .UseNewtonsoftJson())
            .ConfigureEnvironment(env => env
                .ConfigureOptions(options => options
                    .SetValue(GremlinqOption.VertexProjectionSteps, Traversal.Empty)
                    .SetValue(GremlinqOption.EdgeProjectionSteps, Traversal.Empty)
                    .SetValue(GremlinqOption.VertexPropertyProjectionSteps, Traversal.Empty)));
    }
}