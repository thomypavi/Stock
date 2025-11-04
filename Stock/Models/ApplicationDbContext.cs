using Microsoft.EntityFrameworkCore;

namespace Stock.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Cada DbSet equivale a una tabla
        public DbSet<Usuario> Usuarios { get; set; }
    }
}
