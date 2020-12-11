using FluentAssertions;
using FluentAssertions.Json;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.EndToEndTests.Tests.Categories
{
    public class CreateCategoryTreeFixture
    {
        public Guid CategoryId;

        public CreateCategoryTreeFixture(OsdrTestHarness harness)
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
    public class CreateCategoryTree : OsdrWebTest, IClassFixture<CreateCategoryTreeFixture>
    {
        private Guid CategoryId;

        public CreateCategoryTree(OsdrTestHarness harness, ITestOutputHelper output, CreateCategoryTreeFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_CreateNewCategoryTree_BuiltExpectedDocument()
        {
            var contentRequest = await JohnApi.GetData($"/api/categorytrees/tree/{CategoryId}");

            var jsonCategory = await contentRequest.Content.ReadAsJObjectAsync(); 

            jsonCategory.Should().HaveElement("id");
            jsonCategory["id"].Value<string>().Should().Be(CategoryId.ToString());

            jsonCategory.Should().HaveElement("createdBy");
            jsonCategory["createdBy"].Value<string>().Should().Be(JohnId.ToString());

            jsonCategory.Should().HaveElement("createdDateTime")
                .And.HaveElement("createdDateTime")
                .And.HaveElement("updatedDateTime");

            jsonCategory.Should().HaveElement("version");
            jsonCategory["version"].Value<int>().Should().Be(1);

            jsonCategory.Should().HaveElement("nodes");
            var treeNodes = jsonCategory["nodes"].Value<JArray>();
            treeNodes.Should().HaveCount(1);
            var mainNode = treeNodes.Single();
            mainNode.Should().HaveElement("title");
            mainNode["title"].Value<string>().Should().Be("Projects");
            var insideNodes = mainNode["children"].Value<JArray>();
            insideNodes.Should().HaveCount(2);
            var titles = insideNodes.Select(i => i["title"].Value<string>());
            titles.Should().Contain(new List<string> { "Projects One", "Projects Two" });
            insideNodes[0].Should().HaveElement("id");
            insideNodes[1].Should().HaveElement("id");
        }
    }
}
