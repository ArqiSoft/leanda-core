using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.EndToEndTests.Tests.Categories
{
    public class AddEntityCategoriesFixture
    {
        public Guid TreeId { get; set; }
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }

        public AddEntityCategoriesFixture(OsdrTestHarness harness)
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

            TreeId = Guid.Parse(content);

            harness.WaitWhileCategoryTreePersisted(TreeId);

            BlobId = harness.JohnBlobStorageClient.AddResource(harness.JohnId.ToString(), "Chemical-diagram.png", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            FileId = harness.WaitWhileFileProcessed(BlobId);

            // add category to entity
            response = harness.JohnApi.PostData($"/api/categoryentities/entities/{FileId}/categories", new List<Guid> { TreeId }).Result;
            response.EnsureSuccessStatusCode();
            harness.WaitWhileCategoryIndexed(FileId.ToString());
        }
    }

    [Collection("OSDR Test Harness")]
    public class AddEntityCategories : OsdrWebTest, IClassFixture<AddEntityCategoriesFixture>
    {
        private Guid TreeId;
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }

        public AddEntityCategories(OsdrTestHarness harness, ITestOutputHelper output, AddEntityCategoriesFixture fixture) : base(harness, output)
        {
            TreeId = fixture.TreeId;
            BlobId = fixture.BlobId;
            FileId = fixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task AddCategory_AddingCategoryToEntity_CategoryIdShouldAppearInCategoriesListForEntity()
        {
            var elasticSearchNodesRequest = await JohnApi.GetData($"/api/categoryentities/categories/{TreeId}");
            var elasticSearchNodes = await elasticSearchNodesRequest.Content.ReadAsJArrayAsync();
            elasticSearchNodes.Count.Should().Be(1);
            elasticSearchNodes.Single().Value<string>("id").Should().Be(FileId.ToString());
        }
    }
}
