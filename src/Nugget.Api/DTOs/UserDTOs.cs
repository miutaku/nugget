namespace Nugget.Api.DTOs;

/// <summary>
/// ユーザーレスポンスDTO
/// </summary>
public record UserResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Department { get; init; }
    public string? Division { get; init; }
}
