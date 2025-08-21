using System.ComponentModel.DataAnnotations;

namespace InterviewTrainer.Api.Models;

public class Question
{
    public int Id { get; set; }

    [Required, MaxLength(3000)]
    public string Text { get; set; } = null!;

    [Required, MaxLength(5000)]
    public string Answer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}