using System.Xml;
using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class TimeSpanConverterFactory : FixedTypeConverterFactory<TimeSpan>
    {
        protected override TimeSpan? Convert(JsonElement jsonElement, IGremlinQueryEnvironment environment, ITransformer recurse)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => XmlConvert.ToTimeSpan(jsonElement.GetString()!),
                JsonValueKind.Number => TimeSpan.FromMilliseconds(jsonElement.GetDouble()),
                _ => default(TimeSpan?)
            };
        }
    }
}
