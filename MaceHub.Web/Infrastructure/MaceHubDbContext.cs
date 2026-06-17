using MaceHub.Web.Features.Example;
using Microsoft.EntityFrameworkCore;

namespace MaceHub.Web.Infrastructure;

public class MaceHubDbContext(DbContextOptions<MaceHubDbContext> options) : DbContext(options)
{
    public DbSet<ExampleItem> ExampleItems => Set<ExampleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExampleItem>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).IsRequired().HasMaxLength(200);
            b.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        });
    }
}
