// File: Models/SupplierContact.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TextilePro.Core.Models;

public class SupplierContact
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [Required, MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [NotMapped]
    public string CountryCode { get; set; } = "+91";

    [NotMapped]
    public string LocalPhone { get; set; } = string.Empty;

    public void LoadPhoneParts()
    {
        var value = (Phone ?? string.Empty).Trim();
        if (value.StartsWith('+'))
        {
            var separator = value.IndexOf(' ');
            if (separator > 0)
            {
                CountryCode = value[..separator];
                LocalPhone = value[(separator + 1)..];
                return;
            }
        }
        LocalPhone = value;
    }

    public void StorePhoneParts() => Phone = string.IsNullOrWhiteSpace(LocalPhone)
        ? null
        : $"{CountryCode} {LocalPhone}".Trim();

    [MaxLength(200)]
    public string? Website { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }
}
