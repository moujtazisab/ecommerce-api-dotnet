using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("CategoryId", Name = "IX_SubCategories_CategoryId")]
[Index("CategoryId", "Name", Name = "UQ_SubCategories_Category_Name", IsUnique = true)]
public partial class SubCategory
{
    [Key]
    public int SubCategoryId { get; set; }

    public int CategoryId { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("SubCategories")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("SubCategory")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
