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
    [Collection("OSDR Test Harness")]
    public class DeleteCategoryTreeViaPatch : OsdrWebTest, IClassFixture<DeleteCategoryFixture>
    {
        private Guid CategoryId;

        public DeleteCategoryTreeViaPatch(OsdrWebTestHarness harness, ITestOutputHelper output, DeleteCategoryFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Folder)]
        public async Task CategoryTreeOperations_DeleteCategoryTree_ExpectedUpdatedCategory()
        {
            var url = $"/api/categorytrees/tree/{CategoryId}";
            var data = $"[{{'op':'replace','path':'isDeleted','value':true}}]";

            var response = await JohnApi.PatchData(url, data);
            response.EnsureSuccessStatusCode();
            Harness.WaitWhileCategoryTreeDeletePersisted(CategoryId);

            response = await JohnApi.GetData($"/api/categorytrees/tree/{CategoryId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
    }
}