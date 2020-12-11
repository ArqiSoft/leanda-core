using Leanda.Categories.Domain.ValueObjects;
using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain.Events
{
    public interface CategoryTreeNodeDeletePersisted
    {
        Guid NodeId { get; }
        Guid UserId { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        int Version { get; }
    }
}
