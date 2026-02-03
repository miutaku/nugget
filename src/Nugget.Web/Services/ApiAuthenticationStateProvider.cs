using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Nugget.Web.Models;

namespace Nugget.Web.Services;

/// <summary>
/// SAML認証に基づくカスタム認証状態プロバイダー
/// </summary>
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private AuthState? _cachedAuthState;

    public ApiAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // キャッシュがあればそれを使用
            if (_cachedAuthState != null)
            {
                return CreateAuthenticationState(_cachedAuthState);
            }

            // APIから認証状態を取得
            var response = await _httpClient.GetAsync("api/auth/me");
            
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
                if (user != null)
                {
                    _cachedAuthState = new AuthState 
                    { 
                        IsAuthenticated = true, 
                        User = user 
                    };
                    return CreateAuthenticationState(_cachedAuthState);
                }
            }
        }
        catch
        {
            // APIエラー時は未認証として扱う
        }

        _cachedAuthState = new AuthState { IsAuthenticated = false };
        return CreateAuthenticationState(_cachedAuthState);
    }

    /// <summary>
    /// 認証状態をクリア（ログアウト時）
    /// </summary>
    public void ClearAuthState()
    {
        _cachedAuthState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// 認証状態を更新
    /// </summary>
    public void RefreshAuthState()
    {
        _cachedAuthState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState CreateAuthenticationState(AuthState authState)
    {
        if (!authState.IsAuthenticated || authState.User == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authState.User.Id.ToString()),
            new(ClaimTypes.Email, authState.User.Email),
            new(ClaimTypes.Name, authState.User.Name),
            new(ClaimTypes.Role, authState.User.Role)
        };

        var identity = new ClaimsIdentity(claims, "SAML");
        var principal = new ClaimsPrincipal(identity);
        
        return new AuthenticationState(principal);
    }
}
