using LingapDVO.Models;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Register> Register { get; set; }
    }
}
