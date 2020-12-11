using CQRSlite.Domain.Exception;
using Leanda.Categories.Domain.Events;
using Leanda.Categories.Domain.ValueObjects;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Leanda.Categories.Persistence.EventHandlers
{
    public class CategoryTreeEventHandlers : IConsumer<CategoryTreeCreated>,
                                        IConsumer<CategoryTreeUpdated>,
                                        IConsumer<CategoryTreeDeleted>,
                                        IConsumer<CategoryTreeNodeDeleted>
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _categoryTreeCollection;

        public CategoryTreeEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _categoryTreeCollection = _database.GetCollection<BsonDocument>("CategoryTrees");
        }

        public async Task Consume(ConsumeContext<CategoryTreeCreated> context)
        {
            var tree = new
            {
                CreatedBy = context.Message.UserId,
                CreatedDateTime = context.Message.TimeStamp.UtcDateTime,
                UpdatedBy = context.Message.UserId,
                UpdatedDateTime = context.Message.TimeStamp.UtcDateTime,
                context.Message.Id,
                context.Message.Version,
                context.Message.Nodes
            }.ToBsonDocument();

            await _categoryTreeCollection.InsertOneAsync(tree);

            await context.Publish<CategoryTreePersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<CategoryTreeUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("Nodes", context.Message.Nodes)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var element = await _categoryTreeCollection.FindOneAndUpdateAsync(filter, update);

            if (element == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<CategoryTreeUpdatedPersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<CategoryTreeDeleted> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var element = await _categoryTreeCollection.FindOneAndDeleteAsync(filter);
            if (element == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<CategoryTreeDeletePersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<CategoryTreeNodeDeleted> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var treeBson =_categoryTreeCollection.Find(filter).Project<dynamic>(@"{
                    Nodes:1
                }").Single();
            var nodes = ((IDictionary<string, object>)treeBson)["Nodes"];

            nodes = RemoveNodeById(nodes, context.Message.NodeId);

            var update = Builders<BsonDocument>.Update
               .Set("Nodes", nodes)
               .Set("Version", context.Message.Version);

            var element = await _categoryTreeCollection.FindOneAndUpdateAsync(filter, update);

            await context.Publish<CategoryTreeNodeDeletePersisted>(new
            {
                context.Message.Id,
                context.Message.NodeId,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version
            });
        }

        public dynamic RemoveNodeById(dynamic nodes, Guid id)
        {
            foreach (var node in (nodes as List<dynamic>).ToArray())
            {
                if (node._id == id)
                {
                    nodes = (nodes as List<dynamic>).Except(new List<dynamic> { node }).ToList();
                    return nodes;
                }
                if (node.Children != null)
                {
                    node.Children = RemoveNodeById(node.Children, id);
                }
            }
            return nodes;
        }

    }
}
