﻿using System.Dynamic;
using System.Text.Json;

using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Models;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Support.SystemTextJson;
using ExRam.Gremlinq.Tests.Entities;

using FluentAssertions;
using Newtonsoft.Json.Linq;
using static ExRam.Gremlinq.Core.Transformation.ConverterFactory;

namespace ExRam.Gremlinq.Support.NewtonsoftJson.Tests
{
    public class TransformerTest : VerifyBase
    {
        private readonly IGremlinQueryEnvironment _environment;

        public TransformerTest() : base()
        {
            VerifierSettings.DisableRequireUniquePrefix();
            _environment = GremlinQueryEnvironment.Invalid
                .UseModel(GraphModel.FromBaseTypes<Vertex, Edge>());
        }

        [Fact]
        public async Task Irrelevant()
        {
            await Verify(Transformer.Empty
                .Add(Create<JObject, string>((serialized, env, _, recurse) => "should not be here"))
                .TryTransformTo<string>().From("serialized", _environment));
            await Verify(Transformer.Empty
                .Add(Create<JsonElement, string>((serialized, env, _, recurse) => "should not be here"))
                .TryTransformTo<string>().From("serialized", _environment));
        }

        [Fact]
        public async Task More_specific_type_is_deserialized()
        {
            await Verify(_environment
                .UseNewtonsoftJson()
                .Deserializer
                .TryTransformTo<object>().From(JObject.Parse("{ \"@type\": \"g:Date\", \"@value\": 1657527969000 }"), _environment));
            await Verify(_environment
                .UseSystemTextJson()
                .Deserializer
                .TryTransformTo<object>().From(JsonDocument.Parse("{ \"@type\": \"g:Date\", \"@value\": 1657527969000 }").RootElement, _environment));
        }

        [Fact]
        public async Task JObject_is_not_changed()
        {
            var original = JObject.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }");

            var deserialized = _environment
                .UseNewtonsoftJson()
                .Deserializer
                .TryTransformTo<JObject>().From(original, _environment);

            deserialized
                .Should()
                .BeSameAs(original);
        }

        [Fact]
        public async Task JsonElement_is_not_changed()
        {
            var original = JsonDocument.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }").RootElement;

            var deserialized = _environment
                .UseSystemTextJson()
                .Deserializer
                .TryTransformTo<JsonElement>().From(original, _environment);

