using FluentAssertions;
using Leanda.Categories.Domain.ValueObjects;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{

    [Collection("OSDR Test Harness")]
    public class CreateCategoryTreeWithoutAdminPermissions : OsdrWebTest, IClassFixture<CreateAndGetCategoryTreeFixture>
    {
        private Guid CategoryId;

        public CreateCategoryTreeWithoutAdminPermissions(OsdrWebTestHarness harness, ITestOutputHelper output, CreateAndGetCategoryTreeFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_CreateNewCategoryTree_ReturnsForbidden()
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode("Projects", new List<TreeNode>()
                {
                    new TreeNode("Projects One"),
                    new TreeNode("Projects Two")
                })
            };

            var response = await JaneApi.PostData("/api/categorytrees/tree", categories);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden); 
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateCategoryTree_ReturnsForbidden()
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode("Projects", new List<TreeNode>()
                {
                    new TreeNode("Projects One"),
                    new TreeNode("Projects Two")
                })
            };

            var response = await JaneApi.PutData($"/api/categorytrees/tree/{CategoryId}", categories);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            response = await JaneApi.PutData($"/api/categorytrees/tree/{CategoryId}/{Guid.NewGuid()}", categories);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_DeleteCategoryTree_ReturnsForbidden()
        {
            var response = await JaneApi.DeleteData($"/api/categorytrees/tree/{CategoryId}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            response = await JaneApi.DeleteData($"/api/categorytrees/tree/{CategoryId}/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateNodeFromCategoryTree_ReturnsForbidden()
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode("Projects", new List<TreeNode>()
                {
                    new TreeNode("Projects One"),
                    new TreeNode("Projects Two")
                })
            };

            var response = await JaneApi.PutData($"/api/categorytrees/tree/{CategoryId}/{Guid.NewGuid()}", categories);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}