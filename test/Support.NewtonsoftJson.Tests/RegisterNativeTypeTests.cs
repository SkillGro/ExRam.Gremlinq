using ExRam.Gremlinq.Support.SystemTextJson;

using Newtonsoft.Json.Linq;

using static ExRam.Gremlinq.Core.GremlinQuerySource;

namespace ExRam.Gremlinq.Support.NewtonsoftJson.Tests
{
    public class RegisterNativeTypeTests : VerifyBase
    {
        private readonly struct FancyId
        {
            public FancyId(string wrappedId)
            {
                WrappedId = wrappedId;
            }

            public string WrappedId { get; }
        }

        public RegisterNativeTypeTests() : base()
        {
            VerifierSettings.DisableRequireUniquePrefix();
        }

        [Fact]
        public async Task Serialization()
        {
            await Verify(g
                        .ConfigureEnvironment(env => env
                            .UseNewtonsoftJson()
                            .RegisterNativeType(
                                (fancyId, env, _, recurse) => fancyId.WrappedId,
                                (jValue, env, _, recurse) => new FancyId(jValue.Value<string>()!)))
                        .Inject(new FancyId("fancyId"))
                        .Debug());
            await Verify(g
                        .ConfigureEnvironment(env => env
                            .UseSystemTextJson()
                            .RegisterNativeType(
                                (fancyId, env, _, recurse) => fancyId.WrappedId,
                                (jValue, env, _, recurse) => new FancyId(jValue.Value<string>()!)))
                        .Inject(new FancyId("fancyId"))
                        .Debug());
        }

        //TODO: DeserializationTests
    }
}
