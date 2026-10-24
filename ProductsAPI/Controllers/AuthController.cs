
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProductsApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProductsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static readonly List<User> _users = new();
        private static int _nextId = 1;
        private static readonly PasswordHasher<User> _hasher = new();

        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        public record AuthRequest(string Username, string Password);

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(AuthRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Username and password are required.");
            }

            if (_users.Any(u => string.Equals(
                u.Username, req.Username,
                StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("Username already taken.");
            }

            var user = new User
            {
                Id = _nextId++,
                Username = req.Username,
                Role = string.Equals(
                    req.Username, "admin",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Admin"
                    : "User"
            };

            user.PasswordHash =
                _hasher.HashPassword(user, req.Password);

            _users.Add(user);

            return Created("/api/auth/register", new
            {
                user.Id,
                user.Username,
                user.Role
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login(AuthRequest req)
        {
            var user = _users.FirstOrDefault(u =>
                string.Equals(u.Username, req.Username,
                    StringComparison.OrdinalIgnoreCase));

            if (user == null ||
                _hasher.VerifyHashedPassword(
                    user, user.PasswordHash, req.Password)
                == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid username or password.");
            }

            return Ok(new
            {
                token = CreateToken(user),
                username = user.Username,
                role = user.Role
            });
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                signingKey, SecurityAlgorithms.HmacSha256);

            var expiry = _config.GetValue<int>("Jwt:ExpiryMinutes", 60);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
