using Leanda.Categories.Domain.ValueObjects;
using MassTransit;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain.Commands
{
    public interface CreateCategoryTree : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        List<TreeNode> Nodes { get; set; }
        Guid UserId { get; }
	}
}
