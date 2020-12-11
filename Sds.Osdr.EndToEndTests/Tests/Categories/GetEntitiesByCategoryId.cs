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
    [Collection("OSDR Test Harness")]
    public class GetEntitiesByCategoryId : OsdrWebTest, IClassFixture<GetCategoriesIdsByEntityIdFixture>
    {
        private Guid CategoryId;
        public Guid FileId { get; set; }


        public GetEntitiesByCategoryId(OsdrTestHarness harness, ITestOutputHelper output, GetCategoriesIdsByEntityIdFixture fixture) : base(harness, output)
        {
            CategoryId = fixture.CategoryId;
            FileId = fixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Categories)]
        public async Task EntityCategories_GetCategoriesIdsByEntityId_ShouldReturnEntityWithExpectedId()
        {
            var entitiesRequest = await JohnApi.GetData($"/api/categoryentities/categories/{CategoryId}");
            var entities = await entitiesRequest.Content.ReadAsJArrayAsync();
            entities.Should().HaveCount(1);
            var entity = entities.Single();
            entity["id"].Value<string>().Should().Be(FileId.ToString());
        }
    }
}
