using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


[ApiController]
[Route("api/[controller]")]
public class AuthController : Controller
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db)
    {
        _db = db;
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(User user)
    {
        User? checkUser = _db.Users.FirstOrDefault(u => u.Username == user.Username);

        if (checkUser != null && BCrypt.Net.BCrypt.Verify(user.Password, checkUser.Password))
        {
            List<Claim>? claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, checkUser.Username),
                new Claim(ClaimTypes.Role, checkUser.IsAdmin ? "Admin" : "User")
            };
    
            ClaimsIdentity? identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal? principal = new ClaimsPrincipal(identity);
    
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
            return Ok();
        }
        else
        {
            return Unauthorized();   
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}