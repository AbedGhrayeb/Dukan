using System.ComponentModel.DataAnnotations;

namespace Dukan.Web.Application.DTOs;

public sealed class SubscriptionRequestForm
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [StringLength(150, ErrorMessage = "الاسم الكامل يجب ألا يتجاوز 150 حرفاً")]
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المتجر مطلوب")]
    [StringLength(150, ErrorMessage = "اسم المتجر يجب ألا يتجاوز 150 حرفاً")]
    [Display(Name = "اسم المتجر")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "أدخل رقم هاتف صحيحاً")]
    [StringLength(30, ErrorMessage = "رقم الهاتف يجب ألا يتجاوز 30 حرفاً")]
    [Display(Name = "رقم الهاتف")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الواتساب مطلوب")]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "أدخل رقم واتساب صحيحاً")]
    [StringLength(30, ErrorMessage = "رقم الواتساب يجب ألا يتجاوز 30 حرفاً")]
    [Display(Name = "رقم الواتساب")]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "اختر الخطة المطلوبة")]
    [Display(Name = "الخطة المطلوبة")]
    public Guid? PlanId { get; set; }

    [StringLength(1000, ErrorMessage = "الملاحظات يجب ألا تتجاوز 1000 حرف")]
    [Display(Name = "ملاحظات")]
    public string? Notes { get; set; }
}
