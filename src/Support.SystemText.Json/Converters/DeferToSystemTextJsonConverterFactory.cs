using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Providers.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class DeferToSystemTextJsonConverterFactory : IConverterFactory
    {
        private static class DeferToSystemTextJsonConverter<TBinaryMessage>
            where TBinaryMessage : IMemoryOwner<byte>
        {
            private static readonly ThreadLocal<(TBinaryMessage, JsonDocument)?> LastSerialization = new();

            public sealed class DeferToNewtonsoftConverterImpl<TTarget> : IConverter<TBinaryMessage, TTarget>
            {
                private readonly IGremlinQueryEnvironment _environment;

                public DeferToNewtonsoftConverterImpl(IGremlinQueryEnvironment environment)
                {
                    _environment = environment;
                }

                public bool TryConvert(TBinaryMessage source, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
                {
                    value = default;

                    return TryGetJsonDocument(source) is { } document && recurse.TryTransform(document.RootElement, _environment, out value);
                }

                private static JsonDocument TryGetJsonDocument(TBinaryMessage source)
                {
                    if (LastSerialization.Value is { Item1: { } lastMessage, Item2: { } lastToken } && EqualityComparer<TBinaryMessage>.Default.Equals(lastMessage, source))
                        return lastToken;

                    var stream = MemoryMarshal.TryGetArray<byte>(source.Memory, out var segment) && segment is { Array: { } array }
                        ? new MemoryStream(array, segment.Offset, segment.Count)
                        : new MemoryStream(source.Memory.ToArray());

                    return JsonDocument.Parse(stream);
                }
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment) => typeof(TSource) == typeof(GraphSon2BinaryMessage)
            ? (IConverter<TSource, TTarget>)(object)new DeferToSystemTextJsonConverter<GraphSon2BinaryMessage>.DeferToNewtonsoftConverterImpl<TTarget>(environment)
            : typeof(TSource) == typeof(GraphSon3BinaryMessage)
                ? (IConverter<TSource, TTarget>)(object)new DeferToSystemTextJsonConverter<GraphSon3BinaryMessage>.DeferToNewtonsoftConverterImpl<TTarget>(environment)
                : default;
    }
}
