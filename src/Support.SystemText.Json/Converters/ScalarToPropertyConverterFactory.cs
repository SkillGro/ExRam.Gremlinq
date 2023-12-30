﻿using Newtonsoft.Json.Linq;
using ExRam.Gremlinq.Core.GraphElements;
using System.Diagnostics.CodeAnalysis;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;
using System.Linq.Expressions;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class ScalarToPropertyConverterFactory : IConverterFactory
    {
        private sealed class ScalarToPropertyConverter<TTargetProperty, TTargetPropertyValue> : IConverter<JsonElement, TTargetProperty>
            where TTargetProperty : Property
        {
            private readonly IGremlinQueryEnvironment _environment;
            private readonly Func<TTargetPropertyValue, TTargetProperty?> _constructor;

            public ScalarToPropertyConverter(IGremlinQueryEnvironment environment)
            {
                if (typeof(TTargetProperty).GetConstructor(new[] { typeof(TTargetPropertyValue) }) is { } constructor)
                {
                    var lambdaParam = Expression.Parameter(typeof(TTargetPropertyValue));

                    _environment = environment;
                    _constructor = Expression
                        .Lambda<Func<TTargetPropertyValue, TTargetProperty?>>(Expression.New(constructor, lambdaParam), lambdaParam)
                        .Compile();
                }
                else
                    throw new ArgumentException($"{typeof(TTargetProperty).Name} does not contain a constructor that takes a value of type {typeof(TTargetPropertyValue).Name}.");
            }

            public bool TryConvert(JsonElement serialized, ITransformer defer, ITransformer recurse, [NotNullWhen(true)] out TTargetProperty? value)
            {
                if (recurse.TryTransform<JsonElement, TTargetPropertyValue>(serialized, _environment, out var propertyValue))
                {
                    if (_constructor(propertyValue) is { } requestedProperty)
                    {
                        value = requestedProperty;
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }

        public IConverter<TSource, TTarget>? TryCreate<TSource, TTarget>(IGremlinQueryEnvironment environment) => typeof(TSource) == typeof(JsonElement) && typeof(Property).IsAssignableFrom(typeof(TTarget)) && typeof(TTarget).IsGenericType
            ? (IConverter<TSource, TTarget>?)Activator.CreateInstance(typeof(ScalarToPropertyConverter<,>).MakeGenericType(typeof(TTarget), typeof(TTarget).GetGenericArguments()[0]), environment)
            : default;
    }
}
