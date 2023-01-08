using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalog.Contracts;
using Catalog.Service.Dtos;
using Catalog.Service.Entities;
using Common;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Catalog.Service.Controllers
{
    [ApiController]
    [Route("api/v1/catalog")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> itemsRepository;

        private readonly IPublishEndpoint publishEndpoint;

        private readonly ILogger<ItemsController> _logger;

        // private static int requestCounter = 0;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint, ILogger<ItemsController> logger)
        {
            this.itemsRepository = itemsRepository;
            this.publishEndpoint = publishEndpoint;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            /* requestCounter++;
             System.Console.WriteLine($"Request{requestCounter}: Starting .. ");

             if (requestCounter <= 2)
             {
                 System.Console.WriteLine($"Request{requestCounter}: Delaying .. ");

                 await Task.Delay(TimeSpan.FromSeconds(10));
             }

             if (requestCounter <= 4)
             {
                 System.Console.WriteLine($"Request{requestCounter}: 500 Error .. ");

                 return StatusCode(500);
             }*/


            var items = (await itemsRepository.GetAllAsync())
            .Select(item => item.AsDto());

            //System.Console.WriteLine($"Request{requestCounter}: 200 Ok .. ");
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);// items.Where(item => item.Id == id).SingleOrDefault();
            if (item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> CreateDataAsync(CreateItemDto createItemDto)
        {
            //var item = new ItemDto(Guid.NewGuid(), createItemDto.Name, createItemDto.Description, createItemDto.Price, DateTimeOffset.UtcNow);
            //items.Add(item);

            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await itemsRepository.CreateAsync(item);

            await publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));
            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItemAsync(Guid id, UpdateItemDto updateItemDto)
        {
            /*var existingItem = items.Where(item => item.Id == id).SingleOrDefault();
            if (existingItem == null)
            {
                return NotFound();
            }
            var updatedItem = existingItem with
            {
                Name = updateItemDto.Name,
                Description = updateItemDto.Description,
                Price = updateItemDto.Price
            };

            var index = items.FindIndex(existingItem => existingItem.Id == id);
            items[index] = updatedItem;*/

            var existingItem = await itemsRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }
            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await itemsRepository.UpdateAsync(existingItem);

            await publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            /*var index = items.FindIndex(existingItem => existingItem.Id == id);
            if (index < 0)
            {
                return NotFound();
            }
            items.RemoveAt(index);*/
            var item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            await itemsRepository.RemoveAsync(id);

            await publishEndpoint.Publish(new CatalogItemDeleted(id));
            return NoContent();

        }
    }
}