using Leanda.Categories.Domain.ValueObjects;
using MassTransit;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain.Commands
{
    public interface DeleteCategoryTree : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid? NodeId { get; }
        Guid UserId { get; }
	}
}
