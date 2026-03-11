using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Models;

namespace Feniks.Shared.Data;

public class FeniksDbContext : DbContext
{
    public FeniksDbContext(DbContextOptions<FeniksDbContext> options) : base(options) { }
    
    // Существующие DbSet
    public DbSet<ConstructionObject> ConstructionObjects { get; set; }
    public DbSet<Contractor> Contractors { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Estimate> Estimates { get; set; }
    public DbSet<EstimateStage> EstimateStages { get; set; }
    public DbSet<EstimateItemGroup> EstimateItemGroups { get; set; }
    public DbSet<EstimateItem> EstimateItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Work> Works { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Company> Companies { get; set; }
    
    // Новые DbSet для справочников
    public DbSet<ReferenceCategory> ReferenceCategories { get; set; }
    public DbSet<RefCatalog> RefCatalogs { get; set; }
    public DbSet<ReferenceItem> ReferenceItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ========== СУЩЕСТВУЮЩИЕ НАСТРОЙКИ ==========
        
        // ConstructionObject - НАСТРОЙКИ ДЛЯ BUDGET УДАЛЕНЫ
        
        // Estimate
        modelBuilder.Entity<Estimate>()
            .Property(e => e.TotalCost)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Estimate>()
            .Property(e => e.CustomerPrice)
            .HasPrecision(18, 2);
            
        // EstimateItem
        modelBuilder.Entity<EstimateItem>()
            .Property(e => e.Price)
            .HasPrecision(18, 2);
        modelBuilder.Entity<EstimateItem>()
            .Property(e => e.Quantity)
            .HasPrecision(18, 2);
        modelBuilder.Entity<EstimateItem>()
            .Property(e => e.Markup)
            .HasPrecision(5, 2);
        modelBuilder.Entity<EstimateItem>()
            .Property(e => e.Progress)
            .HasPrecision(5, 2);
            
        // Payment
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);
            
        // Purchase
        modelBuilder.Entity<Purchase>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Purchase>()
            .Property(p => p.Quantity)
            .HasPrecision(18, 2);
            
        // Material
        modelBuilder.Entity<Material>()
            .Property(m => m.Price)
            .HasPrecision(18, 2);
            
        // Work
        modelBuilder.Entity<Work>()
            .Property(w => w.Progress)
            .HasPrecision(5, 2);
            
        // Связи Estimate
        modelBuilder.Entity<Estimate>()
            .HasMany(e => e.Stages)
            .WithOne(s => s.Estimate)
            .HasForeignKey(s => s.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<EstimateStage>()
            .HasMany(s => s.Groups)
            .WithOne(g => g.Stage)
            .HasForeignKey(g => g.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EstimateStage>()
            .HasMany(s => s.Items)
            .WithOne(i => i.Stage)
            .HasForeignKey(i => i.StageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EstimateItem>()
            .HasOne(i => i.Group)
            .WithMany(g => g.Items)
            .HasForeignKey(i => i.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EstimateItem>()
            .Property(e => e.CustomerPrice)
            .HasPrecision(18, 2);
            
        // ========== НОВЫЕ НАСТРОЙКИ ДЛЯ СПРАВОЧНИКОВ ==========
        
        // ReferenceCategory - настройка иерархии
        modelBuilder.Entity<ReferenceCategory>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // RefCatalog - связь с категорией
        modelBuilder.Entity<RefCatalog>()
            .HasOne(c => c.Category)
            .WithMany(c => c.RefCatalogs)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // ReferenceItem - связь с каталогом
        modelBuilder.Entity<ReferenceItem>()
            .HasOne(i => i.Reference)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.ReferenceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<ReferenceItem>()
            .Property(i => i.Price)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<ReferenceItem>()
            .Property(i => i.Markup)
            .HasPrecision(5, 2);
            
        modelBuilder.Entity<ReferenceItem>()
            .Property(i => i.CustomerPrice)
            .HasPrecision(18, 2);
            
        // Добавляем настройку для Quantity
        modelBuilder.Entity<ReferenceItem>()
            .Property(i => i.Quantity)
            .HasPrecision(18, 2);
            
        // Настройка индексов для оптимизации поиска
        modelBuilder.Entity<ReferenceCategory>()
            .HasIndex(c => c.ParentId);
            
        modelBuilder.Entity<RefCatalog>()
            .HasIndex(c => c.CategoryId);
            
        modelBuilder.Entity<ReferenceItem>()
            .HasIndex(i => i.ReferenceId);
    }
}