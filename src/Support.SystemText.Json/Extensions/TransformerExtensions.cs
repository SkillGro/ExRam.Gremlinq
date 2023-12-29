﻿using System.Collections.Concurrent;
using System.Reflection;

using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Transformation;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal static class TransformerExtensions
    {
        public readonly struct FluentForType
        {
            private readonly Type _type;
            private readonly ITransformer _deserializer;

            private static readonly ConcurrentDictionary<(Type, Type), Delegate?> FromClassDelegates = new();
            private static readonly ConcurrentDictionary<(Type, Type), Delegate?> FromStructDelegates = new();

            public FluentForType(ITransformer deserializer, Type type)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    type = type.GetGenericArguments()[0];

                _type = type;
                _deserializer = deserializer;
            }

            public object? From<TSource>(TSource source, IGremlinQueryEnvironment environment)
            {
                return TryGetDelegate(typeof(TSource), _type) is Func<ITransformer, TSource, IGremlinQueryEnvironment, object?> fromDelegate
                    ? fromDelegate(_deserializer, source, environment)
                    : default;
            }

            private static Delegate? TryGetDelegate(Type sourceType, Type targetType)
            {
                var delegatesDict = targetType.IsValueType
                    ? FromStructDelegates
                    : FromClassDelegates;

                return delegatesDict
                    .GetOrAdd(
                        (sourceType, targetType),
                        static tuple =>
                        {
                            var (sourceType, targetType) = tuple;

                            var methodName = targetType.IsValueType
                                ? nameof(FromStruct)
                                : nameof(FromClass);

                            return typeof(FluentForType)
                                .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
                                .MakeGenericMethod(sourceType, targetType)
                                .Invoke(null, Array.Empty<object>()) as Delegate;
                        });
            }

            private static Func<ITransformer, TSource, IGremlinQueryEnvironment, object?> FromClass<TSource, TTarget>()
                where TTarget : class => (deserializer, serialized, environment) => deserializer.TryTransform<TSource, TTarget>(serialized, environment, out var value)
                    ? value
                    : default;

            private static Func<ITransformer, TSource, IGremlinQueryEnvironment, object?> FromStruct<TSource, TTarget>()
                where TTarget : struct => (deserializer, serialized, environment) => deserializer.TryTransform<TSource, TTarget>(serialized, environment, out var value)
                    ? value
                    : default(TTarget?);
        }

        public static ITransformer UseNewtonsoftJson(this ITransformer transformer)
        {
            return transformer
                .Add(new DeferToNewtonsoftConverterFactory())
                .Add(new NewtonsoftJsonSerializerConverterFactory())
                .Add(new VertexPropertyPropertiesConverterFactory())
                .Add(new DictionaryConverterFactory())
                .Add(new DynamicObjectConverterFactory())

                .Add(new ExtractPropertyValueConverterFactory())
                .Add(new ScalarToPropertyConverterFactory())
                .Add(new PropertyHeuristicConverterFactory())

                .Add(new VertexOrEdgeConverterFactory())
                .Add(new LabelLookupConverterFactory())

                .Add(new TypedValueConverterFactory())
                .Add(new MapDeferralConverterFactory())
                .Add(new MapToDictionaryConverterFactory())
                .Add(new BulkSetConverterFactory())
                .Add(new EnumerableConverterFactory())

                .Add(new NativeTypeConverterFactory())
                .Add(new NullableConverterFactory())

                .Add(new TimeSpanConverterFactory())
                .Add(new DateTimeOffsetConverterFactory())
                .Add(new DateTimeConverterFactory());
        }

        public static FluentForType TryTransformTo(this ITransformer deserializer, Type type) => new(deserializer, type);
    }
}
