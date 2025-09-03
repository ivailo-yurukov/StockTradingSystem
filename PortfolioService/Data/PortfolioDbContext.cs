using Microsoft.EntityFrameworkCore;
using PortfolioService.Models;
using System.Collections.Generic;

namespace PortfolioService.Data
{
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

        public DbSet<Holding> Holdings { get; set; }
        public DbSet<Price> Prices { get; set; }
    }
}
