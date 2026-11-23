using System.ComponentModel.DataAnnotations;

namespace Recrutement.ViewModels;

public class ApplyViewModel
{
    [Required(ErrorMessage = "Le nom est requis.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le courriel est requis.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Le titre actuel est requis.")]
    [StringLength(120)]
    public string CurrentTitle { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Le salaire doit être positif.")]
    public decimal ExpectedSalary { get; set; }

    public bool Consent { get; set; }

    public IFormFile? Cv { get; set; }
}