using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Recrutement.Models;

public class JobOffer
{
    [Key]
    public int Id { get; set; }

    // Jeton utilisé dans le lien public, pour ne pas exposer l'Id
    public Guid PublicToken { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Le titre est requis.")]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La description est requise.")]
    public string Description { get; set; } = string.Empty;

    // Un critère par ligne
    [Required(ErrorMessage = "Les critères sont requis.")]
    public string Requirements { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BudgetMin { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BudgetMax { get; set; }

    public bool IsOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosesAt { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}