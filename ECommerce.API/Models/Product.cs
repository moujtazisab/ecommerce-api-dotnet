using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("IsActive", "IsAvailable", "Price", Name = "IX_Products_Catalog")]
[Index("DiscountId", Name = "IX_Products_DiscountId")]
[Index("SubCategoryId", Name = "IX_Products_SubCategoryId")]
[Index("SupplierId", Name = "IX_Products_SupplierId")]
[Index("Sku", Name = "UQ_Products_SKU", IsUnique = true)]
public partial class Product
{
    [Key]
    public int ProductId { get; set; }

    public int SubCategoryId { get; set; }

    public int? SupplierId { get; set; }

    public int? DiscountId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [Column("SKU")]
    [StringLength(100)]
    public string Sku { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? OriginalPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }

    public int MinStockThreshold { get; set; }

    public bool IsActive { get; set; }

    public bool IsAvailable { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ForeignKey("DiscountId")]
    [InverseProperty("Products")]
    public virtual Discount? Discount { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Product")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [ForeignKey("SubCategoryId")]
    [InverseProperty("Products")]
    public virtual SubCategory SubCategory { get; set; } = null!;

    [ForeignKey("SupplierId")]
    [InverseProperty("Products")]
    public virtual Supplier? Supplier { get; set; }
}
