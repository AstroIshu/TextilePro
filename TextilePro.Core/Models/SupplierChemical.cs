// File: Models/SupplierChemical.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace TextilePro.Core.Models;

public class SupplierChemical
{
    public int SupplierId { get; set; }
    public int ChemicalId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(ChemicalId))]
    public virtual ZDHCChemical? Chemical { get; set; }
}