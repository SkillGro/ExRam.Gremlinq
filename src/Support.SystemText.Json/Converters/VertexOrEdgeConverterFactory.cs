using Newtonsoft.Json.Linq;
using ExRam.Gremlinq.Core.GraphElements;
using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class VertexOrEdgeConverterFactory : IConverterFactory
    {
        private sealed class VertexOrEdgeConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public VertexOrEdgeConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement jObject, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (jObject.LooksLikeElement(out var idToken, out var label, out var maybePropertiesObject))
                {
                    if (recurse.TryTransform(maybePropertiesObject ?? default, _environment, out value))
                    {
                        value.SetIdAndLabel(idToken, label, _environment, recurse);
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment) => (typeof(TSource) == typeof(JObject) && !typeof(TTarget).IsAssignableFrom(typeof(TSource)) && !typeof(TTarget).IsArray && typeof(TTarget) != typeof(object) && !typeof(TTarget).IsInterface && !typeof(Property).IsAssignableFrom(typeof(TTarget)))
            ? (IConverter<TSource, TTarget>)(object)new VertexOrEdgeConverter<TTarget>(environment)
            : default;
    }
}
