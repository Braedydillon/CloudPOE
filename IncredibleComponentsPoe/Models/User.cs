using IncredibleComponentsPOE.Models;
using System.ComponentModel.DataAnnotations;
namespace IncredibleComponentsPoe.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "User"; // default role

        public ICollection<OrderItemEntity> CartItems { get; set; } = new List<OrderItemEntity>();
      
    }
}
