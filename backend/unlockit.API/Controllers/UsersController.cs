using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Claims;
using unlockit.API.DTOs.Address;
using unlockit.API.DTOs.User;
using unlockit.API.Helpers;
using unlockit.API.Models;
using unlockit.API.Repositories;
using unlockit_API.DTOs.Order;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        //Dependency Injection 
        private readonly UserRepository _userRepository;
        private readonly OrderRepository _orderRepository;

        public UsersController(UserRepository userRepository, OrderRepository orderRepository)
        {
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            //Benutzer identifizieren
            var user = await _userRepository.GetUserByUsernameAsync(username);

            if (user != null)
            {
                //Formular
                var userDto = new UserDto
                {
                    UserUUID = user.UserUUID,
                    UserName = user.UserName,
                    Role = user.Role,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Birthdate = user.Birthdate,
                    ProfilePictureUrl = user.ProfilePictureUrl
                };
                return Ok(userDto);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            //Validierung (Benutzername & Email)
            var existingUser = await _userRepository.GetUserByUsernameAsync(createUserDto.UserName);

            if (existingUser != null)
            {
                return Conflict("Ein Benutzer mit diesem Benutzername existiert bereits!");
            }
            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(createUserDto.Email);
            if (existingUserByEmail != null)
            {
                return Conflict("Ein Benutzer mit dieser E-Mail-Adresse existiert bereits!");
            }

            //Passwort verschlüsseln
            var passwordHash = PasswordHelper.HashPassword(createUserDto.Password);

            //Rolle erstellen
            var newUser = new User
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                PasswordHash = passwordHash,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Role = Enum.TryParse<UserRole>(createUserDto.Role, true, out var role) ? role : UserRole.Kunde
            };
            //Auftrag & Ergebniss
            var createdUser = await _userRepository.CreateUserAsync(newUser);

            var userDto = new UserDto
            {
                //Formular
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Role = UserRole.Kunde
            };
            return CreatedAtAction(nameof(GetUserByUsername), new { username = userDto.UserName }, userDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{userUuid:guid}")]
        public async Task<IActionResult> UpdateUser(Guid userUuid, [FromBody] UpdateUserDto updateUserDto)
        {
            //Benutzer identifizieren
            var userFromDb = await _userRepository.GetUserByUuidAsync(userUuid);
            if (userFromDb == null)
            {
                return NotFound();
            }
            //Auftrag & Ergebniss
            userFromDb.FirstName = updateUserDto.FirstName;
            userFromDb.LastName = updateUserDto.LastName;
            userFromDb.UserName = updateUserDto.UserName;
            userFromDb.Email = updateUserDto.Email;
            userFromDb.Birthdate = updateUserDto.Birthdate;

            //Rolle aktualisieren (Optional)
            if (Enum.TryParse<UserRole>(updateUserDto.Role, true, out var newRole))
            {
                userFromDb.Role = newRole;
            }

            //Passwort aktualisieren (Optional)
            if (!string.IsNullOrEmpty(updateUserDto.Password))
            {
                userFromDb.PasswordHash = PasswordHelper.HashPassword(updateUserDto.Password);
            }
            else
            {
                userFromDb.PasswordHash = userFromDb.PasswordHash;
            }

            var success = await _userRepository.UpdateUserAsync(userUuid, userFromDb);

            if (success)
            {
                return NoContent();
            }

            return BadRequest("Benutzer konnte nicht aktualisiert werden.");
        }

        [HttpDelete("{userUuid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid userUuid)
        {
            //Auftrag & Ergebniss
            var success = await _userRepository.DeleteUserAsync(userUuid);

            if (success)
            {
                return NoContent();
            }
            else
            {
                return NotFound("Benutzer nicht gefunden.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            //Benutzer holen
            var users = await _userRepository.GetAllUsersAsync();

            //Auftrag & Ergebniss
            var userDtos = users.Select(user => new UserDto
            {
                //Formular
                UserUUID = user.UserUUID,
                UserName = user.UserName,
                Role = user.Role,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthdate = user.Birthdate,
                ProfilePictureUrl = user.ProfilePictureUrl,
                RecentOrders = new List<OrderSummaryDto>()
            }).ToList();

            return Ok(userDtos);
        }

        [HttpGet("{userUuid:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserDetails(Guid userUuid, [FromQuery] int orderCount = 5)
        {
            try
            {
                //Benutzer holen
                var user = await _userRepository.GetUserByUuidAsync(userUuid);
                if (user == null)
                {
                    return NotFound("Benutzer nicht gefunden.");
                }
                var userDto = new UserDto
                {
                    //Formular
                    UserUUID = user.UserUUID,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    Birthdate = user.Birthdate,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    RecentOrders = new List<OrderSummaryDto>()
                };

                //Auftrag & Ergebniss
                if (user.Role == UserRole.Kunde)
                {
                    var recentOrders = await _orderRepository.GetRecentOrdersByUserIdAsync(user.UserId, orderCount);
                    userDto.RecentOrders = recentOrders.ToList();
                }

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ein interner Fehler ist aufgetreten: {ex.Message}");
            }
        }

        [HttpGet("my/addresses")]
        [Authorize]
        public async Task<IActionResult> GetMyAddresses()
        {
            //Benutzer identifizieren
            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Token enthält keine gültige Benutzer-ID.");
            }

            if (!Guid.TryParse(userIdString, out var userUuid))
            {
                return BadRequest("Ungültiges Benutzer-ID-Format im Token.");
            }

            //Auftrag & Ergebniss
            var addresses = await _userRepository.GetAddressesByUserUuidAsync(userUuid);

            return Ok(addresses);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            //Benutzer identifizieren
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(userIdString, out var userUuid))
            {
                return BadRequest("Ungültiges Benutzer-ID-Format im Token.");
            }

            //Auftrag & Ergebniss
            var user = await _userRepository.GetUserByUuidAsync(userUuid);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                //Formular
                UserUUID = user.UserUUID,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Birthdate = user.Birthdate,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return Ok(userDto);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto updateUserDto)
        {
            //Benutzer identifizieren
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userUuid))
            {
                return Unauthorized();
            }
            //Auftrag & Ergebniss
            var userToUpdate = await _userRepository.GetUserByUuidAsync(userUuid);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            //Formular
            userToUpdate.FirstName = updateUserDto.FirstName;
            userToUpdate.LastName = updateUserDto.LastName;
            userToUpdate.Email = updateUserDto.Email;
            userToUpdate.UserName = updateUserDto.UserName;
            userToUpdate.Birthdate = updateUserDto.Birthdate;

            //Passwort aktualisieren (Optional)
            if (!string.IsNullOrWhiteSpace(updateUserDto.Password))
            {
                
                userToUpdate.PasswordHash = PasswordHelper.HashPassword(updateUserDto.Password);
            }
            else
            {
                userToUpdate.PasswordHash = userToUpdate.PasswordHash;
            }

            var success = await _userRepository.UpdateUserAsync(userUuid, userToUpdate);

            if (success)
            {
                var updatedUser = await _userRepository.GetUserByUuidAsync(userUuid);
                var userDto = new UserDto
                {
                    //Formular
                    UserUUID = updatedUser.UserUUID,
                    UserName = updatedUser.UserName,
                    Email = updatedUser.Email,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    Role = updatedUser.Role,
                    Birthdate = updatedUser.Birthdate,
                    ProfilePictureUrl = updatedUser.ProfilePictureUrl
                };
                return Ok(new { message = "Benutzer erfolgreich aktualisiert", user = userDto });
            }

            return BadRequest("Benutzer konnte nicht aktualisiert werden.");
        }

        [HttpPost("me/upload-profile-picture")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            //Benutzer identifizieren
            var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userUuidString, out var userUuid))
            {
                return Unauthorized();
            }

            //Validierung
            if (file == null || file.Length == 0)
            {
                return BadRequest("Es wurde keine Datei hochgeladen.");
            }

            //Ordner im Archiv vorbereiten
            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "ProfilePictures");
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            var fileName = userUuid.ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolderPath, fileName);

            //Auftrag & Ergebniss
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/Uploads/ProfilePictures/{fileName}";

            var success = await _userRepository.UpdateProfilePictureUrlAsync(userUuid, imageUrl);

            if (!success)
            {
                return StatusCode(500, "Das Profilbild konnte nicht in der Datenbank gespeichert werden.");
            }

            return Ok(new { profilePictureUrl = imageUrl });
        }
    }
}