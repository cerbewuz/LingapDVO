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

        public DbSet<FillupformHospitalBill> FillupformHospitalBill { get; set; }

        public DbSet<Medicalandlabform> Medicalandlabform { get; set; }

        public DbSet<Funeralburialform> Funeralburialform { get; set; }

        public DbSet<Adminaccount> Adminaccount { get; set; }

        public DbSet<Superadminaccount> Superadminaccount { get; set; }
    }
}
