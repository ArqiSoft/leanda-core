using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class AddEntityCategoriesFixture
    {
        public Guid CategoryId { get; set; }
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }
        public Guid FileNodeId { get; set; }


        public AddEntityCategoriesFixture(OsdrWebTestHarness harness)
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

            CategoryId = Guid.Parse(content);

            harness.WaitWhileCategoryTreePersisted(CategoryId);

            BlobId = harness.JohnBlobStorageClient.AddResource(harness.JohnId.ToString(), "Chemical-diagram.png", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            FileId = harness.WaitWhileFileProcessed(BlobId);

            var fileNodeResponse = harness.JohnApi.GetNodeById(FileId).Result;
            var fileNode = JsonConvert.DeserializeObject<JObject>(fileNodeResponse.Content.ReadAsStringAsync().Result);
            FileNodeId = Guid.Parse(fileNode.Value<string>("id"));

            // add category to entity
            response = harness.JohnApi.PostData($"/api/categoryentities/entities/{FileNodeId}/categories", new List<Guid> { CategoryId }).Result;
            response.EnsureSuccessStatusCode();
            harness.WaitWhileCategoryIndexed(CategoryId.ToString());
        }
    }

    [Collection("OSDR Test Harness")]
    public class AddEntityCategories : OsdrWebTest, IClassFixture<AddEntityCategoriesFixture>
    {
        private Guid CategoryId;
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }
        public Guid FileNodeId { get; set; }

        public AddEntityCategories(OsdrWebTestHarness harness, ITestOutputHelper output, AddEntityCategoriesFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
            BlobId = fixture.BlobId;
            FileId = fixture.FileId;
            FileNodeId = fixture.FileNodeId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Folder)]
        public async Task AddOneCategoryToEntity()
        {
            var nodesRequest = await JohnApi.GetData($"/api/categoryentities/categories/{CategoryId}");
            var nodes = await nodesRequest.Content.ReadAsJArrayAsync();
            nodes.Count.Should().Be(1);
            nodes[0].Value<string>("id").Should().Be(FileNodeId.ToString());
        }
    }
}