using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class UpdateCategoryTreeNodeFixture
    {
        public Guid CategoryId;

        public Guid NodeId;

        public UpdateCategoryTreeNodeFixture(OsdrWebTestHarness harness)
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

            NodeId = nodeIds.Last();
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
    public class UpdateCategoryTreeNode : OsdrWebTest, IClassFixture<UpdateCategoryTreeNodeFixture>
    {
        private Guid CategoryId;
        private Guid NodeId;
        private List<TreeNode> Categories;

        public UpdateCategoryTreeNode(OsdrWebTestHarness harness, ITestOutputHelper output, UpdateCategoryTreeNodeFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
            NodeId = fixture.NodeId;
            Categories = JsonConvert.DeserializeObject<List<TreeNode>>($@"[
              {{
                'title': 'Level 0: Main Node 1',
                'children': [
                  {{ 'title': 'Level 1: Node 1' }},
                  {{ 'title': 'Level 1: Node 2' }}
                ]
              }}
            ]");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateCategoryTreeNode_UpdatedCategoryMayBeExpectedDocument()
        {
            var response = await JohnApi.PutData($"/api/categorytrees/tree/{CategoryId}/{NodeId}", Categories);

            Harness.WaitWhileCategoryTreeUpdatedPersisted(CategoryId);

            response = await JohnApi.GetData($"/api/categorytrees/tree/{CategoryId}");
            response.EnsureSuccessStatusCode();

            var jsonCategory = JToken.Parse(await response.Content.ReadAsStringAsync());
            jsonCategory.Should().ContainsJson($@"
            {{
            	'id': '{CategoryId}',
            	'createdBy': '{JohnId}',
            	'createdDateTime': *EXIST*,
            	'updatedBy': '{JohnId}',
            	'updatedDateTime': *EXIST*,
            	'version': 2,
                'nodes': *EXIST*
            }}");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateCategoryTreeWithNonExistantNode_ReturnsNotFound()
        {
            var response = await JohnApi.PutData($"/api/categorytrees/tree/{CategoryId}/{Guid.NewGuid()}", Categories);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateNodeInNonExistantCategoryTree_ReturnsNotFound()
        {
            var response = await JohnApi.PutData($"/api/categorytrees/tree/{Guid.NewGuid()}/{NodeId}", Categories);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
} 