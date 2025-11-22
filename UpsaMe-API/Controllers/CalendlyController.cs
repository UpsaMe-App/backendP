using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsaMe_API.Services;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("calendly")]
    public class CalendlyController : ControllerBase
    {
        private readonly CalendlyService _calendlyService;

        public CalendlyController(CalendlyService calendlyService)
        {
            _calendlyService = calendlyService;
        }

        // ================================
        // 1) Forzar sincronización con Calendly
        //    y devolver eventos futuros
        // ================================
        [HttpGet("events/sync")]
        [Authorize] // puedes poner [AllowAnonymous] si quieres probar sin login
        public async Task<IActionResult> SyncAndGetUpcoming(CancellationToken ct)
        {
            var events = await _calendlyService.SyncUpcomingEventsAsync(ct);

            var result = events.Select(e => new
            {
                e.Id,
                e.EventUri,
                e.Status,
                StartLocal = e.StartUtc,
                EndLocal = e.EndUtc
            });

            return Ok(result);
        }

        // ================================
        // 2) Leer solo lo que ya está en DB
        // ================================
        [HttpGet("events/upcoming")]
        [Authorize]
        public async Task<IActionResult> GetUpcomingFromDb(CancellationToken ct)
        {
            var events = await _calendlyService.GetUpcomingFromDbAsync(ct);

            var result = events.Select(e => new
            {
                e.Id,
                e.EventUri,
                e.Status,
                StartLocal = e.StartUtc,
                EndLocal = e.EndUtc
            });

            return Ok(result);
        }
    }
}