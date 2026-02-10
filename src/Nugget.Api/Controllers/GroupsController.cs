using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nugget.Api.DTOs;
using Nugget.Core.Interfaces;

namespace Nugget.Api.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;

    public GroupsController(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    /// <summary>
    /// 全グループを取得
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.GetAllAsync(cancellationToken);
        
        // シンプルなリストを返す
        var response = groups.Select(g => new
        {
            g.Id,
            g.DisplayName,
            MemberCount = g.UserGroups.Count
        });

        return Ok(response);
    }

    /// <summary>
    /// グループに所属するユーザーを取得
    /// </summary>
    [HttpGet("{id:guid}/users")]
    public async Task<IActionResult> GetGroupMembers(Guid id, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        
        if (group == null)
        {
            return NotFound();
        }

        var users = group.UserGroups
            .Select(ug => ug.User)
            .Where(u => u.IsActive)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Department = u.Department,
                Division = u.Division
            });

        return Ok(users);
    }
}
