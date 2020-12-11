using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class CreateAndGetCategoryTreeFixture
    {
        public Guid CategoryId;

        public CreateAndGetCategoryTreeFixture(OsdrWebTestHarness harness)
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode("Projects", new List<TreeNode>()
                {
                    new TreeNode("Projects One"),
                    new TreeNode("Projects Two")
                })
            };

            var response = harness.JohnApi.PostData("/api/categorytrees/tree", categories).Result;

            var content = response.Content.ReadAsStringAsync().Result;

            CategoryId = Guid.Parse(content);

            harness.WaitWhileCategoryTreePersisted(CategoryId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreateAndGetCategoryTree : OsdrWebTest, IClassFixture<CreateAndGetCategoryTreeFixture>
    {
        private Guid CategoryId;

        public CreateAndGetCategoryTree(OsdrWebTestHarness harness, ITestOutputHelper output, CreateAndGetCategoryTreeFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_CreateNewCategoryTree_BuiltExpectedDocument()
        {
            var response = await JohnApi.GetData($"/api/categorytrees/tree/{CategoryId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonCategory = JToken.Parse(await response.Content.ReadAsStringAsync());
             
            jsonCategory.Should().ContainsJson($@"
            {{
            	'id': '{CategoryId}',
            	'createdBy': '{JohnId}',
            	'createdDateTime': *EXIST*,
            	'updatedBy': '{JohnId}',
            	'updatedDateTime': *EXIST*,
            	'version': 1,
                'nodes': *EXIST*
            }}");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_GetNonExistantCategoryTree_ReturnsNotFoundCode()
        {
            var response = await JohnApi.GetData($"/api/categorytrees/tree/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}