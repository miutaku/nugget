using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Nugget.Api.Models.Scim;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;

namespace Nugget.Api.Controllers;

[ApiController]
[Route("api/scim/v2")]
public class ScimController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScimController> _logger;

    public ScimController(
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IConfiguration configuration,
        ILogger<ScimController> logger)
    {
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _configuration = configuration;
        _logger = logger;
    }

    private bool IsAuthorized()
    {
        var token = _configuration["Scim:ApiToken"];
        if (string.IsNullOrEmpty(token)) return false; // Token not configured

        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return false;
        }

        var incomingToken = authHeader.Substring("Bearer ".Length).Trim();
        return incomingToken == token;
    }

    private IActionResult UnauthorizedScim()
    {
        return Unauthorized(new ScimError { Status = "401", Detail = "Unauthorized" });
    }

    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? filter,
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        // Check for specific filter (e.g. userName eq "...")
        // MVP: ignoring complex filters, just returning all or simple match
        IEnumerable<User> users;
        
        if (!string.IsNullOrEmpty(filter) && filter.Contains("userName eq"))
        {
            // Simple extraction of userName
            var parts = filter.Split(new[] { "eq" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var email = parts[1].Trim().Trim('"');
                var user = await _userRepository.GetByEmailAsync(email);
                users = user != null ? new[] { user } : Array.Empty<User>();
            }
            else
            {
                users = await _userRepository.GetAllActiveUsersAsync();
            }
        }
        else if (!string.IsNullOrEmpty(filter) && filter.Contains("externalId eq"))
        {
             var parts = filter.Split(new[] { "eq" }, StringSplitOptions.RemoveEmptyEntries);
             if (parts.Length > 1)
             {
                 var extId = parts[1].Trim().Trim('"');
                 var user = await _userRepository.GetByExternalIdAsync(extId);
                 users = user != null ? new[] { user } : Array.Empty<User>();
             }
             else
             {
                 users = await _userRepository.GetAllActiveUsersAsync();
             }
        }
        else
        {
            users = await _userRepository.GetAllActiveUsersAsync();
        }

        var total = users.Count();
        var pagedUsers = users.Skip(startIndex - 1).Take(count).ToList();

        var response = new ScimListResponse<ScimUser>
        {
            TotalResults = total,
            ItemsPerPage = pagedUsers.Count,
            StartIndex = startIndex,
            Resources = pagedUsers.Select(MapToScimUser).ToList()
        };

        return Ok(response);
    }

    [HttpGet("Users/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        if (!Guid.TryParse(id, out var guid))
            return NotFound(new ScimError { Status = "404", Detail = "Resource not found" });

        var user = await _userRepository.GetByIdAsync(guid);
        if (user == null)
            return NotFound(new ScimError { Status = "404", Detail = "Resource not found" });

        return Ok(MapToScimUser(user));
    }

    [HttpPost("Users")]
    public async Task<IActionResult> CreateUser([FromBody] ScimUser scimUser)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        var existingUser = await _userRepository.GetByEmailAsync(scimUser.UserName);
        if (existingUser != null)
        {
            return Conflict(new ScimError { Status = "409", Detail = "User already exists" });
        }

        var user = new User
        {
            Email = scimUser.UserName,
            Name = scimUser.DisplayName,
            ExternalId = scimUser.ExternalId,
            IsActive = scimUser.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        return Created($"api/scim/v2/Users/{user.Id}", MapToScimUser(user));
    }

    [HttpPut("Users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] ScimUser scimUser)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        if (!Guid.TryParse(id, out var guid))
            return NotFound(new ScimError { Status = "404", Detail = "Resource not found" });

        var user = await _userRepository.GetByIdAsync(guid);
        if (user == null)
            return NotFound(new ScimError { Status = "404", Detail = "Resource not found" });

        user.Name = scimUser.DisplayName;
        user.ExternalId = scimUser.ExternalId;
        user.IsActive = scimUser.Active;
        // Email update might be sensitive, checking if changed
        if (user.Email != scimUser.UserName)
        {
            var existing = await _userRepository.GetByEmailAsync(scimUser.UserName);
            if (existing != null && existing.Id != user.Id)
                return Conflict(new ScimError { Status = "409", Detail = "Email already taken" });
            user.Email = scimUser.UserName;
        }

        await _userRepository.UpdateAsync(user);

        return Ok(MapToScimUser(user));
    }
    
    // Group endpoints (Minimal implementation)
    [HttpGet("Groups")]
    public async Task<IActionResult> GetGroups(
        [FromQuery] string? filter,
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        // Ignorning filter for MVP usually needed for groups? Okta might filter by displayName
        var groups = await _groupRepository.GetAllAsync();
        
        var total = groups.Count;
        var pagedGroups = groups.Skip(startIndex - 1).Take(count).ToList();

        var response = new ScimListResponse<ScimGroup>
        {
            TotalResults = total,
            ItemsPerPage = pagedGroups.Count,
            StartIndex = startIndex,
            Resources = pagedGroups.Select(MapToScimGroup).ToList()
        };

        return Ok(response);
    }
    
    [HttpPost("Groups")]
    public async Task<IActionResult> CreateGroup([FromBody] ScimGroup scimGroup)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        var group = new Group
        {
            DisplayName = scimGroup.DisplayName,
            ExternalId = scimGroup.Id, // Sometimes Id in payload is externalId
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _groupRepository.AddAsync(group);
        
        if (scimGroup.Members != null && scimGroup.Members.Any())
        {
             var userIds = new List<Guid>();
             foreach(var member in scimGroup.Members)
             {
                 if (Guid.TryParse(member.Value, out var uid))
                 {
                     userIds.Add(uid);
                 }
             }
             await _groupRepository.UpdateMembersAsync(group, userIds);
             
             // Refresh to get members
             var refreshedGroup = await _groupRepository.GetByIdAsync(group.Id);
             if (refreshedGroup != null) group = refreshedGroup;
        }

        return Created($"api/scim/v2/Groups/{group.Id}", MapToScimGroup(group));
    }

    [HttpPut("Groups/{id}")]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] ScimGroup scimGroup)
    {
        if (!IsAuthorized()) return UnauthorizedScim();

        if (!Guid.TryParse(id, out var guid))
            return NotFound(new ScimError { Status = "404", Detail = "Resource not found" });

        var group = await _groupRepository.GetByIdAsync(guid);
        if (group == null)
            return NotFound(new ScimError { Status = "404", Detail = "Resource not found" });

        group.DisplayName = scimGroup.DisplayName;
        await _groupRepository.UpdateAsync(group);

        // Update members
        if (scimGroup.Members != null)
        {
             var userIds = new List<Guid>();
             foreach(var member in scimGroup.Members)
             {
                 if (Guid.TryParse(member.Value, out var uid))
                 {
                     userIds.Add(uid);
                 }
             }
             await _groupRepository.UpdateMembersAsync(group, userIds);
        }
        
        var updatedGroup = await _groupRepository.GetByIdAsync(group.Id);

        return Ok(MapToScimGroup(updatedGroup!));
    }

    private ScimUser MapToScimUser(User user)
    {
        return new ScimUser
        {
            Id = user.Id.ToString(),
            ExternalId = user.ExternalId,
            UserName = user.Email,
            DisplayName = user.Name,
            Active = user.IsActive,
            Emails = new List<ScimEmail> { new ScimEmail { Value = user.Email } },
            Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = user.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                LastModified = user.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Location = $"/api/scim/v2/Users/{user.Id}"
            }
        };
    }

    private ScimGroup MapToScimGroup(Group group)
    {
        return new ScimGroup
        {
            Id = group.Id.ToString(),
            DisplayName = group.DisplayName,
            Members = group.UserGroups.Select(ug => new ScimMember
            {
                Value = ug.UserId.ToString(),
                Display = ug.User.Name
            }).ToList(),
            Meta = new ScimMeta
            {
                ResourceType = "Group",
                Created = group.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                LastModified = group.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Location = $"/api/scim/v2/Groups/{group.Id}"
            }
        };
    }
}
