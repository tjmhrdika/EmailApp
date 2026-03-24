using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;


[ApiController]
[Route("api/[controller]")]
public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }
    
    [HttpPost("login")]
    public IActionResult Login(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = jwt });
    }
    // public async Task<IActionResult> Login(User user)
    // {
    //     User? checkUser = _db.Users.FirstOrDefault(u => u.Username == user.Username);

    //     if (checkUser != null && BCrypt.Net.BCrypt.Verify(user.Password, checkUser.Password))
    //     {
    //         List<Claim>? claims = new List<Claim>
    //         {
    //             new Claim(ClaimTypes.Name, checkUser.Username),
    //             new Claim(ClaimTypes.Role, checkUser.IsAdmin ? "Admin" : "User")
    //         };
    
    //         ClaimsIdentity? identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    //         ClaimsPrincipal? principal = new ClaimsPrincipal(identity);
    
    //         await HttpContext.SignInAsync(
    //             CookieAuthenticationDefaults.AuthenticationScheme,
    //             principal);
    //         return Ok();
    //     }
    //     else
    //     {
    //         return Unauthorized();   
    //     }
    // }

    // [HttpPost("logout")]
    // public async Task<IActionResult> Logout()
    // {
    //     await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    //     return Ok();
    // }
}