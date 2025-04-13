using Microsoft.EntityFrameworkCore;
using Cashflow.Consolidado.API.Domain.Aggregates;

namespace Cashflow.Consolidado.API.Infrastructure.Persistence;

public class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options) { }

    public DbSet<SaldoConsolidado> SaldosConsolidados => Set<SaldoConsolidado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SaldoConsolidado>().ToTable("SaldosConsolidados");
    }
}
