// File: Models/Supplier.cs
using System.ComponentModel.DataAnnotations;

namespace TextilePro.Core.Models;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Country { get; set; }
    public string? Address { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    // Navigation properties
    public virtual ICollection<SupplierContact> Contacts { get; set; } = new List<SupplierContact>();
    public virtual ICollection<SupplierChemical> SupplierChemicals { get; set; } = new List<SupplierChemical>();
    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}