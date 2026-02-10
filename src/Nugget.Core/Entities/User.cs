using Nugget.Core.Enums;

namespace Nugget.Core.Entities;

/// <summary>
/// ユーザーエンティティ（SCIM経由で同期）
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// メールアドレス（ユニーク）
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 表示名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slack ユーザーID（DM送信用）
    /// </summary>
    public string? SlackUserId { get; set; }

    /// <summary>
    /// ユーザーロール
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// SAML NameID（IdP識別子）
    /// </summary>
    public string? SamlNameId { get; set; }

    /// <summary>
    /// SCIM external ID
    /// </summary>
    public string? ExternalId { get; set; }

    // SCIM Enterprise User Schema attributes
    /// <summary>
    /// 部署名 (SCIM: department)
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// 事業部 (SCIM: division)
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// 役職 (SCIM: title)
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// 社員番号 (SCIM: employeeNumber)
    /// </summary>
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// コストセンター (SCIM: costCenter)
    /// </summary>
    public string? CostCenter { get; set; }

    /// <summary>
    /// 組織名 (SCIM: organization)
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// アクティブフラグ
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public NotificationSetting? NotificationSetting { get; set; }
    public ICollection<TodoAssignment> Assignments { get; set; } = new List<TodoAssignment>();
    public ICollection<Todo> CreatedTodos { get; set; } = new List<Todo>();
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}
