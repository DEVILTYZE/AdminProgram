using System.Data.Entity;

namespace AdminProgram.Models
{
    public class AdminContext : DbContext
    {
        public DbSet<HostDb> Hosts { get; set; }

        public AdminContext() : base("DefaultConnection")
        {
            if (!Database.Exists())
                Database.Create();
        }
    }
}