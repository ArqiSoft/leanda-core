using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class DeleteEntityCategoryFixture
    {
        public Guid RootCategoryId;
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }


        public DeleteEntityCategoryFixture(OsdrWebTestHarness harness)
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode("Category Root", new List<TreeNode>()
                {
                    new TreeNode("My Test Category"),
                    new TreeNode("Projects Two")
                })
            };

            var response = harness.JohnApi.PostData("api/categorytrees/tree", categories).Result;

            var content = response.Content.ReadAsStringAsync().Result;

            RootCategoryId = Guid.Parse(content);

            harness.WaitWhileCategoryTreePersisted(RootCategoryId);

            BlobId = harness.JohnBlobStorageClient.AddResource(harness.JohnId.ToString(), "Chemical-diagram.png", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            FileId = harness.WaitWhileFileProcessed(BlobId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class DeleteEntityCategory : OsdrWebTest, IClassFixture<DeleteEntityCategoryFixture>
    {
        private Guid RootCategoryId;
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }


        public DeleteEntityCategory(OsdrWebTestHarness harness, ITestOutputHelper output, DeleteEntityCategoryFixture fixture) : base(harness, output)
        {
            RootCategoryId = fixture.RootCategoryId;
            BlobId = fixture.BlobId;
            FileId = fixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task DeleteOneCategoryToEntity()
        {
            var fileNodeResponse = await JohnApi.GetNodeById(FileId);
            var fileNode = await fileNodeResponse.Content.ReadAsJObjectAsync();
            var fileNodeId = Guid.Parse(fileNode.Value<string>("id"));

            var treeResponse = await JohnApi.GetData($"api/categorytrees/tree/{RootCategoryId}");
            var treeContent = await treeResponse.Content.ReadAsJObjectAsync();
            var categoryId1 = treeContent["nodes"][0]["children"][0]["id"].ToString();
            var categoryId2 = treeContent["nodes"][0]["children"][1]["id"].ToString();

            // add categories to entity
            await JohnApi.PostData($"/api/categoryentities/entities/{fileNodeId}/categories", new List<string> { categoryId1, categoryId2 });
            WebFixture.WaitWhileCategoryIndexed(categoryId1.ToString());
            WebFixture.WaitWhileCategoryIndexed(categoryId2.ToString());
            // check if node exists by categoryId1
            var firstCategoryAddedNodeRequest = await JohnApi.GetData($"/api/categoryentities/categories/{categoryId1}");
            var firstCategoryAddedNode = await firstCategoryAddedNodeRequest.Content.ReadAsJArrayAsync();
            firstCategoryAddedNode.First().Value<string>("id").Should().Be(fileNodeId.ToString());

            // delete first category from node
            await JohnApi.DeleteData($"/api/categoryentities/entities/{fileNodeId}/categories/{categoryId1}");
            WebFixture.WaitWhileCategoryDeleted(categoryId1.ToString());
            // check if node contains categoryId1
            var firstCategoryDeletedNodeRequest = await JohnApi.GetData($"/api/categoryentities/categories/{categoryId1}");
            var firstCategoryDeletedNode = await firstCategoryDeletedNodeRequest.Content.ReadAsJArrayAsync();
            firstCategoryDeletedNode.Should().BeEmpty();

            var secondCategoryAddedNodeRequest = await JohnApi.GetData($"/api/categoryentities/categories/{categoryId2}");
            var secondCategoryAddedNode = await secondCategoryAddedNodeRequest.Content.ReadAsJArrayAsync();
            secondCategoryAddedNode.First().Value<string>("id").Should().Be(fileNodeId.ToString());
        }
    }
}
