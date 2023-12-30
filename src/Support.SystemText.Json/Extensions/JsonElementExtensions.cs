using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Transformation;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal static class JsonElementExtensions
    {
        public static IEnumerable<TItem>? TryExpandTraverser<TItem>(this JsonElement jObject, IGremlinQueryEnvironment env, ITransformer recurse)
        {
            if (jObject.TryGetProperty("@type", out var nestedType)
                && "g:Traverser".Equals(nestedType.GetString(), StringComparison.OrdinalIgnoreCase)
                && jObject.TryGetProperty("@value", out var valueToken)
                && valueToken.ValueKind == JsonValueKind.Object)
            {
                var nestedTraverserObject = valueToken;
                var bulk = 1;

                if (nestedTraverserObject.TryGetProperty("bulk", out var bulkToken)
                    && recurse.TryTransform<JsonElement, int>(bulkToken, env, out var bulkObject))
                    bulk = bulkObject;

                if (nestedTraverserObject.TryGetProperty("value", out var traverserValue))
                {
                    return Core();

                    IEnumerable<TItem> Core()
                    {
                        if (recurse.TryTransform<JsonElement, TItem>(traverserValue, env, out var item))
                        {
                            for (var j = 0; j < bulk; j++)
                                yield return item;
                        }
                    }
                }
            }

            return null;
        }

        public static bool LooksLikeElement(this JsonElement jObject, [NotNullWhen(true)] out JsonElement idToken, [NotNullWhen(true)] out JsonElement labelValue, out JsonElement? propertiesObject)
        {
            idToken = default;
            labelValue = default;
            propertiesObject = null;

            if (!jObject.TryGetProperty("value", out _)
                && jObject.TryGetProperty("id", out idToken)
                && idToken.ValueKind != JsonValueKind.Array
                && jObject.TryGetProperty("label", out var labelToken)
                && labelToken.ValueKind == JsonValueKind.String)
            {
                if (labelToken.ValueKind != JsonValueKind.Object
                 && labelToken.ValueKind != JsonValueKind.Array)
                {
                    labelValue = labelToken;
                    if (jObject.TryGetProperty("properties", out var propertiesToken))
                    {
                        if (propertiesToken.ValueKind != JsonValueKind.Object)
                            return false;

                        propertiesObject = propertiesToken;
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool LooksLikeProperty(this JsonElement jObject)
        {
            return jObject.TryGetProperty("value", out _)
                && jObject.TryGetProperty("key", out var keyToken)
                && keyToken.ValueKind == JsonValueKind.String;
        }

        public static bool LooksLikeVertexProperty(this JsonElement jObject)
        {
            if (jObject.TryGetProperty("value", out _)
                && jObject.TryGetProperty("id", out var idToken)
                && idToken.ValueKind != JsonValueKind.Array)
            {
                if (!jObject.TryGetProperty("label", out var labelToken)
                    || labelToken.ValueKind == JsonValueKind.String)
                {
                    if (!jObject.TryGetProperty("properties", out var propertiesToken)
                        || propertiesToken.ValueKind == JsonValueKind.Object)
                        return true;
                }
            }

            return false;
        }
    }
}
