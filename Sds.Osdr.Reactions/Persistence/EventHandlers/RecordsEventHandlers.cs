﻿using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Reactions.Domain.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Reactions.Persistence.EventHandlers
{
    public class RecordsEventHandlers : IConsumer<ReactionCreated>
    {
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Records { get { return database.GetCollection<BsonDocument>("Records"); } }

        public RecordsEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<ReactionCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
