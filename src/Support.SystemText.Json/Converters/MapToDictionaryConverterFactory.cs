using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Support.SystemTextJson.Extensions;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class MapToDictionaryConverterFactory : IConverterFactory
    {
        private sealed class MapToDictionaryConverter<TKey, TValue, TTarget> : IConverter<JsonElement, TTarget>
            where TKey : notnull
        {
            private readonly IGremlinQueryEnvironment _environment;

            public MapToDictionaryConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (serialized.ValueKind == JsonValueKind.Object
                 && serialized.TryGetProperty("@type", out var nestedType)
                 && nestedType.ValueKind == JsonValueKind.String
                 && "g:Map".Equals(nestedType.GetString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (serialized.TryGetProperty("@value", out var valueToken)
                     && valueToken.ValueKind == JsonValueKind.Array)
                    {
                        var mapArray = valueToken;
                        var retObject = new Dictionary<TKey, TValue>();

                        foreach (var (propertyKey, propertyValue) in mapArray.EnumerateArray().PairWise())
                        {
                            if (recurse.TryTransform(propertyKey, _environment, out TKey? key)
                             && recurse.TryTransform(propertyValue, _environment, out TValue? entry))
                                retObject.Add(key, entry);
                        }

                        value = (TTarget)(object)retObject;
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            if (typeof(TSource) == typeof(JsonElement))
            {
                var maybeCompatibleInterface = typeof(TTarget)
                    .GetInterfaces().Prepend(typeof(TTarget))
                    .Select(static iface => iface is { IsGenericType: true, GenericTypeArguments: [var keyType, var valueType] } && typeof(TTarget).IsAssignableFrom(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))
                        ? (keyType, valueType)
                        : default((Type keyType, Type valueType)?))
                    .FirstOrDefault(static x => x != null);

                if (maybeCompatibleInterface is ({ } keyType, { } valueType))
                    return (IConverter<TSource, TTarget>?)Activator.CreateInstance(typeof(MapToDictionaryConverter<,,>).MakeGenericType(keyType, valueType, typeof(TTarget)), environment);
            }

            return default;
        }
    }
}
