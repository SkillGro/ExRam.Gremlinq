﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using Gremlin.Net.Process.Traversal;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class TypedValueConverterFactory : IConverterFactory
    {
        private static readonly Dictionary<string, Type> GraphSONTypes = new()
        {
            { "g:Int32", typeof(int) },
            { "g:Int64", typeof(long) },
            { "g:Float", typeof(float) },
            { "g:Double", typeof(double) },
            { "g:Direction", typeof(Direction) },
            { "g:Merge", typeof(Merge) },
            { "g:UUID", typeof(Guid) },
            { "g:Date", typeof(DateTimeOffset) },
            { "g:Timestamp", typeof(DateTimeOffset) },
            { "g:T", typeof(T) },

            //Extended
            { "gx:BigDecimal", typeof(decimal) },
            { "gx:Duration", typeof(TimeSpan) },
            { "gx:BigInteger", typeof(BigInteger) },
            { "gx:Byte",typeof(byte) },
            { "gx:ByteBuffer", typeof(byte[]) },
            { "gx:Char", typeof(char) },
            { "gx:Int16", typeof(short) }
        };

        public sealed class TypedValueConverter<TTarget> : IConverter<JsonElement, TTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;

            public TypedValueConverter(IGremlinQueryEnvironment environment)
            {
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTarget? value)
            {
                if (serialized.ValueKind == JsonValueKind.Object
                 && serialized.TryGetProperty("@type", out var typeName)
                 && serialized.TryGetProperty("@value", out var valueToken))
                {
                    if (typeName.ValueKind == JsonValueKind.String
                     && typeName.GetString() is { } typeNameString
                     && GraphSONTypes.TryGetValue(typeNameString, out var moreSpecificType))
                    {
                        if (typeof(TTarget) != moreSpecificType && typeof(TTarget).IsAssignableFrom(moreSpecificType))
                        {
                            if (recurse.TryTransformTo(moreSpecificType).From(valueToken, _environment) is TTarget target)
                            {
                                value = target;
                                return true;
                            }
                        }
                    }

                    return recurse.TryTransform(valueToken, _environment, out value);
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            return typeof(TSource) == typeof(JsonElement)
                ? (IConverter<TSource, TTarget>)(object)new TypedValueConverter<TTarget>(environment)
                : default;
        }
    }
}
