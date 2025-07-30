using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using unlockit.API.Repositories;
using unlockit.API.DTOs.Cart;
using unlockit.API.Models;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class CartController : ControllerBase
    {
        //Dependency Injection 
        private readonly CartRepository _cartRepository;
        private readonly UserRepository _userRepository;

        public CartController(CartRepository cartRepository, UserRepository userRepository)
        {
            _cartRepository = cartRepository;
            _userRepository = userRepository;
        }

        private async Task<User> GetCurrentUser()
        {
            //Benutzer-UUID holen
            var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Benutzer aus der Datenbank laden
            if (Guid.TryParse(userUuidString, out var userUuid))
            {
                return await _userRepository.GetUserByUuidAsync(userUuid);
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            //Benutzer identifizieren
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();
            //Warenkorb aus der Datenbank holen
            var items = await _cartRepository.GetCartItemsByUserIdAsync(user.UserId);
            return Ok(items);
        }

        [HttpPost("merge")]
        public async Task<IActionResult> MergeCart([FromBody] List<CartItemDto> localCartItems)
        {
            //Benutzer identifizieren
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            //Auftrag & Ergebniss
            await _cartRepository.MergeLocalCartAsync(user.UserId, localCartItems);

            var updatedItems = await _cartRepository.GetCartItemsByUserIdAsync(user.UserId);
            return Ok(updatedItems);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            //Benutzer identifizieren
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            //Auftrag & Ergebniss
            await _cartRepository.ClearCartAsync(user.UserId);
            return NoContent();
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] CartItemDto itemDto)
        {
            //Benutzer identifizieren
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            //Validierung
            if (itemDto.Quantity <= 0) return BadRequest("Menge muss positiv sein.");

            await _cartRepository.AddItemAsync(user.UserId, itemDto.ProductUuid, itemDto.Quantity);
            //Auftrag & Ergebniss
            var updatedItems = await _cartRepository.GetCartItemsByUserIdAsync(user.UserId);
            return Ok(updatedItems);
        }

        [HttpPut("items/{productUuid}")]
        public async Task<IActionResult> UpdateItemQuantity(Guid productUuid, [FromBody] CartItemDto itemDto)
        {
            //Benutzer identifizieren
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            //Auftrag & Ergebniss
            await _cartRepository.UpdateItemQuantityAsync(user.UserId, productUuid, itemDto.Quantity);

            var updatedItems = await _cartRepository.GetCartItemsByUserIdAsync(user.UserId);
            return Ok(updatedItems);
        }

        [HttpDelete("items/{productUuid}")]
        public async Task<IActionResult> RemoveItem(Guid productUuid)
        {
            //Benutzer identifizieren
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            //Auftrag & Ergebniss
            await _cartRepository.RemoveItemAsync(user.UserId, productUuid);

            var updatedItems = await _cartRepository.GetCartItemsByUserIdAsync(user.UserId);
            return Ok(updatedItems);
        }
    }
}