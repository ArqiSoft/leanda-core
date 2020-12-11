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
    [Collection("OSDR Test Harness")]
    public class UpdateCategoryTreeNodeViaPatch : OsdrWebTest, IClassFixture<UpdateCategoryTreeNodeFixture>
    {
        private Guid CategoryId;
        private Guid NodeId;

        public UpdateCategoryTreeNodeViaPatch(OsdrWebTestHarness harness, ITestOutputHelper output, UpdateCategoryTreeNodeFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
            NodeId = fixture.NodeId;            
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task CategoryTree_UpdateCategoryTreeNode_UpdatedCategoryMayBeExpectedDocument()
        {
            var json = $@"[
              {{
                'title': 'Level 0: Main Node 1',
                'children': [
                  {{ 'title': 'Level 1: Node 1' }},
                  {{ 'title': 'Level 1: Node 2' }}
                ]
              }}
            ]";

            var url = $"/api/categorytrees/tree/{CategoryId}";
            var data = $"[{{'op':'replace','path':'nodes','value': {json} }}]";

            var response = await JohnApi.PatchData(url, data);

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
    }
} 