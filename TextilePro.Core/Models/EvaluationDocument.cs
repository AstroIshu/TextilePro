// File: Models/EvaluationDocument.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TextilePro.Core.Models;

public class EvaluationDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EvaluationId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; }

    [ForeignKey(nameof(EvaluationId))]
    public virtual Evaluation? Evaluation { get; set; }
}
