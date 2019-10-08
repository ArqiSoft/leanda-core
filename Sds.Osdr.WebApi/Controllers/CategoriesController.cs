using Leanda.Categories.Domain.Commands;
using Leanda.Categories.Domain.ValueObjects;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.WebApi.Filters;
using Sds.Osdr.WebApi.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sds.Osdr.WebApi.Extensions;
using System.Linq;
using CQRSlite.Domain;
using Leanda.Categories.Domain;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "Administrator")]
    [UserInfoRequired]
    public class CategoriesController : MongoDbController, IPaginationController
    {
        private IBusControl Bus;
        private IMongoCollection<BsonDocument> CategoryTreeCollection;
        private readonly ISession _session;

        //IElasticClient _elasticClient;
        private IUrlHelper _urlHelper;
        public CategoriesController(IMongoDatabase database, IBusControl bus /*IElasticClient elasticClient*/, IUrlHelper urlHelper, ISession session) : base(database)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
            CategoryTreeCollection = Database.GetCollection<BsonDocument>("CategoryTrees");
            _session = session ?? throw new ArgumentNullException(nameof(session));
            //_elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
        }

        /// <summary>
        /// Returns all available categories
        /// </summary>
        /// <returns></returns>
        [HttpGet("tree")]
        public async Task<IActionResult> GetAllCategoryTree([FromQuery]PaginationRequest request)
        {
            var result = CategoryTreeCollection.Find(_ => true).Project<dynamic>(@"{
                    CreatedBy:1,
                    CreatedDateTime:1,
                    UpdatedBy:1,
                    UpdatedDateTime:1,
                    Version:1
                }");

            if (request != null)
            {
                var pagedResult = await result.ToPagedListAsync(request.PageNumber, request.PageSize);

                this.AddPaginationHeader(request, pagedResult, nameof(GetAllCategoryTree), null, null, null);

                return Ok(pagedResult);
            }

            return Ok(result.ToListAsync());
        }

        /// <summary>
        /// Create new category tree
        /// </summary>
        /// <returns></returns>
        [HttpPost("tree")]
        public async Task<IActionResult> CreateCategoryTree([FromBody] List<TreeNode> nodes)
        {
            Guid categoriesTreeId = Guid.NewGuid();

            nodes.InitNodeIds();

            await Bus.Publish<CreateCategoryTree>(new
            {
                Id = categoriesTreeId,
                UserId,
                Nodes = nodes
            });

            return CreatedAtRoute("GetCategoriesTree", new { id = categoriesTreeId }, categoriesTreeId.ToString());
        }

        /// <summary>
        /// Get categories tree by Id
        /// </summary>
        /// <param name="id">Caregories tree aggregate ID</param>
        /// <returns></returns>
        [HttpGet("tree/{id}", Name = "GetCategoriesTree")]
        public async Task<IActionResult> GetCategoriesTree(Guid id)
        {
            var tree = await CategoryTreeCollection.Find(new BsonDocument("_id", id))
                .Project<dynamic>(@"{
                    CreatedBy:1,
                    CreatedDateTime:1,
                    UpdatedBy:1,
                    UpdatedDateTime:1,
                    Version:1,
                    Nodes:1
                }")
                .FirstOrDefaultAsync();

            if (tree == null)
            {
                return BadRequest();
            }

            return Ok(tree);
        }

        /// <summary>
        /// Update categories tree
        /// </summary>
        /// <param name="id">Categories tree ID</param>
        /// <param name="nodes">New categories tree nodes</param>
        /// <param name="version">Carrent cattegories tree object version</param>
        /// <returns></returns>
        [HttpPut("tree/{id}")]
        public async Task<IActionResult> UpdateCategoriesTree(Guid id, [FromBody] List<TreeNode> nodes, int version)
        {
            var tree = await _session.Get<CategoryTree>(id);

            if(tree == null)
            {
                return NotFound();
            }

            var aggregateIds = tree.Nodes.GetNodeIds();
            var requestIds = nodes.GetNodeIds();

            if(!requestIds.All(i => aggregateIds.Contains(i)))
            {
                var invalidIds = requestIds.Where(i => !aggregateIds.Contains(i));
                
                return BadRequest($"Can not find nodes with ids {string.Join(", ", invalidIds)}");
            }

            nodes.InitNodeIds();

            await Bus.Publish<UpdateCategoryTree>(new
            {
                Id = id,
                UserId = UserId,
                Nodes = nodes,
                ExpectedVersion = version
            });

            return Accepted();
        }

        /// <summary>
        /// Update categories tree node
        /// </summary>
        /// <param name="id">Categories tree ID</param>
        /// <param name="nodeId">Categories tree node ID</param>
        /// <param name="nodes">New categories tree nodes</param>
        /// <param name="version">Carrent cattegories tree object version</param>
        /// <returns></returns>
        [HttpPut("tree/{id}/{nodeId}")]
        public async Task<IActionResult> UpdateCategoriesTreeNode(Guid id, Guid nodeId, [FromBody] List<TreeNode> nodes, int version)
        {
            await Bus.Publish<UpdateCategoryTree>(new
            {
                Id = id,
                ParentId = nodeId,
                UserId = UserId,
                Nodes = nodes,
                ExpectedVersion = version
            });

            return Accepted();
        }

        [HttpDelete("tree/{id}")]
        public async Task<IActionResult> DeleteCategoriesTreeNode(Guid id, int version)
        {
            await Bus.Publish<DeleteCategoryTree>(new
            {
                Id = id,
                UserId,
                ExpectedVersion = version
            });

            return Accepted();
        }

        [HttpDelete("tree/{id}/{nodeId}")]
        public async Task<IActionResult> DeleteCategoriesTreeNode(Guid id, Guid nodeId, int version)
        {
            await Bus.Publish<DeleteCategoryTree>(new
            {
                Id = id,
                NodeId = nodeId,
                UserId,
                ExpectedVersion = version
            });

            return Accepted();
        }
        
        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, Guid? entityId = null, string filter = null, IEnumerable<string> fields = null)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;
            return _urlHelper.Link(action, new RouteValueDictionary
            {
                { "id", entityId },
                { "pageSize",  request.PageSize },
                { "pageNumber", pageNumber },
                { "$filter", filter },
                { "$projection",  string.Join(",", fields ?? new string[] { }) }
            });
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, RouteValueDictionary routeValueDictionary)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;

            return _urlHelper.Link(action, routeValueDictionary);
        }
    }
}
