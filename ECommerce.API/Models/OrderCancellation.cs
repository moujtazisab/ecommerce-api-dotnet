using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("OrderId", Name = "UQ_OrderCancellations_Order", IsUnique = true)]
public partial class OrderCancellation
{
    [Key]
    public int CancellationId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    [StringLength(500)]
    public string Reason { get; set; } = null!;

    [Precision(0)]
    public DateTime CancellationDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RefundAmount { get; set; }

    [StringLength(20)]
    public string RefundStatus { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("OrderCancellation")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("OrderCancellations")]
    public virtual User User { get; set; } = null!;
}
