// File: Models/Evaluation.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TextilePro.Core.Models;

public class Evaluation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [Required]
    public int Score { get; set; } // 0-24

    [Required]
    public DateTime EvaluationDate { get; set; }

    [Required, MaxLength(20)]
    public string SelfDeclarationStatus { get; set; } = "Not Available";

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<EvaluationAnswer> Answers { get; set; } = new List<EvaluationAnswer>();
    public virtual ICollection<EvaluationDocument> Documents { get; set; } = new List<EvaluationDocument>();
}