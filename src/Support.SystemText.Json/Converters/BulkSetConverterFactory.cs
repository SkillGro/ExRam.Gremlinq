using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Support.SystemTextJson.Extensions;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class BulkSetConverterFactory : IConverterFactory
    {
        private sealed class BulkSetConverter<TTargetArray, TTargetArrayItem> : IConverter<JsonElement, TTargetArray>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public BulkSetConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTargetArray? value)
            {
                if (serialized.ValueKind == JsonValueKind.Object
                 && serialized.TryGetProperty("@type", out var typeToken)
                 && typeToken.ValueKind == JsonValueKind.String
                 && "g:BulkSet".Equals(typeToken.GetString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (serialized.TryGetProperty("@value", out var valueToken)
                     && valueToken.ValueKind == JsonValueKind.Array)
                    {
                        var setArray = valueToken;
                        var array = new List<TTargetArrayItem>();

                        foreach (var (item, count) in setArray.EnumerateArray().PairWise())
                        {
                            if (recurse.TryTransform<JsonElement, TTargetArrayItem>(item, _environment, out var element))
                            {
                                if (recurse.TryTransform<JsonElement, int>(count, _environment, out var bulk) && bulk != 1)
                                {
                                    for (var j = 0; j < bulk; j++)
                                    {
                                        array.Add(element);
                                    }
                                }
                                else
                                    array.Add(element);
                            }
                        }

                        value = (TTargetArray)(object)array.ToArray();
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            return typeof(TTarget).IsArray && !environment.SupportsType(typeof(TTarget)) && typeof(TSource) == typeof(JsonElement)
                ? (IConverter<TSource, TTarget>?)Activator.CreateInstance(typeof(BulkSetConverter<,>).MakeGenericType(typeof(TTarget), typeof(TTarget).GetElementType()!), environment)
                : default;
        }
    }
}
