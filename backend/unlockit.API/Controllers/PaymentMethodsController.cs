using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unlockit.API.DTOs.PaymentMethod;
using unlockit.API.Models;
using unlockit.API.Repositories; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodsController : ControllerBase
    {
        //Dependency Injection 
        private readonly PaymentMethodRepository _repository;

        public PaymentMethodsController(PaymentMethodRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Mitarbeiter")]
        public async Task<ActionResult<IEnumerable<PaymentMethodDto>>> GetAll()
        {
            var methods = await _repository.GetAllAsync();
            var dtos = methods.Select(m => new PaymentMethodDto { PaymentMethodId = m.PaymentMethodId, Name = m.Name, IsEnabled = m.IsEnabled });
            return Ok(dtos);
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<PaymentMethodDto>>> GetActive()
        {
            var methods = await _repository.GetActiveAsync();
            var dtos = methods.Select(m => new PaymentMethodDto { PaymentMethodId = m.PaymentMethodId, Name = m.Name, IsEnabled = m.IsEnabled });
            return Ok(dtos);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentMethodDto>> Create(CreatePaymentMethodDto createDto)
        {
            var newMethod = new PaymentMethod { Name = createDto.Name, IsEnabled = true };
            var createdMethod = await _repository.CreateAsync(newMethod);
            var dto = new PaymentMethodDto { PaymentMethodId = createdMethod.PaymentMethodId, Name = createdMethod.Name, IsEnabled = createdMethod.IsEnabled };
            return CreatedAtAction(nameof(GetAll), new { id = dto.PaymentMethodId }, dto);
        }

        [HttpPut("{id}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var method = await _repository.GetByIdAsync(id);
            if (method == null)
            {
                return NotFound();
            }

            method.IsEnabled = !method.IsEnabled;
            method.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(method);

            return NoContent();
        }
    }
}