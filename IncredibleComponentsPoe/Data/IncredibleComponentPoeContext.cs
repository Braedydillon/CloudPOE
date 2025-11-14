using IncredibleComponentsPoe.Models;
using Microsoft.EntityFrameworkCore;

namespace IncredibleComponentsPOE.Data
{
    public class IncredibleComponentPoe : DbContext
    {
        public IncredibleComponentPoe(DbContextOptions<IncredibleComponentPoe> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
