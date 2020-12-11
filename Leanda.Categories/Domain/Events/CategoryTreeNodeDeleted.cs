using Leanda.Categories.Domain.ValueObjects;
using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain.Events
{
    public class CategoryTreeNodeDeleted : IUserEvent
	{
        public Guid NodeId { get; set; }

        public CategoryTreeNodeDeleted(Guid id, Guid userId, Guid nodeId)
        {
            Id = id;
            NodeId = nodeId;
            UserId = userId;
		}

        public Guid UserId { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
	}
}
