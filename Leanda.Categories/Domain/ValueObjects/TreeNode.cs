using Newtonsoft.Json;
using Sds.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Leanda.Categories.Domain.ValueObjects
{
    public class TreeNode : ValueObject<TreeNode>
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public List<TreeNode> Children { get; set; }

        public TreeNode(Guid id, string title, List<TreeNode> children = null)
        {
            Id = id;
            Title = title;
            Children = children;
        }

        public TreeNode()
        {
        }

        public TreeNode(string title, List<TreeNode> children = null)
        {
            Title = title;
            Children = children;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<Object>() { Id, Title };
        }
    }

    public static class EnumerableTreeNodeExtensions
    {
        public static void InitNodeIds(this IEnumerable<TreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Id == default(Guid))
                {
                    node.Id = Guid.NewGuid();
                }

                if (node.Children != null && node.Children.Any())
                {
                    node.Children.InitNodeIds();
                }
            }
        }

        public static IEnumerable<Guid> GetNodeIds(this IEnumerable<TreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Id != default(Guid))
                {
                    yield return node.Id;
                }

                if (node.Children != null && node.Children.Any())
                {
                    var guids = node.Children.GetNodeIds();
                    foreach (var id in guids)
                    {
                        yield return id;
                    }
                }
            }
        }

        public static bool ContainsTree(this IEnumerable<TreeNode> nodes, Guid id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    return true;
                }
                if (node.Children != null)
                {
                    return node.Children.ContainsTree(id);
                }
            }
            return false;
        }

        public static bool ContainsTreeInBsonDocument(this MongoDB.Bson.BsonArray nodes, Guid id)
        {
            foreach (var node in nodes)
            {
                if (node["_id"].AsGuid == id)
                {
                    return true;
                }
                if (node["Children"] != MongoDB.Bson.BsonNull.Value)
                {
                    if(node["Children"].AsBsonArray.ContainsTreeInBsonDocument(id)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void DeleteCategoryNodeById(this IEnumerable<TreeNode> nodes, Guid id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    nodes.ToList().Remove(nodes.Single(i => i.Id == id));
                    return;
                }
                if (node.Children != null)
                {
                    node.Children.DeleteCategoryNodeById(id);
                }
            }
        }

        public static void UpdateCategoryNodeById(this IEnumerable<TreeNode> nodes, Guid id, IEnumerable<TreeNode> newNodes)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    node.Children = newNodes.ToList();
                    return;
                }
                if (node.Children != null)
                {
                    node.Children.DeleteCategoryNodeById(id);
                }
            }
        }
    }
}
