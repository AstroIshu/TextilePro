// File: Models/EvaluationAnswer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TextilePro.Core.Models;

public class EvaluationAnswer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EvaluationId { get; set; }

    [Required]
    public int QuestionId { get; set; } // 1-12

    [Required]
    public int SelectedScore { get; set; } // 0,1,2

    [ForeignKey(nameof(EvaluationId))]
    public virtual Evaluation? Evaluation { get; set; }
}