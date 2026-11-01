using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("Status", "OrderDate", Name = "IX_Orders_Status_OrderDate", IsDescending = new[] { false, true })]
[Index("UserId", "OrderDate", Name = "IX_Orders_UserId_OrderDate", IsDescending = new[] { false, true })]
[Index("OrderNumber", Name = "UQ_Orders_OrderNumber", IsUnique = true)]
public partial class Order
{
    [Key]
    public int OrderId { get; set; }

    public int UserId { get; set; }

    [StringLength(50)]
    public string OrderNumber { get; set; } = null!;

    [Precision(0)]
    public DateTime OrderDate { get; set; }

    public int TotalQuantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ShippingAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(20)]
    public string DeliveryType { get; set; } = null!;

    [StringLength(500)]
    public string? ShippingAddress { get; set; }

    [StringLength(100)]
    public string? ShippingCity { get; set; }

    [StringLength(20)]
    public string? ShippingPostalCode { get; set; }

    [StringLength(100)]
    public string? ShippingCountry { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [StringLength(500)]
    public string? Notes { get; set; }

    [InverseProperty("Order")]
    public virtual OrderCancellation? OrderCancellation { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Order")]
    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    [InverseProperty("Order")]
    public virtual Payment? Payment { get; set; }

    [InverseProperty("Order")]
    public virtual Shipment? Shipment { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Orders")]
    public virtual User User { get; set; } = null!;
}
