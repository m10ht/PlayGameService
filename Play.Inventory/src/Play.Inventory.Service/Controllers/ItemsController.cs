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
        private readonly IRepository<InventoryItem> _repository;
        private readonly CatalogClient _catalogClient;

        public ItemsController(IRepository<InventoryItem> repository, CatalogClient catalogClient)
        {
            _repository = repository;
            _catalogClient = catalogClient;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }
            // var items = (await _repository.GetAllAsync(item => item.UserId == userId)).Select(item => item.AsDto());
            var catalogItems = await _catalogClient.GetCatalogItemsAsncn();
            var inventoryItemEntities = await _repository.GetAllAsync(item => item.UserId == userId);

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItems.Single(c => c.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });
            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemDto)
        {
            var itemInventory = await _repository.GetItemAsync(
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
                await _repository.CreateAsync(itemInventory);
            }
            else
            {
                itemInventory.Quantity += grantItemDto.Quantity;
                await _repository.UpdateAsync(itemInventory);
            }

            return Ok();
        }
    }
}