            Assert.Equal(original, deserialized);
        }

        [Fact]
        public async Task Request_for_object_yields_DynamicObject()
        {
            {
                var original = JObject.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }");

                var deserialized = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TryTransformTo<object>().From(original, _environment);

                deserialized
                    .Should()
                    .BeAssignableTo<DynamicObject>();

                await Verify(deserialized);
            }
            {
                var original = JsonDocument.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }").RootElement;

                var deserialized = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TryTransformTo<object>().From(original, _environment);

                deserialized
                    .Should()
                    .BeAssignableTo<DynamicObject>();

                await Verify(deserialized);
            }
        }

        [Fact]
        public async Task Request_for_Dictionary_yields_Dictionary()
        {
            {
                var original = JObject.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }");

                var deserialized = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TryTransformTo<IDictionary<string, object>>().From(original, _environment);

                deserialized
                    .Should()
                    .BeOfType<Dictionary<string, object?>>();

                await Verify(deserialized);
            }
            {
                var original = JsonDocument.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }").RootElement;

                var deserialized = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TryTransformTo<IDictionary<string, object>>().From(original, _environment);

                deserialized
                    .Should()
                    .BeOfType<Dictionary<string, object?>>();

                await Verify(deserialized);
            }
        }

        [Fact]
        public async Task Request_for_Dictionary_from_typed_GraphSON_yields_Dictionary()
        {
            {
                var original = JObject.Parse("{ \"@type\": \"g:unknown\", \"@value\": { \"prop1\": \"value\", \"prop2\": 1657527969000 } }");

                var deserialized = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TryTransformTo<IDictionary<string, object>>().From(original, _environment);

                deserialized
                    .Should()
                    .BeOfType<Dictionary<string, object?>>();

                await Verify(deserialized);
            }
            {
                var original = JsonDocument.Parse("{ \"@type\": \"g:unknown\", \"@value\": { \"prop1\": \"value\", \"prop2\": 1657527969000 } }").RootElement;

                var deserialized = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TryTransformTo<IDictionary<string, object>>().From(original, _environment);

                deserialized
                    .Should()
                    .BeOfType<Dictionary<string, object?>>();

                await Verify(deserialized);
            }
        }

        [Fact]
        public async Task Request_for_object_from_map_yields_DynamicObject()
        {
            {
                var original = JObject.Parse("{ \"@type\": \"g:Map\", \"@value\": [ \"name\", \"Daniel Weber\", \"timestamp\", { \"@type\": \"g:Date\", \"@value\": 1689868807115 } ] }");

                var deserialized = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TryTransformTo<object>().From(original, _environment);

                deserialized
                    .Should()
                    .BeAssignableTo<DynamicObject>();

                await Verify(deserialized);
            }
            {
                var original = JsonDocument.Parse("{ \"@type\": \"g:Map\", \"@value\": [ \"name\", \"Daniel Weber\", \"timestamp\", { \"@type\": \"g:Date\", \"@value\": 1689868807115 } ] }").RootElement;

                var deserialized = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TryTransformTo<object>().From(original, _environment);

                deserialized
                    .Should()
                    .BeAssignableTo<DynamicObject>();

                await Verify(deserialized);
            }
        }

        [Fact]
        public async Task Dynamic_access()
        {
            {
                var original = JObject.Parse("{ \"@type\": \"g:Map\", \"@value\": [ \"name\", \"A name\", \"timestamp\", { \"@type\": \"g:Date\", \"@value\": 1689868807115 } ] }");

                var deserialized = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TryTransformTo<dynamic>().From(original, _environment);

                var name = deserialized!.name;
                var timestamp = deserialized!.timestamp;

                await Verify((name, timestamp));
            }
            {
                var original = JsonDocument.Parse("{ \"@type\": \"g:Map\", \"@value\": [ \"name\", \"A name\", \"timestamp\", { \"@type\": \"g:Date\", \"@value\": 1689868807115 } ] }").RootElement;

                var deserialized = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TryTransformTo<dynamic>().From(original, _environment);

                var name = deserialized!.name;
                var timestamp = deserialized!.timestamp;

                await Verify((name, timestamp));
            }
        }

        [Fact]
        public async Task Overridden_request_for_Dictionary_yields_dictionary()
        {
            {
                var original = JObject.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }");

                var deserialized = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .Add(Create<JObject, IDictionary<string, object?>>((static (jObject, env, _, recurse) =>
                    {
                        if (recurse.TryTransformTo<JObject>().From(jObject, env) is JObject processedFragment)
                        {
                            var dict = new Dictionary<string, object?>();

                            foreach (var property in processedFragment)
                            {
                                dict.TryAdd(property.Key, recurse.TryTransformTo<object>().From(property.Value, env));
                            }

                            return dict;
                        }

                        return default;
                    })))
                    .TryTransformTo<IDictionary<string, object>>().From(original, _environment);

                deserialized
                    .Should()
                    .BeOfType<Dictionary<string, object?>>();

                await Verify(deserialized);
            }
            {
                var original = JsonDocument.Parse("{ \"prop1\": \"value\", \"prop2\": 1657527969000 }").RootElement;

                var deserialized = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .Add(Create<JsonElement, IDictionary<string, object?>>((static (jObject, env, _, recurse) =>
                    {
                        if (recurse.TryTransformTo<JsonElement>().From(jObject, env) is { } processedFragment
                         && processedFragment.ValueKind == JsonValueKind.Object)
                        {
                            var dict = new Dictionary<string, object?>();

                            foreach (var property in processedFragment.EnumerateObject())
                            {
                                dict.TryAdd(property.Name, recurse.TryTransformTo<object>().From(property.Value, env));
                            }

                            return dict;
                        }

                        return default;
                    })))
                    .TryTransformTo<IDictionary<string, object>>().From(original, _environment);

                deserialized
                    .Should()
                    .BeOfType<Dictionary<string, object?>>();

                await Verify(deserialized);
            }
        }

        [Fact]
        public async Task Transform_to_List()
        {
            {
                var token = JObject.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}");

                await Verify(_environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TransformTo<List<object>>()
                    .From(token, _environment));
            }
            {
                var token = JsonDocument.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}").RootElement;

                await Verify(_environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TransformTo<List<object>>()
                    .From(token, _environment));
            }
        }

        [Fact]
        public async Task Transform_to_array()
        {
            {
                var token = JObject.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}");

                await Verify(_environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TransformTo<object[]>()
                    .From(token, _environment));
            }
            {
                var token = JsonDocument.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}").RootElement;

                await Verify(_environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TransformTo<object[]>()
                    .From(token, _environment));
            }
        }

        [Fact]
        public async Task Transform_to_IEnumerable()
        {
            {
                var token = JObject.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}");

                var result = _environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TransformTo<IEnumerable<object>>()
                    .From(token, _environment);

                await Verify(result);
            }
            {
                var token = JsonDocument.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}").RootElement;

                var result = _environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TransformTo<IEnumerable<object>>()
                    .From(token, _environment);

                await Verify(result);
            }
        }

        [Fact]
        public async Task Transform_from_JArray_to_object()
        {
            {
                var token = JObject.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}");

                await Verify(_environment
                    .UseNewtonsoftJson()
                    .Deserializer
                    .TransformTo<object>()
                    .From(token, _environment));
            }
            {
                var token = JsonDocument.Parse("{ \"@type\": \"g:List\", \"@value\": [ { \"@type\": \"g:Traverser\", \"@value\": { \"bulk\": { \"@type\": \"g:Int64\", \"@value\": 3 }, \"value\": { \"@type\": \"g:Map\", \"@value\": [ \"id\", { \"@type\": \"g:Int64\", \"@value\": 184 }, \"label\", \"Label\", \"properties\", { \"@type\": \"g:Map\", \"@value\": [] } ] } } } ]}").RootElement;

                await Verify(_environment
                    .UseSystemTextJson()
                    .Deserializer
                    .TransformTo<object>()
                    .From(token, _environment));
            }
        }
    }
}
