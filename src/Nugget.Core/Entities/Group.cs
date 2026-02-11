namespace Nugget.Core.Entities;

/// <summary>
/// ユーザーグループ（SCIM経由で同期）
/// </summary>
public class Group
{
    public Guid Id { get; set; }

    /// <summary>
    /// 表示名
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// SCIM external ID
    /// </summary>
    public string? ExternalId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}
