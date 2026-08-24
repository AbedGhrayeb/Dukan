using System.ComponentModel.DataAnnotations;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Application.DTOs;

public sealed class PlanForm
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "اسم الخطة مطلوب")]
    [StringLength(150, ErrorMessage = "اسم الخطة يجب ألا يتجاوز 150 حرفاً")]
    [Display(Name = "اسم الخطة")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 3650, ErrorMessage = "المدة يجب أن تكون أكبر من صفر")]
    [Display(Name = "المدة")]
    public int Duration { get; set; }

    [Required(ErrorMessage = "وحدة المدة مطلوبة")]
    [EnumDataType(typeof(DurationUnit), ErrorMessage = "وحدة المدة غير صالحة")]
    [Display(Name = "وحدة المدة")]
    public DurationUnit DurationUnit { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "السعر غير صالح")]
    [Display(Name = "السعر")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "العملة مطلوبة")]
    [StringLength(10, ErrorMessage = "العملة يجب ألا تتجاوز 10 أحرف")]
    [Display(Name = "العملة")]
    public string Currency { get; set; } = "ILS";

    [Display(Name = "خطة تجريبية مجانية")]
    public bool IsTrial { get; set; }

    [Display(Name = "نشطة")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "ترتيب العرض")]
    [Range(1, 10, ErrorMessage = "ترتيب العرض يجب أن يكون بين 1 و 10")]
    public int DisplayOrder { get; set; }

    [StringLength(1000, ErrorMessage = "الوصف يجب ألا يتجاوز 1000 حرف")]
    [Display(Name = "الوصف")]
    public string? Description { get; set; }

    public static PlanForm FromEntity(Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Duration = plan.Duration,
        DurationUnit = plan.DurationUnit,
        Price = plan.Price,
        Currency = plan.Currency,
        IsTrial = plan.IsTrial,
        IsActive = plan.IsActive,
        DisplayOrder = plan.DisplayOrder,
        Description = plan.Description,
    };

    public void ApplyTo(Plan plan)
    {
        plan.Name = Name;
        plan.Duration = Duration;
        plan.DurationUnit = DurationUnit;
        plan.Price = Price;
        plan.Currency = Currency;
        plan.IsTrial = IsTrial;
        plan.IsActive = IsActive;
        plan.DisplayOrder = DisplayOrder;
        plan.Description = Description ?? string.Empty;
    }
}
