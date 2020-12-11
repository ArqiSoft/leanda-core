using MassTransit;
using System;

namespace Leanda.Categories.Domain.Commands
{
    public interface DeleteCategoryTreeNode : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid NodeId { get; }
        Guid UserId { get; }
	}
}
