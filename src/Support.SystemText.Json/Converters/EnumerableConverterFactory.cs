using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using System.Collections;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class EnumerableConverterFactory : IConverterFactory
    {
        private abstract class EnumerableConverter<TTargetItem>
        {
            protected EnumerableConverter(IGremlinQueryEnvironment environment)
            {
                Environment = environment;
            }

            protected IEnumerable<TTargetItem> GetEnumerable(JsonElement source, ITransformer recurse)
            {
                foreach (JsonElement jsonElement in source.EnumerateArray())
                {
                    if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryExpandTraverser<TTargetItem>(Environment, recurse) is { } enumerable)
                    {
                        foreach (var item in enumerable)
                            yield return item;
                    }
                    else if (recurse.TryTransform<JsonElement, TTargetItem>(jsonElement, Environment, out var item))
                    {
                        yield return item;
                    }
                }
            }

            protected IGremlinQueryEnvironment Environment { get; }
        }

        private sealed class ArrayConverter<TTargetArray, TTargetItem> : EnumerableConverter<TTargetItem>, IConverter<JsonElement, TTargetArray>
        {
            public ArrayConverter(IGremlinQueryEnvironment environment) : base(environment)
            {
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTargetArray? value)
            {
                if (!Environment.SupportsType(typeof(TTargetArray)) && serialized.ValueKind == JsonValueKind.Array)
                {
                    value = (TTargetArray)(object)GetEnumerable(serialized, recurse).ToArray();
                    return true;
                }

                value = default;
                return false;
            }
        }

        private sealed class ListConverter<TTarget, TTargetItem> : EnumerableConverter<TTargetItem>, IConverter<JsonElement, TTarget>
        {
            public ListConverter(IGremlinQueryEnvironment environment) : base(environment)
            {
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                value = (TTarget)(object)GetEnumerable(serialized, recurse).ToList();
                return true;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            if (typeof(TSource) == typeof(JsonElement))
            {
                if (typeof(TTarget).IsAssignableFrom(typeof(object[])))
                    return (IConverter<TSource, TTarget>?)(object)new ArrayConverter<TTarget, object>(environment);

                if (typeof(TTarget).IsArray)
                    return (IConverter<TSource, TTarget>?)Activator.CreateInstance(typeof(ArrayConverter<,>).MakeGenericType(typeof(TTarget), typeof(TTarget).GetElementType()!), environment);

                if (typeof(TTarget).IsConstructedGenericType && typeof(IEnumerable).IsAssignableFrom(typeof(TTarget)))
                {
                    if (typeof(TTarget).GenericTypeArguments is [ var elementType ])
                    {
                        var listType = typeof(List<>).MakeGenericType(elementType);

                        if (typeof(TTarget).IsAssignableFrom(listType))
                            return (IConverter<TSource, TTarget>?)Activator.CreateInstance(typeof(ListConverter<,>).MakeGenericType(typeof(TTarget), typeof(TTarget).GenericTypeArguments[0]), environment);
                    }
                }
            }

            return default;
        }
    }
}
