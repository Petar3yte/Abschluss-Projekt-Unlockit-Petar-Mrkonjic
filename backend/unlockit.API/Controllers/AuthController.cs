using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using unlockit.API.DTOs.User;
using unlockit.API.Helpers;
using unlockit.API.Models;
using unlockit.API.Repositories;
using static unlockit.API.Helpers.PasswordHelper;
using unlockit.API.Services;
using unlockit.API.DTOs.Auth;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        //Dependency Injection 
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;

        public AuthController(UserRepository userRepository, TokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            //Benutzer finden
            var user = await _userRepository.GetUserByUsernameAsync(loginRequest.UserName);
            if (user == null)
            {
                return Unauthorized(new { message = "Benutzername oder Passwort falsch." });
            }

            //Passwort überprüfen
            var providedPasswordHash = HashPassword(loginRequest.Password);

            if (user.PasswordHash != providedPasswordHash)
            {
                return Unauthorized(new { message = "Benutzername oder Passwort falsch." });
            }

            //Token generieren (Services/TokenService.cs)
            var tokenString = _tokenService.GenerateToken(user);

            //Antwortpaket          
            var userDto = new UserDto
            {
                UserUUID = user.UserUUID,
                UserName = user.UserName,
                Role = user.Role,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            var response = new LoginResponseDto
            {
                User = userDto,
                Token = tokenString
            };

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            //Verfügbarkeitscheck
            var existingUser = await _userRepository.GetUserByUsernameAsync(createUserDto.UserName);
            if (existingUser != null)
            {
                return Conflict(new { message = "Ein Benutzer mit diesem Benutzernamen existiert bereits." });
            }

            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(createUserDto.Email);
            if (existingUserByEmail != null)
            {
                return Conflict(new { message = "Ein Benutzer mit dieser E-Mail-Adresse existiert bereits." });
            }

            var passwordHash = PasswordHelper.HashPassword(createUserDto.Password);

            //Formular
            var newUser = new User
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                PasswordHash = passwordHash,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Role = UserRole.Kunde 
            };

            //Erstellen und Speichern
            var createdUser = await _userRepository.CreateUserAsync(newUser);
            return StatusCode(201, new { message = "Benutzer erfolgreich registriert. Sie können sich nun anmelden." });
        }
    }
}