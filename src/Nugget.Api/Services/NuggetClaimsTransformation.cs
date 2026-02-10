using Microsoft.AspNetCore.Authentication;
using Nugget.Core.Interfaces;
using System.Security.Claims;

namespace Nugget.Api.Services;

/// <summary>
/// 認証後のクレーム変換（SAMLメールアドレスから内部ID/ロールへのマッピング）
/// </summary>
public class NuggetClaimsTransformation : IClaimsTransformation
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<NuggetClaimsTransformation> _logger;

    public NuggetClaimsTransformation(
        IUserRepository userRepository,
        ILogger<NuggetClaimsTransformation> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // 未認証の場合は何もしない
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // すでに変換済みの場合は重複実行を避ける
        if (principal.HasClaim(c => c.Type == "nugget_transformed"))
        {
            return principal;
        }

        var nameId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nameId))
        {
            return principal;
        }

        // すでに Guid 形式の場合は、内部 ID がセットされているとみなす
        if (Guid.TryParse(nameId, out _))
        {
            return principal;
        }

        // SAML 等から渡された NameID (メールアドレス) を元に DB からユーザーを検索
        _logger.LogInformation("Attempting to map SAML identity {NameId} to local user", nameId);
        var user = await _userRepository.GetByEmailAsync(nameId);

        if (user != null)
        {
            var identity = (ClaimsIdentity)principal.Identity;
            
            // 内部 Guid を NameIdentifier として追加
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            
            // ロール情報を決定
            var finalRole = user.Role;
            
            // 環境変数による管理者制限チェック
            var adminEmailsRaw = Environment.GetEnvironmentVariable("SAML_ADMIN_EMAILS");
            if (!string.IsNullOrEmpty(adminEmailsRaw))
            {
                var adminEmails = adminEmailsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var isAuthorizedAdmin = adminEmails.Any(e => e.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
                
                if (user.Role == Nugget.Core.Enums.UserRole.Admin && !isAuthorizedAdmin)
                {
                    _logger.LogWarning("User {Email} has Admin role in DB but is not in SAML_ADMIN_EMAILS. Downgrading to User role.", user.Email);
                    finalRole = Nugget.Core.Enums.UserRole.User;
                }
                else if (user.Role != Nugget.Core.Enums.UserRole.Admin && isAuthorizedAdmin)
                {
                    _logger.LogInformation("User {Email} is in SAML_ADMIN_EMAILS. Upgrading to Admin role.", user.Email);
                    finalRole = Nugget.Core.Enums.UserRole.Admin;
                }
            }
            
            identity.AddClaim(new Claim(ClaimTypes.Role, finalRole.ToString()));
            
            // 変換済みフラグを付与
            identity.AddClaim(new Claim("nugget_transformed", "true"));
            
            _logger.LogInformation("Successfully mapped user {Email} to internal ID {Id} with role {Role}", 
                user.Email, user.Id, finalRole);
        }
        else
        {
            _logger.LogWarning("User with email {Email} not found in database. SSO successful but local identity mapping failed.", nameId);
        }

        return principal;
    }
}
