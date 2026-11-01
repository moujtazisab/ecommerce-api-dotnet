using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Models;

[Index("UserId", "ExpiresAt", Name = "IX_RefreshTokens_UserId_ExpiresAt")]
[Index("TokenHash", Name = "UQ_RefreshTokens_TokenHash", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    public long RefreshTokenId { get; set; }

    public int UserId { get; set; }

    [StringLength(500)]
    public string TokenHash { get; set; } = null!;

    [Precision(0)]
    public DateTime ExpiresAt { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? RevokedAt { get; set; }

    [StringLength(500)]
    public string? ReplacedByTokenHash { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User User { get; set; } = null!;
}
