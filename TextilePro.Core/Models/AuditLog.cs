// File: Models/AuditLog.cs
using System.ComponentModel.DataAnnotations;

namespace TextilePro.Core.Models;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? TableAffected { get; set; }
}