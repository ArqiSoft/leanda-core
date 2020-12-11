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
    public class GetCategoriesIdsByEntityIdFixture
    {
        public Guid CategoryId;
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }


        public GetCategoriesIdsByEntityIdFixture(OsdrTestHarness harness)
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

            harness.JohnApi.PostData($"/api/categoryentities/entities/{FileId}/categories", new List<Guid> { CategoryId }).Wait();

            harness.WaitWhileCategoryIndexed(CategoryId.ToString());
        }
    }

    [Collection("OSDR Test Harness")]
    public class GetCategoriesIdsByEntityId : OsdrWebTest, IClassFixture<GetCategoriesIdsByEntityIdFixture>
    {
        private Guid CategoryId;
        public Guid BlobId { get; set; }
        public Guid FileId { get; set; }


        public GetCategoriesIdsByEntityId(OsdrTestHarness harness, ITestOutputHelper output, GetCategoriesIdsByEntityIdFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
            BlobId = fixture.BlobId;
            FileId = fixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryEntities_GetCategoriesIdsByEntityId_ShouldReturnExpectedCategoryIds()
        {
            var content = await JohnApi.GetData($"/api/categoryentities/entities/{FileId}/categories");
            var categoriesIds = await content.Content.ReadAsJArrayAsync();
            categoriesIds.Any(x => x.Value<string>() == CategoryId.ToString()).Should().BeTrue();
        }
    }
}
