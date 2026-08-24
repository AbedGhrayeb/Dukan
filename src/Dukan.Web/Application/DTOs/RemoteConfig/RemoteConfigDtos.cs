using System.ComponentModel.DataAnnotations;

namespace Dukan.Web.Application.DTOs.RemoteConfig;

public sealed record RemoteConfigParameterDto(
    string Key,
    string Value,
    string ValueType,
    string? Description,
    DateTime? UpdateTime);

public sealed record RemoteConfigTemplateDto(
    IReadOnlyList<RemoteConfigParameterDto> Parameters,
    string? ETag,
    long VersionNumber,
    DateTime? UpdateTime,
    bool HasUnpublishedChanges);

public sealed record RemoteConfigVersionDto(
    long VersionNumber,
    DateTime UpdateTime,
    string? UpdateUser,
    string? Description);

public sealed class RemoteConfigUpsertForm
{
    [Required(ErrorMessage = "المفتاح مطلوب")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_]*$", ErrorMessage = "المفتاح يجب أن يبدأ بحرف ويحتوي فقط على حروف وأرقام و _")]
    [StringLength(256, ErrorMessage = "المفتاح طويل جداً (الحد 256)")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "القيمة مطلوبة")]
    public string Value { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "الوصف طويل جداً")]
    public string? Description { get; set; }

    [Required]
    public string ValueType { get; set; } = "string"; // string | boolean | number | json
}
