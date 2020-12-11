using Leanda.Categories.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class UpdateCategoryTreeRequest
    {
        public bool IsDeleted { get; set; } = false;
        public IList<TreeNode> Nodes { get; set; } = new List<TreeNode>();
    }
}
