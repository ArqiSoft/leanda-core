using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class DeleteNodeFromCategoryTreeViaPatch : OsdrWebTest, IClassFixture<DeleteCategoryFixture>
    {
        private Guid CategoryId;
        private Guid ChildCategoryId;

        public DeleteNodeFromCategoryTreeViaPatch(OsdrWebTestHarness harness, ITestOutputHelper output, DeleteCategoryFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
            ChildCategoryId = GetNodeIdsForCategory(harness.JohnApi, CategoryId).Result.Last();
        }

        private async Task<IEnumerable<Guid>> GetNodeIdsForCategory(OsdrWebClient client, Guid categoryId)
        {
            var response = await client.GetData($"/api/categorytrees/tree/{categoryId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            json = json.Replace("_id", "id");
            var treeJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)["nodes"].ToString();
            var tree = JsonConvert.DeserializeObject<List<TreeNode>>(treeJson);
            return tree.GetNodeIds();
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTreeOperations_DeleteNodeFromCategoryTree_ExpectedCategoryWithoutDeletedCategory()
        {
            //will be sure that we`re deleting node which exists
            var nodesIds = await GetNodeIdsForCategory(JohnApi, CategoryId);
            nodesIds.Should().Contain(ChildCategoryId);

            var url = $"/api/categorytrees/tree/{CategoryId}/{ChildCategoryId}";
            var data = $"[{{'op':'replace','path':'isDeleted','value': true}}]";

            var response = await JohnApi.PatchData(url, data);
            response.EnsureSuccessStatusCode();
            Harness.WaitWhileCategoryTreeNodeDeletePersisted(ChildCategoryId);

            //And now we got rid from it
            nodesIds = await GetNodeIdsForCategory(JohnApi, CategoryId);
            nodesIds.Should().NotContain(ChildCategoryId);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTreeOperations_DeleteNonExistantNodeFromCategoryTree_ReturnsNotFoundCode()
        {
            var url = $"/api/categorytrees/tree/{CategoryId}/{Guid.NewGuid()}";
            var data = $"[{{'op':'replace','path':'isDeleted','value': true}}]";

            var response = await JohnApi.PatchData(url, data);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTreeOperations_DeleteNodeFromNonExistantCategoryTree_ReturnsNotFoundCode()
        {
            var url = $"/api/categorytrees/tree/{Guid.NewGuid()}/{ChildCategoryId}";
            var data = $"[{{'op':'replace','path':'isDeleted','value': true}}]";

            var response = await JohnApi.PatchData(url, data);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}