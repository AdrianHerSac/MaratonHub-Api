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

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _userRepository.CreateUserAsync(user);

            var token = GenerateJwtToken(user.Id!, user.Username);
            return Ok(new AuthResponseDto { Token = token, Username = user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(dto.Username);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return Unauthorized("Credenciales inválidas.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Credenciales inválidas.");

            var token = GenerateJwtToken(user.Id!, user.Username);
            return Ok(new AuthResponseDto { Token = token, Username = user.Username });
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

                    user = new User
                    {
                        GoogleId = payload.Subject,
                        Username = username,
                    };
                    await _userRepository.CreateUserAsync(user);
                }

                var token = GenerateJwtToken(user.Id!, user.Username);
                return Ok(new AuthResponseDto { Token = token, Username = user.Username });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized("Token de Google inválido.");
            }
        }

        private string GenerateJwtToken(string userId, string username)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
