using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Infrastructure;

/// <summary>
/// EF Core DbContext for TaskFlow persistence.
/// Owns the database connection + entity mappings.
/// </summary>
public sealed class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Task items persisted in PostgreSQL.
    /// </summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(builder =>
        {
            builder.ToTable("tasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            // Optional field, but constrain length for safety + schema clarity.
            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            builder.Property(t => t.IsCompleted)
                .IsRequired();

            // Persisted timestamp used for ordering + auditing.
            builder.Property(t => t.CreatedAtUtc)
                .IsRequired();

            // Supports "most recent first" queries efficiently.
            builder.HasIndex(t => t.CreatedAtUtc);
        });
    }
}