using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class DateTimeOffsetConverterFactory : FixedTypeConverterFactory<DateTimeOffset>
    {
        protected override DateTimeOffset? Convert(JsonElement jsonElement, IGremlinQueryEnvironment environment, ITransformer recurse)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String when jsonElement.TryGetDateTimeOffset(out var dateTimeOffset) => dateTimeOffset,
                JsonValueKind.String when jsonElement.TryGetDateTime(out var dateTime) => new DateTimeOffset(dateTime),
                JsonValueKind.Number => DateTimeOffset.FromUnixTimeMilliseconds(jsonElement.GetInt64()),
                _ => default(DateTimeOffset?)
            };
        }
    }
}

