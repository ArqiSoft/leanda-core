using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class DeleteCategoryFixture
    {
        public Guid CategoryId { get; }

        public DeleteCategoryFixture(OsdrWebTestHarness harness)
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode(Guid.NewGuid(), "Projects", new List<TreeNode>()
                {
                    new TreeNode(Guid.NewGuid(), "Projects One"),
                    new TreeNode(Guid.NewGuid(), "Projects Two")
                })
            };

            var response = harness.JohnApi.PostData("/api/categorytrees/tree", categories).Result;

            var content = response.Content.ReadAsStringAsync().Result;

            CategoryId = Guid.Parse(content);

            harness.WaitWhileCategoryTreePersisted(CategoryId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class DeleteCategoryTree : OsdrWebTest, IClassFixture<DeleteCategoryFixture>
    {
        private Guid CategoryId;

        public DeleteCategoryTree(OsdrWebTestHarness harness, ITestOutputHelper output, DeleteCategoryFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Folder)]
        public async Task CategoryTreeOperations_DeleteCategoryTree_ExpectedUpdatedCategory()
        {
            var response = await JohnApi.DeleteData($"/api/categorytrees/tree/{CategoryId}");
            response.EnsureSuccessStatusCode();
            Harness.WaitWhileCategoryTreeDeletePersisted(CategoryId);

            response = await JohnApi.GetData($"/api/categorytrees/tree/{CategoryId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
    }
}