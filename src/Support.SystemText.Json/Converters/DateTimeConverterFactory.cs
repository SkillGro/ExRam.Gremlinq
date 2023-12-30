using ExRam.Gremlinq.Core.Transformation;
using ExRam.Gremlinq.Core;

namespace ExRam.Gremlinq.Support.SystemTextJson
{
    internal sealed class DateTimeConverterFactory : FixedTypeConverterFactory<DateTime>
    {
        protected override DateTime? Convert(JsonElement jsonElement, IGremlinQueryEnvironment environment, ITransformer recurse)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String when jsonElement.TryGetDateTimeOffset(out var dateTimeOffset) => dateTimeOffset.UtcDateTime,
                JsonValueKind.String when jsonElement.TryGetDateTime(out var dateTime) => dateTime,
                JsonValueKind.Number when jsonElement.TryGetInt64(out var unixTimeMilliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds).UtcDateTime,
                _ => default(DateTime?)
            };
        }
    }
}
