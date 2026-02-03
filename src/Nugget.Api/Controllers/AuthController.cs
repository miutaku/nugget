using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nugget.Core.Interfaces;
using System.Security.Claims;

namespace Nugget.Api.Controllers;

/// <summary>
/// 認証関連のエンドポイント
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// 現在のログインユーザー情報を取得
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(userGuid, cancellationToken);
        
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        });
    }

    /// <summary>
    /// SAMLログイン開始
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        // SAML認証にチャレンジ
        return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = returnUrl
        }, "Saml2");
    }

    /// <summary>
    /// ログアウト
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Cookie認証からサインアウト
        return SignOut(
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = "/"
            },
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 認証状態チェック（軽量）
    /// </summary>
    [HttpGet("check")]
    public IActionResult CheckAuth()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new { IsAuthenticated = true });
        }
        
        return Ok(new { IsAuthenticated = false });
    }
}
