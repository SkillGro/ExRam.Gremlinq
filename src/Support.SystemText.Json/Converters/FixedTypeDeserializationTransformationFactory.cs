﻿using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Transformation;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal abstract class FixedTypeConverterFactory<TStaticTarget> : IConverterFactory
        where TStaticTarget : struct
    {
        private sealed class FixedTypeConverter : IConverter<JsonElement, TStaticTarget>
        {
            private readonly IGremlinQueryEnvironment _environment;
            private readonly FixedTypeConverterFactory<TStaticTarget> _factory;

            public FixedTypeConverter(FixedTypeConverterFactory<TStaticTarget> factory, IGremlinQueryEnvironment environment)
            {
                _factory = factory;
                _environment = environment;
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, out TStaticTarget value)
            {
                if (_factory.Convert(serialized, _environment, recurse) is { } requested)
                {
                    value = requested;

                    return true;
                }

                value = default;

                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment)
        {
            return typeof(TTarget) == typeof(TStaticTarget) && typeof(TSource) == typeof(JsonElement)
                ? (IConverter<TSource, TTarget>)(object)new FixedTypeConverter(this, environment)
                : null;
        }

        protected abstract TStaticTarget? Convert(JsonElement jValue, IGremlinQueryEnvironment environment, ITransformer recurse);
    }
}
