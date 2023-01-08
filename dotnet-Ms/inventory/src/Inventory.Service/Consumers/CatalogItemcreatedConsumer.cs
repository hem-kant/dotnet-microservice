using System.Threading.Tasks;
using Catalog.Contracts;
using Common;
using Inventory.Service.Entities;
using MassTransit;

namespace Inventory.Service.Consumers
{
    public class CatalogItemcreatedConsumer : IConsumer<CatalogItemCreated>
    {
        private readonly IRepository<CatalogItem> repository;

        public CatalogItemcreatedConsumer(IRepository<CatalogItem> repository)
        {
            this.repository = repository;
        }
        public async Task Consume(ConsumeContext<CatalogItemCreated> context)
        {
            var message = context.Message;

            var item = await repository.GetAsync(message.ItemId);

            if (item != null)
            {
                return;
            }
            item = new CatalogItem
            {
                Id = message.ItemId,
                Name = message.Name,
                Description = message.Description
            };

            await repository.CreateAsync(item);
        }
    }

}