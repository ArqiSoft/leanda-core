using Nest;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;
using Serilog;
using Serilog.Events;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.EndToEndTests
{
    [CollectionDefinition("OSDR Test Harness")]
    public class OsdrTestCollection : ICollectionFixture<OsdrTestHarness>
    {
    }

    public class OsdrWebTest : OsdrTest
    {
        public OsdrWebTest(OsdrTestHarness fixture, ITestOutputHelper output = null) : base(fixture)
        {
            if (output != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo
                    .TestOutput(output, LogEventLevel.Verbose)
                    .CreateLogger()
                    .ForContext<OsdrTest>();
            }
        }
        public OsdrWebClient JohnApi => WebFixture.JohnApi;
        public OsdrWebClient JaneApi => WebFixture.JaneApi;
        public OsdrWebClient UnauthorizedApi => WebFixture.UnauthorizedApi;
        public IElasticClient ElasticClient => WebFixture.ElasticClient;
        protected OsdrTestHarness WebFixture => Harness as OsdrTestHarness;
    }
}