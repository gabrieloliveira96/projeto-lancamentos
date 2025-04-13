using Microsoft.EntityFrameworkCore;
using Cashflow.Lancamentos.API.Domain.Entities;

namespace Cashflow.Lancamentos.API.Infrastructure.Persistence;

public class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : base(options) { }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>().ToTable("Lancamentos");
        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessage");

    }
}
