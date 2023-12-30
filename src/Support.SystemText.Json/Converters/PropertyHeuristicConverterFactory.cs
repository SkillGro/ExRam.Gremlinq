using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.GraphElements;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class PropertyHeuristicConverterFactory : IConverterFactory
    {
        private sealed class PropertyHeuristicConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public PropertyHeuristicConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement jObject, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (jObject.ValueKind == JsonValueKind.Object)
                {
                    if (jObject.LooksLikeVertexProperty())
                    {
                        if (recurse.TryTransform(jObject, _environment, out VertexProperty<object>? vProp) && vProp is TTarget target)
                        {
                            value = target;
                            return true;
                        }
                    }
                    else if (jObject.LooksLikeProperty())
                    {
                        if (recurse.TryTransform(jObject, _environment, out Property<object>? prop) && prop is TTarget target)
                        {
                            value = target;
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment) => typeof(JsonElement) == typeof(TSource) && typeof(TTarget).IsAssignableFrom(typeof(VertexProperty<object>)) && !typeof(Property<object>).IsAssignableFrom(typeof(TTarget))
            ? (IConverter<TSource, TTarget>)(object)new PropertyHeuristicConverter<TTarget>(environment)
            : default;
    }
}
