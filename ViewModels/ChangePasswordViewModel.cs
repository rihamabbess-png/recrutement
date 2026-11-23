using System.ComponentModel.DataAnnotations;

namespace Recrutement.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Le mot de passe actuel est requis.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nouveau mot de passe est requis.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Minimum 8 caractères.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmez le mot de passe.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Les mots de passe ne correspondent pas.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}