using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Table("OrderStatusHistory")]
[Index("OrderId", "ChangedAt", Name = "IX_OrderStatusHistory_OrderId_ChangedAt", IsDescending = new[] { false, true })]
public partial class OrderStatusHistory
{
    [Key]
    public long OrderStatusHistoryId { get; set; }

    public int OrderId { get; set; }

    [StringLength(30)]
    public string? OldStatus { get; set; }

    [StringLength(30)]
    public string NewStatus { get; set; } = null!;

    public int? ChangedByUserId { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }

    [Precision(0)]
    public DateTime ChangedAt { get; set; }

    [ForeignKey("ChangedByUserId")]
    [InverseProperty("OrderStatusHistories")]
    public virtual User? ChangedByUser { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderStatusHistories")]
    public virtual Order Order { get; set; } = null!;
}
