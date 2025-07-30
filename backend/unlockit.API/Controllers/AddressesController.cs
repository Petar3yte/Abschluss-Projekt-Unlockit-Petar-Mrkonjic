using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using unlockit.API.DTOs.Address;
using unlockit.API.Models;
using unlockit.API.Repositories;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        //Dependency Injection 
        private readonly UserRepository _userRepository;

        public AddressesController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto addressDto)
        {
            //Sicherheitskontrolle
            var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userUuidString, out var userUuid))
            {
                return Unauthorized();
            }

            //Formular
            var newAddress = new Address
            {
                Name = addressDto.Name,
                AddressLine1 = addressDto.AddressLine1,
                City = addressDto.City,
                PostalCode = addressDto.PostalCode,
                Country = addressDto.Country
            };

            // Auftrag & Ergebniss
            try
            {
                var createdAddress = await _userRepository.CreateAddressAsync(userUuid, newAddress);
                return CreatedAtAction(nameof(CreateAddress), new { id = createdAddress.AddressUUID }, createdAddress);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //ID muss vorhanden sein!
        [HttpPut("{addressUuid:guid}")]
        public async Task<IActionResult> UpdateAddress(Guid addressUuid, [FromBody] UpdateAddressDto addressDto)
        {
            //Sicherheitskontrolle
            var userUuidString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userUuidString, out var userUuid))
            {
                return Unauthorized();
            }

            //Formular
            var addressToUpdate = new Address
            {
                Name = addressDto.Name,
                AddressLine1 = addressDto.AddressLine1,
                City = addressDto.City,
                PostalCode = addressDto.PostalCode,
                Country = addressDto.Country
            };

            // Auftrag & Ergebniss
            var success = await _userRepository.UpdateAddressAsync(addressUuid, userUuid, addressToUpdate);

            if (!success)
            {
                return NotFound("Adresse nicht gefunden oder keine Berechtigung zum Bearbeiten.");
            }

            return NoContent(); 
        }

        //ID muss vorhanden sein!
        [HttpDelete("{addressUuid:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid addressUuid)
        {
            //Sicherheitskontrolle
            var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userUuidString, out var userUuid))
            {
                return Unauthorized();
            }

            //Auftrag & Ergebniss
            var success = await _userRepository.DeleteAddressAsync(addressUuid, userUuid);

            if (!success)
            {
                return NotFound("Adresse nicht gefunden oder keine Berechtigung zum Löschen.");
            }

            return NoContent();
        }
    }
}