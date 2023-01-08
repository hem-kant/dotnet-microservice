using Common;
using System;
namespace Inventory.Service.Entities
{
    public class CatalogItem : IEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public String Description { get; set; }
    }
}