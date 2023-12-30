using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.NewtonsoftJson
{
    internal sealed class NativeTypeConverterFactory : IConverterFactory
    {
        public sealed class NativeTypeConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public NativeTypeConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (serialized.ValueKind == JsonValueKind.Object
                 || serialized.ValueKind == JsonValueKind.Array)
                {
                    value = default;
                    return false;
                }

                try
                {
                    value = serialized.Deserialize<TTarget>();
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
                    return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
                }
                catch (JsonException)
                {
                    value = default;
                    return false;
                }
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            return typeof(TSource) == typeof(JsonElement)
                ? (IConverter<TSource, TTarget>)(object)new NativeTypeConverter<TTarget>(environment)
                : default;
        }
    }
}
