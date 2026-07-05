// File: Models/ZDHCChemical.cs
using System.ComponentModel.DataAnnotations;

namespace TextilePro.Core.Models;

public class ZDHCChemical
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(10)]
    public string Serial { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ChemicalName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string CAS { get; set; } = string.Empty;

    public string? UsageDescription { get; set; }

    [Required, MaxLength(50)]
    public string RiskCategory { get; set; } = string.Empty; // "Virgin", "Non-Virgin", "Virgin and Non-Virgin"

    // Navigation
    public virtual ICollection<SupplierChemical> SupplierChemicals { get; set; } = new List<SupplierChemical>();
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}