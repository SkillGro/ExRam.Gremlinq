using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.GraphElements;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class ExtractPropertyValueConverterFactory : IConverterFactory
    {
        private sealed class ExtractPropertyValueConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public ExtractPropertyValueConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (serialized.ValueKind == JsonValueKind.Object)
                {
                    if (!typeof(Property).IsAssignableFrom(typeof(TTarget)) && (serialized.LooksLikeProperty() || serialized.LooksLikeVertexProperty()) && serialized.TryGetProperty("value", out var valueToken))
                        return recurse.TryTransform(valueToken, _environment, out value);
                }
                else if (serialized.ValueKind == JsonValueKind.Array)
                {
                    var item = serialized.EnumerateArray().SingleOrDefault();
                    if (item.ValueKind != JsonValueKind.Undefined)
                        return recurse.TryTransform(item, _environment, out value);
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
         => typeof(TSource) == typeof(JsonElement)
            ? (IConverter<TSource, TTarget>)(object)new ExtractPropertyValueConverter<TTarget>(environment)
            : default;
    }
}
