using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Play.Utility;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _repositoryInventory;
        private readonly IRepository<CatalogItem> _repositoryCatalog;

        public ItemsController(IRepository<InventoryItem> repositoryInventory, IRepository<CatalogItem> repositoryCatalog)
        {
            _repositoryInventory = repositoryInventory;
            _repositoryCatalog = repositoryCatalog;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }
            // var items = (await _repository.GetAllAsync(item => item.UserId == userId)).Select(item => item.AsDto());
            var inventoryItemEntities = await _repositoryInventory.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemEntities.Select(i => i.CatalogItemId);
            var catalogItemEntities = await _repositoryCatalog.GetAllAsync(item => itemIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(c => c.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });
            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemDto)
        {
            var itemInventory = await _repositoryInventory.GetItemAsync(
                item => item.UserId == grantItemDto.UserId && item.CatalogItemId == grantItemDto.CatalogItemId);

            if (itemInventory == null)
            {
                itemInventory = new()
                {
                    CatalogItemId = grantItemDto.CatalogItemId,
                    UserId = grantItemDto.UserId,
                    Quantity = grantItemDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };
                await _repositoryInventory.CreateAsync(itemInventory);
            }
            else
            {
                itemInventory.Quantity += grantItemDto.Quantity;
                await _repositoryInventory.UpdateAsync(itemInventory);
            }

            return Ok();
        }
    }
}