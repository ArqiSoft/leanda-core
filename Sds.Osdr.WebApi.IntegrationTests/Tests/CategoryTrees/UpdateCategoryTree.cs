using FluentAssertions;
using FluentAssertions.Json;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class UpdateCategoryTreeFixture
    {
        public Guid CategoryId;

        public UpdateCategoryTreeFixture(OsdrWebTestHarness harness)
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

            var nodeIds = GetNodeIdsForCategory(harness, CategoryId).Result.ToList();

            var guidOne = nodeIds.ElementAt(0);
            var guidTwo = nodeIds.ElementAt(1);
            var guidThree = nodeIds.ElementAt(2);

            var json = $@"[
              {{
                'id': '{guidOne}',
                'title': 'Level 0: Main Node 1',
                'children': [
                  {{ 'id': '{guidTwo}', 'title': 'Level 1: Node 1', 'children': null }},
                  {{ 'id': '{guidThree}', 'title': 'Level 1: Node 2', 'children': null }}
                ]
              }},
              {{ 'title': 'NoNameNode' }},
              {{ 'title': '1' }},
              {{ 'title': '2' }},
              {{ 'title': '3' }},
              {{ 'title': '4', 'children': [{{ 'title': '4-1' }}, {{ 'title': '4-2', 'children': [{{ 'title': '4-2-1' }}] }}] }}
            ]";
            categories = JsonConvert.DeserializeObject<List<TreeNode>>(json);

            response = harness.JohnApi.PutData($"/api/categorytrees/tree/{CategoryId}", categories).Result;

            harness.WaitWhileCategoryTreeUpdatedPersisted(CategoryId);
        }

        private async Task<IEnumerable<Guid>> GetNodeIdsForCategory(OsdrWebTestHarness harness, Guid categoryId)
        {
            var response = await harness.JohnApi.GetData($"/api/categorytrees/tree/{categoryId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            json = json.Replace("_id", "id");
            var treeJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)["nodes"].ToString();
            var tree = JsonConvert.DeserializeObject<List<TreeNode>>(treeJson);
            return tree.GetNodeIds();
        }
    }

    [Collection("OSDR Test Harness")]
    public class UpdateCategoryTree : OsdrWebTest, IClassFixture<UpdateCategoryTreeFixture>
    {
        private Guid CategoryId;

        public UpdateCategoryTree(OsdrWebTestHarness harness, ITestOutputHelper output, UpdateCategoryTreeFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateExistantCategoryTree_BuiltExpectedDocument()
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
            jsonCategory["version"].Value<int>().Should().Be(2);

            jsonCategory.Should().HaveElement("nodes");
            var treeNodes = jsonCategory["nodes"].Value<JArray>();
            treeNodes.Should().HaveCount(6);
            treeNodes.Select(i => i.Should().HaveElement("id"));
            var titles = treeNodes.Select(i => i["title"].Value<string>());
            titles.Should().Contain(new List<string> { "Level 0: Main Node 1", "NoNameNode", "1", "2", "3", "4"});
            var firstNode = treeNodes.Where(i => i.Value<string>("title") == "Level 0: Main Node 1").SingleOrDefault();
            firstNode.Should().NotBeNull();
            firstNode.Should().HaveElement("title");
            var insideNodes = firstNode["children"].Value<JArray>();
            insideNodes.Should().HaveCount(2);
            var insideTitles = insideNodes.Select(i => i["title"].Value<string>());
            insideTitles.Should().Contain(new List<string> { "Level 1: Node 1", "Level 1: Node 2" });
            insideNodes.Select(i => i.Should().HaveElement("id"));

            var lastNode = treeNodes.Where(i => i.Value<string>("title") == "4").SingleOrDefault();
            lastNode.Should().NotBeNull();
            var lastNodeInsideNodes = lastNode["children"].Value<JArray>();
            lastNodeInsideNodes.Should().HaveCount(2);
            var lastNodeInsideTitles = lastNodeInsideNodes.Select(i => i["title"].Value<string>());
            lastNodeInsideTitles.Should().Contain(new List<string> { "4-1", "4-2" });
            lastNodeInsideNodes.Select(i => i.Should().HaveElement("id"));

            var lastNodeSubnode = lastNodeInsideNodes.Where(i => i.Value<string>("title") == "4-2").SingleOrDefault();
            lastNodeSubnode.Should().NotBeNull();
            var lastSubnodeChildren = lastNodeSubnode["children"].Value<JArray>();
            lastSubnodeChildren.Should().HaveCount(1);
            lastSubnodeChildren.Single().Should().HaveElement("id");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateNonExistantCategoryTree_ReturnsNotFoundCode()
        {
            var response = await JohnApi.PutData($"/api/categorytrees/tree/{Guid.NewGuid()}", new List<TreeNode>()
            {
                new TreeNode("Projects", new List<TreeNode>()
                {
                    new TreeNode("Projects One"),
                    new TreeNode("Projects Two")
                })
            });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
} 