using System.ComponentModel.DataAnnotations;

namespace Dukan.Web.Application.DTOs;

public sealed class FirebaseConfigForm
{
    [Required(ErrorMessage = "محتوى ملف JSON مطلوب")]
    [Display(Name = "محتوى ملف Service Account JSON")]
    public string CredentialJson { get; set; } = string.Empty;
}
