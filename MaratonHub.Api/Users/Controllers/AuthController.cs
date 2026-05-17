using Google.Apis.Auth;
using MaratonHub.Api.Users.Dtos;
using MaratonHub.Api.Users.Models;
using MaratonHub.Api.Users.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MaratonHub.Api.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;

        public AuthController(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("El nombre de usuario y contraseña son obligatorios.");

            var existingUser = await _userRepository.GetUserByUsernameAsync(dto.Username);
            if (existingUser != null)
                return BadRequest("El nombre de usuario ya está en uso.");

            var adminUsername = _config["AdminSettings:AdminUsername"]?.Trim();
            var role = (dto.Username.Trim().ToLower() == adminUsername?.ToLower()) ? UserRoles.Admin : UserRoles.User;

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role
            };

            await _userRepository.CreateUserAsync(user);

            var token = GenerateJwtToken(user);
            return Ok(new AuthResponseDto { Token = token, Username = user.Username, Role = user.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(dto.Username);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return Unauthorized("Credenciales inválidas.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Credenciales inválidas.");

            if (user.Username.Trim().Equals("Adrian", StringComparison.OrdinalIgnoreCase))
            {
                user.Role = UserRoles.Admin;
            }

            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            var token = GenerateJwtToken(user);
            return Ok(new AuthResponseDto { Token = token, Username = user.Username, Role = user.Role });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { _config["GoogleAuth:ClientId"]! }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

                var user = await _userRepository.GetUserByGoogleIdAsync(payload.Subject);

                if (user == null)
                {
                    var baseUsername = payload.GivenName ?? payload.Email.Split('@')[0];
                    var username = baseUsername;
                    int suffix = 1;

                    while (await _userRepository.GetUserByUsernameAsync(username) != null)
                    {
                        username = $"{baseUsername}{suffix}";
                        suffix++;
                    }

                    var adminUsername = _config["AdminSettings:AdminUsername"];
                    var role = (username.ToLower() == adminUsername?.ToLower()) ? UserRoles.Admin : UserRoles.User;

                    user = new User
                    {
                        GoogleId = payload.Subject,
                        Username = username,
                        Role = role
                    };
                    await _userRepository.CreateUserAsync(user);
                }

                if (user.Username.Trim().Equals("Adrian", StringComparison.OrdinalIgnoreCase))
                {
                    user.Role = UserRoles.Admin;
                }

                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(user);

                var token = GenerateJwtToken(user);
                return Ok(new AuthResponseDto { Token = token, Username = user.Username, Role = user.Role });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized("Token de Google inválido.");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id!),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
