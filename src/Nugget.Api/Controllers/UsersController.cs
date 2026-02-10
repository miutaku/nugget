using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nugget.Api.DTOs;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;

namespace Nugget.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// ユーザーを検索（属性または全件）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? attributeKey,
        [FromQuery] string? attributeValue,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<User> users;

        if (!string.IsNullOrEmpty(q))
        {
            users = await _userRepository.SearchUsersAsync(q, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(attributeKey) && !string.IsNullOrEmpty(attributeValue))
        {
            users = await _userRepository.GetUsersByAttributeAsync(attributeKey, attributeValue, cancellationToken);
        }
        else
        {
            users = await _userRepository.GetAllActiveUsersAsync(cancellationToken);
        }

        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Department = u.Department,
            Division = u.Division
        });

        return Ok(response);
    }
}
