using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unlockit.API.Repositories;
using System;
using System.Threading.Tasks;
using unlockit.API.Models;

namespace unlockit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class BillingController : ControllerBase
    {
        //Dependency Injection 
        private readonly BillingRepository _billingRepository;

        public BillingController(BillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int year, [FromQuery] int month)
        {
            //Abfrage (Zeitraum)
            if (year == 0 || month == 0)
            {
                year = DateTime.UtcNow.Year;
                month = DateTime.UtcNow.Month;
            }

            // Auftrag & Ergebniss
            var summary = await _billingRepository.GetFinancialSummary(year, month);
            if (summary == null)
            {
                return NotFound();
            }
            return Ok(summary);
        }

        [HttpGet("summary/overall")]
        public async Task<IActionResult> GetOverallSummary()
        {
            // Auftrag & Ergebniss
            var summary = await _billingRepository.GetOverallFinancialSummary();
            if (summary == null)
            {
                return NotFound();
            }
            return Ok(summary);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetMonthlyTransactions([FromQuery] int year, [FromQuery] int month)
        {
            //Abfrage (Zeitraum)
            if (year == 0 || month == 0)
            {
                year = DateTime.UtcNow.Year;
                month = DateTime.UtcNow.Month;
            }

            // Auftrag & Ergebniss
            var transactions = await _billingRepository.GetTransactionsAsync(year, month);
            return Ok(transactions);
        }

        [HttpGet("transactions/all")]
        public async Task<IActionResult> GetAllTimeTransactions()
        {
            // Auftrag & Ergebniss
            var transactions = await _billingRepository.GetAllTransactionsAsync();
            return Ok(transactions);
        }
    }
}