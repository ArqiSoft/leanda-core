using CQRSlite.Domain;
using Leanda.Categories.Domain.Events;
using Leanda.Categories.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Leanda.Categories.Domain
{
    public class CategoryTree : AggregateRoot
    {
        /// <summary>
        /// User Id of the person who created the file
        /// </summary>
        public Guid CreatedBy { get; private set; }

        /// <summary>
        /// Date and time when file was created
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; private set; }

        /// <summary>
        /// User Id of person who changed the file last time
        /// </summary>
        public Guid UpdatedBy { get; protected set; }

        /// <summary>
        /// Date and time when file was changed last time
        /// </summary>
        public DateTimeOffset UpdatedDateTime { get; protected set; }

        public List<TreeNode> Nodes { get; protected set; }

        private void Apply(CategoryTreeCreated e)
        {
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            Nodes = e.Nodes;
        }

        private void Apply(CategoryTreeUpdated e)
        {
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;

            if (e.ParentId == null)
            {
                Nodes = e.Nodes;
            }
            else
            {
                Nodes.UpdateCategoryNodeById(e.Id, e.Nodes);
            }
        }

        private void Apply(CategoryTreeNodeDeleted e)
        {
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            Nodes.DeleteCategoryNodeById(e.Id);
        }

        protected CategoryTree()
        {
        }

        public CategoryTree(Guid id, Guid userId, List<TreeNode> nodes)
        {
            Id = id;
            ApplyChange(new CategoryTreeCreated(Id, userId, nodes));
        }

        public void Update(Guid userId, Guid? parentId, List<TreeNode> nodes)
        {
            ApplyChange(new CategoryTreeUpdated(Id, userId, parentId, nodes));
        }

        public void DeleteNode(Guid userId, Guid nodeId)
        {
            ApplyChange(new CategoryTreeNodeDeleted(Id, userId, nodeId));
        }

        public void Delete(Guid userId)
        {
            ApplyChange(new CategoryTreeDeleted(Id, userId));
        }
    }
}
