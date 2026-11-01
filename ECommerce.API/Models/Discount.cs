using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

public partial class Discount
{
    [Key]
    public int DiscountId { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(20)]
    public string DiscountType { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountValue { get; set; }

    [Precision(0)]
    public DateTime StartDate { get; set; }

    [Precision(0)]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Discount")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
