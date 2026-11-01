using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("OrderId", Name = "UQ_Shipments_Order", IsUnique = true)]
public partial class Shipment
{
    [Key]
    public int ShipmentId { get; set; }

    public int OrderId { get; set; }

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    [StringLength(100)]
    public string? Carrier { get; set; }

    [Precision(0)]
    public DateTime? ShippingDate { get; set; }

    [Precision(0)]
    public DateTime? DeliveryDate { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("Shipment")]
    public virtual Order Order { get; set; } = null!;
}
