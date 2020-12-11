using Leanda.Categories.Domain.Commands;
using MassTransit;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.Domain;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class CategoryEntities : IConsumer<AddEntityCategories>,
                                     IConsumer<DeleteEntityCategories>
    {
        private readonly IElasticClient _elasticClient;

        public CategoryEntities(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task Consume(ConsumeContext<AddEntityCategories> context)
        {
            var node = new { _id = context.Message.EntityId, Name = "Moq Name of Node" };
            var insertDocument = new { CategoriesIds = context.Message.CategoriesIds.Distinct(), Node = node };

            var status = await _elasticClient.IndexAsync(insertDocument,
                i => i.Index("categories").Type("category"));
        }

        public async Task Consume(ConsumeContext<DeleteEntityCategories> context)
        {
            var result = _elasticClient.Search<dynamic>(s => s
                .Index("categories")
                .Type("category")
                .Query(q => q.QueryString(qs => qs.Query(context.Message.EntityId.ToString()))));

            foreach (var hit in result.Hits)
            {
                JObject hitObject = JsonConvert.DeserializeObject<JObject>(hit.Source.ToString());
                IEnumerable<string> categoriesIds = hitObject.Value<JArray>("CategoriesIds").Select(x => x.ToString());
                categoriesIds = categoriesIds.Where(x => !context.Message.CategoriesIds.Select(z => z.ToString()).Contains(x));

                if (categoriesIds.Any())
                {
                    var indexDocument = new { CategoriesIds = categoriesIds.Distinct() };
                    await _elasticClient.UpdateAsync<dynamic>(hit.Id,
                         i => i.Doc(indexDocument).Index("categories").Type("category"));
                }
                else
                {
                    await _elasticClient.DeleteAsync(new DeleteRequest("categories", "category", hit.Id));
                }
            }
        }
    }
}
