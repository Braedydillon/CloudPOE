using IncredibleComponentsPoe.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IncredibleComponentsPOE.Models
{
    public class OrderItemEntity
    {
        public int Id { get; set; } // PK
        public int OrderId { get; set; }
        public OrderEntity Order { get; set; }

        public int ProductId { get; set; }
        public ProductEntity Product { get; set; }

        public int Quantity { get; set; } = 1;
    }

}
