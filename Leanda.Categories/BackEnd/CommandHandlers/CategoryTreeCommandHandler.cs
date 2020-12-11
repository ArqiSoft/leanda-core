using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using Leanda.Categories.Domain;
using Leanda.Categories.Domain.Commands;
using Leanda.Categories.Domain.Events;
using MassTransit;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Leanda.Categories.BackEnd.CommandHandlers
{
    public class CategoryTreeCommandHandler : IConsumer<CreateCategoryTree>,
                                              IConsumer<UpdateCategoryTree>,
                                              IConsumer<DeleteCategoryTree>,
                                              IConsumer<DeleteCategoryTreeNode>
    {
        private readonly ISession _session;

        public CategoryTreeCommandHandler(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateCategoryTree> context)
        {
            var tree = new CategoryTree(context.Message.Id, userId: context.Message.UserId, nodes: context.Message.Nodes);

            await _session.Add(tree);

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<UpdateCategoryTree> context)
        {
            var tree = await _session.Get<CategoryTree>(context.Message.Id);

            tree.Update(context.Message.UserId, context.Message.ParentId, context.Message.Nodes);

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<DeleteCategoryTree> context)
        {
            var tree = await _session.Get<CategoryTree>(context.Message.Id);

            tree.Delete(context.Message.UserId);

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<DeleteCategoryTreeNode> context)
        {
            var tree = await _session.Get<CategoryTree>(context.Message.Id);

            tree.DeleteNode(context.Message.UserId, context.Message.NodeId);

            await _session.Commit();
        }
    }
}
