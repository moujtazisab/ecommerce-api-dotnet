using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("ProductId", "IsApproved", Name = "IX_Reviews_ProductId_IsApproved")]
[Index("UserId", "ProductId", Name = "UQ_Reviews_User_Product", IsUnique = true)]
public partial class Review
{
    [Key]
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public byte Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public bool IsApproved { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Reviews")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Reviews")]
    public virtual User User { get; set; } = null!;
}
