using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class DictionaryConverterFactory : IConverterFactory
    {
        private sealed class DictionaryConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public DictionaryConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (serialized.ValueKind != JsonValueKind.Object)
                {
                    value = default;
                    return false;
                }

                var ret = new Dictionary<string, object?>();

                foreach (var property in serialized.EnumerateObject())
                {
                    if (property.Value is { } propertyValue && recurse.TryTransform(propertyValue, _environment, out object? item))
                        ret.TryAdd(property.Name, item);
                }

                value = (TTarget)(object)ret;
                return true;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            return typeof(TSource) == typeof(JsonElement) && typeof(TTarget).IsAssignableFrom(typeof(Dictionary<string, object?>))
                ? (IConverter<TSource, TTarget>)(object)new DictionaryConverter<TTarget>(environment)
                : default;
        }
    }
}
