// File: Models/Inventory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TextilePro.Core.Models;

public class Inventory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [Required]
    public int ChemicalId { get; set; }

    [Required]
    public decimal Volume { get; set; }

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty; // "Virgin", "Non-Virgin", etc.

    [Required, MaxLength(10)]
    public string PurchaseMonth { get; set; } = string.Empty; // "MMM-YYYY"

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(ChemicalId))]
    public virtual ZDHCChemical? Chemical { get; set; }
}