using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contratcts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Utility;

namespace Play.Catalog.Service.Controllers
{
    // https://localhost:5001/items
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndPoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndPoint)
        {
            _itemsRepository = itemsRepository;
            _publishEndPoint = publishEndPoint;
        }

        // GET: /items
        [HttpGet]
        public async Task<IEnumerable<ItemDto>> GetAsync()
        {
            var items = (await _itemsRepository.GetAllAsync()).Select(i => i.AsDto());
            return items;
        }

        // GET: /items/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(ItemDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }

        // POST: /items
        [HttpPost]
        public async Task<IActionResult> PostAsync(CreateItemDto createItem)
        {
            var item = new Item
            {
                Name = createItem.Name,
                Description = createItem.Description,
                Price = createItem.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };
            await _itemsRepository.CreateAsync(item);

            await _publishEndPoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        // PUT: /items/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItem)
        {
            var existingItem = await _itemsRepository.GetItemAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = updateItem.Name;
            existingItem.Description = updateItem.Description;
            existingItem.Price = updateItem.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            await _publishEndPoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            return NoContent();
        }

        //DELETE: /items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsycn(Guid id)
        {
            var existingItem = await _itemsRepository.GetItemAsync(id);

            if (existingItem == null)
            {
                return NotFound();
            }

            await _itemsRepository.RemoveAsync(existingItem.Id);

            await _publishEndPoint.Publish(new CatelogItemDeleted(id));

            return NoContent();
        }
    }
}