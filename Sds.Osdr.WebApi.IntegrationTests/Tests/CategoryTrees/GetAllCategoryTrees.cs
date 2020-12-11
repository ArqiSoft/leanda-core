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
    public class GetAllCategoryTrees : OsdrWebTest
    {
        public GetAllCategoryTrees(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            var categories = new List<TreeNode>()
            {
                new TreeNode("Projects", new List<TreeNode>()
                {
                    new TreeNode("Projects One"),
                    new TreeNode("Projects Two")
                })
            };

            for (int i = 0; i < 10; i++)
            {
                var response = JohnApi.PostData("/api/categorytrees/tree", categories).Result;

                var content = response.Content.ReadAsStringAsync().Result;

                var categoryId = Guid.Parse(content);

                Harness.WaitWhileCategoryTreePersisted(categoryId);
            }
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTreeOperations_GetAllCategoryTrees_ExpectedListOfCategories()
        {
            var response = await JohnApi.GetData($"/api/categorytrees/tree");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonCategories = JArray.Parse(await response.Content.ReadAsStringAsync());
            jsonCategories.Should().NotBeEmpty();
            foreach (var category in jsonCategories.Children())
            {
                category.Should().ContainsJson($@"
                {{
            	    'id': *EXIST*,
            	    'createdBy': '{JohnId}',
            	    'createdDateTime': *EXIST*,
            	    'updatedBy': '{JohnId}',
            	    'updatedDateTime': *EXIST*,
            	    'version': *EXIST*
                }}");
            }
        }
    }
}