using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace EmailApp.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, IHttpContextAccessor httpContextAccessor)
        {
            _jsRuntime = jsRuntime;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var httpUser = _httpContextAccessor.HttpContext?.User;

                if (httpUser?.Identity?.IsAuthenticated == true)
                {
                    return new AuthenticationState(httpUser);
                }

                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

                if (string.IsNullOrEmpty(token))
                {
                    return new AuthenticationState(_anonymous);
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo <= DateTime.UtcNow)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                    return new AuthenticationState(_anonymous);
                }

                return new AuthenticationState(CreatePrincipal(jwtToken));
            }
            catch
            {
                return new AuthenticationState(_anonymous);
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var user = CreatePrincipal(jwtToken);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }

        private static ClaimsPrincipal CreatePrincipal(JwtSecurityToken jwtToken)
        {
            var claims = jwtToken.Claims.ToList();
            var role = claims.FirstOrDefault(claim => claim.Type is ClaimTypes.Role or "role")?.Value;
            var name = claims.FirstOrDefault(claim => claim.Type is ClaimTypes.Name or "name" or "unique_name")?.Value;

            if (!string.IsNullOrWhiteSpace(role) && claims.All(claim => claim.Type != ClaimTypes.Role))
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (!string.IsNullOrWhiteSpace(name) && claims.All(claim => claim.Type != ClaimTypes.Name))
                claims.Add(new Claim(ClaimTypes.Name, name));

            var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
    }
}
