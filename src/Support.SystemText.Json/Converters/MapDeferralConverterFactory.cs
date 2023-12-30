using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Support.SystemTextJson.Extensions;

namespace ExRam.Gremlinq.Support.SystemTextJson
{

    internal sealed class MapDeferralConverterFactory : IConverterFactory
    {

        private sealed class MapDeferralConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public MapDeferralConverter(IGremlinQueryEnvironment environment)
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
                        var jsonObject = new System.Text.Json.Nodes.JsonObject();

                        foreach (var (propertyKey, propertyValue) in mapArray.EnumerateArray().PairWise())
                        {
                            if (propertyKey.ValueKind == JsonValueKind.String)
                                jsonObject.Add(propertyKey.GetString()!, System.Text.Json.Nodes.JsonNode.Parse(propertyValue.GetRawText()));
                        }

                        // TODO: Add converter for JsonObject instead
                        var jsonString = jsonObject.ToString();
                        using var jsonDocument = JsonDocument.Parse(jsonString);
                        var jsonElement = jsonDocument.RootElement;

                        return recurse.TryTransform(jsonElement, _environment, out value);
                    }
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            return typeof(TSource) == typeof(JsonElement)
                ? (IConverter<TSource, TTarget>)(object)new MapDeferralConverter<TTarget>(environment)
                : default;
        }
    }
}
