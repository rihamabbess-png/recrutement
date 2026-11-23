using System.ComponentModel.DataAnnotations.Schema;

namespace Recrutement.Models;

public class JobApplication
{
    public int Id { get; set; }

    public int JobOfferId { get; set; }
    public JobOffer? JobOffer { get; set; }

    // Informations du candidat
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string CurrentTitle { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedSalary { get; set; }

    public string CvFilePath { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.EnAnalyse;

    // Résultats de l'évaluation IA
    public DateTime? AnalyzedAt { get; set; }
    public int? AiScore { get; set; }
    public bool? AiBudgetFit { get; set; }
    public string? AiCriteriaMet { get; set; }      // un par ligne
    public string? AiCriteriaMissing { get; set; }  // un par ligne
    public string? AiSummary { get; set; }
    public string? AiError { get; set; }

    // Suivi RH
    public string? HrNotes { get; set; }
    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
}