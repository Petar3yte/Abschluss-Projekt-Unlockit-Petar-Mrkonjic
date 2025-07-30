using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using unlockit.API.DTOs.Order;
using unlockit.API.Repositories;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    //Dependency Injection 
    private readonly OrderRepository _orderRepository;
    private readonly CartRepository _cartRepository;
    private readonly UserRepository _userRepository;

    public OrdersController(OrderRepository orderRepository, CartRepository cartRepository, UserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
    }

    [HttpPost]
    [Authorize(Roles = "Kunde,Admin")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        //Identität & Berechtigung prüfen
        var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userUuidString, out var userUuid))
        {
            return Unauthorized("Ungültiger oder fehlender Token.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            //Bestellung erstellen
            var createdOrder = await _orderRepository.CreateOrderAsync(
                userUuid,
                createOrderDto.ShippingAddressUUID,
                createOrderDto.Items,
                createOrderDto.PaymentMethodName);

            //Wareenkorb leeren
            var user = await _userRepository.GetUserByUuidAsync(userUuid);

            if (user != null)
            {
                await _cartRepository.ClearCartAsync(user.UserId);
            }

            return StatusCode(201, new { OrderUUID = createdOrder.OrderUUID });
        }
        //Prüfungen
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unerwarteter Fehler bei der Erstellung der Bestellung: {ex}");
            return StatusCode(500, new { message = "Ein interner Serverfehler ist aufgetreten." });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int? limit)
    {
        //Benutzer identifizieren
        var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userUuidString, out var userUuid))
        {
            return Unauthorized();
        }
        //Auftrag & Ergebniss
        var orders = await _orderRepository.GetOrdersByUserAsync(userUuid, limit);
        return Ok(orders);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,Mitarbeiter")]
    public async Task<IActionResult> GetAllOrders()
    {
        //Auftrag & Ergebniss
        try
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            return Ok(orders);
        }
        catch (Exception)
        {
            return StatusCode(500, "Ein interner Fehler ist aufgetreten.");
        }
    }

    [HttpGet("{orderUuid}")]
    [Authorize(Roles = "Admin,Mitarbeiter")]
    public async Task<IActionResult> GetOrderDetails(Guid orderUuid)
    {
        try
        {
            //Auftrag & Ergebniss (Bestelldetails aus DB holen)
            var order = await _orderRepository.GetOrderDetailsByUuidAsync(orderUuid);

            if (order == null)
            {
                return NotFound("Bestellung nicht gefunden.");
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ein interner Fehler ist aufgetreten.");
        }
    }


    [HttpPost("{orderUuid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderUuid)
    {
        //Benutzer identifizieren
        var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userUuidString, out var userUuid))
        {
            return Unauthorized("Ungültiger Token.");
        }

        //Auftrag & Ergebniss
        var success = await _orderRepository.CancelOrderAsync(orderUuid, userUuid);

        if (success)
        {
            return NoContent();
        }

        return BadRequest("Bestellung konnte nicht storniert werden (existiert nicht, oder bereits versendet/storniert).");
    }

    [HttpPut("{orderUuid}/status")]
    [Authorize(Roles = "Admin,Mitarbeiter")]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderUuid, [FromBody] UpdateOrderStatusDto updateDto)
    {
        //Validierung
        if (updateDto == null || string.IsNullOrWhiteSpace(updateDto.NewStatus))
        {
            return BadRequest("Neuer Status darf nicht leer sein.");
        }

        try
        {
            //Auftrag & Ergebniss
            var success = await _orderRepository.UpdateOrderStatusAsync(orderUuid, updateDto.NewStatus);

            if (success)
            {
                return Ok(new { message = "Bestellstatus erfolgreich aktualisiert." });
            }
            else
            {
                return BadRequest("Bestellstatus konnte nicht aktualisiert werden. (Ungültiger Status oder Bestellung nicht gefunden)");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ein interner Fehler ist aufgetreten.");
        }
    }

    [HttpPost("{orderUuid:guid}/reorder")]
    public async Task<IActionResult> Reorder(Guid orderUuid)
    {
        //Benutzer identifizieren
        var userUuidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userUuidString, out var userUuid))
        {
            return Unauthorized(new { message = "Ungültige Benutzer-ID im Token." });
        }

        try
        {
            var user = await _userRepository.GetUserByUuidAsync(userUuid);
            if (user == null)
            {
                return Unauthorized("Benutzer nicht gefunden.");
            }

            //Auftrag & Ergebniss (Artikel der alten Bestellung holen)
            var itemsToReorder = await _orderRepository.GetOrderItemsForReorderAsync(orderUuid, user.UserId);

            if (!itemsToReorder.Any())
            {
                return BadRequest(new { message = "Keine Artikel in der Bestellung gefunden oder Sie haben keine Berechtigung für diese Aktion." });
            }

            //Artikel der alten Bestellung in den aktuellen Warenkorb legen
            foreach (var item in itemsToReorder)
            {
                await _cartRepository.AddItemAsync(user.UserId, item.ProductUuid, item.Quantity);
            }

            //Neu gefüllten Warenkorb zurückgeben
            var updatedCartItems = await _cartRepository.GetCartItemsByUserIdAsync(user.UserId);
            return Ok(updatedCartItems);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ein unerwarteter interner Fehler ist aufgetreten: " + ex.Message });
        }
    }
}