using System;
using System.Collections.Generic;
using ECommerce.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Discount> Discounts { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderCancellation> OrderCancellations { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<SubCategory> SubCategories { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Carts_CreatedAt");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Carts_Users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.Property(e => e.AddedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_CartItems_AddedAt");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems).HasConstraintName("FK_CartItems_Carts");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Products");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Categories_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Categories_IsActive");
        });

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Discounts_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Discounts_IsActive");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.DeliveryType).HasDefaultValue("DELIVERY", "DF_Orders_DeliveryType");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(sysutcdatetime())", "DF_Orders_OrderDate");
            entity.Property(e => e.Status).HasDefaultValue("PENDING", "DF_Orders_Status");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderCancellation>(entity =>
        {
            entity.Property(e => e.CancellationDate).HasDefaultValueSql("(sysutcdatetime())", "DF_OrderCancellations_Date");
            entity.Property(e => e.RefundStatus).HasDefaultValue("NOT_APPLICABLE", "DF_OrderCancellations_RefundStatus");

            entity.HasOne(d => d.Order).WithOne(p => p.OrderCancellation)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderCancellations_Orders");

            entity.HasOne(d => d.User).WithMany(p => p.OrderCancellations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderCancellations_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_OrderStatusHistory_ChangedAt");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.OrderStatusHistories).HasConstraintName("FK_OrderStatusHistory_Users");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories).HasConstraintName("FK_OrderStatusHistory_Orders");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Payments_CreatedAt");
            entity.Property(e => e.Status).HasDefaultValue("PENDING", "DF_Payments_Status");

            entity.HasOne(d => d.Order).WithOne(p => p.Payment)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Orders");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_PaymentMethods");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_PaymentMethods_IsActive");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Products_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Products_IsActive");
            entity.Property(e => e.IsAvailable).HasDefaultValue(true, "DF_Products_IsAvailable");
            entity.Property(e => e.MinStockThreshold).HasDefaultValue(5, "DF_Products_MinStockThreshold");

            entity.HasOne(d => d.Discount).WithMany(p => p.Products).HasConstraintName("FK_Products_Discounts");

            entity.HasOne(d => d.SubCategory).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_SubCategories");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products).HasConstraintName("FK_Products_Suppliers");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_RefreshTokens_CreatedAt");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Reviews_CreatedAt");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Products");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Users");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue("PREPARING", "DF_Shipments_Status");

            entity.HasOne(d => d.Order).WithOne(p => p.Shipment)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipments_Orders");
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_SubCategories_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_SubCategories_IsActive");

            entity.HasOne(d => d.Category).WithMany(p => p.SubCategories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubCategories_Categories");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Suppliers_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Suppliers_IsActive");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Users_IsActive");
            entity.Property(e => e.RoleId).HasDefaultValue((byte)2, "DF_Users_RoleId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
