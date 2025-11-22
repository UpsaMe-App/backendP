using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UpsaMe_API.Data;
using UpsaMe_API.Models;

namespace UpsaMe_API.Services
{
    public class CalendlyService
    {
        private readonly HttpClient _http;
        private readonly UpsaMeDbContext _db;
        private readonly ILogger<CalendlyService> _logger;

        public CalendlyService(
            HttpClient http,
            UpsaMeDbContext db,
            IConfiguration config,
            ILogger<CalendlyService> logger)
        {
            _http = http;
            _db = db;
            _logger = logger;

            // BaseUrl por si no vino ya seteado en Program.cs
            if (_http.BaseAddress == null)
            {
                var baseUrl = config["Calendly:BaseUrl"] ?? "https://api.calendly.com/";
                _http.BaseAddress = new Uri(baseUrl);
            }

            // API Key por si no vino ya en los headers
            if (_http.DefaultRequestHeaders.Authorization == null)
            {
                var apiKey = config["Calendly:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);
                }
            }
        }

        // ================================
        // 1) Sincronizar próximos eventos desde Calendly -> DB
        // ================================
        public async Task<List<CalendlyEvent>> SyncUpcomingEventsAsync(CancellationToken ct = default)
        {
            // 1) Obtener los datos del usuario dueño del token (tu cuenta de Calendly)
            var me = await _http.GetFromJsonAsync<UserMeResponse>("users/me", ct);
            var userUri = me?.resource?.uri
                          ?? throw new InvalidOperationException("No se pudo obtener users/me de Calendly");

            // 2) Pedir eventos futuros para ese usuario
            var nowIso = DateTime.UtcNow.ToString("O"); // ISO 8601
            var url = $"scheduled_events?user={Uri.EscapeDataString(userUri)}" +
                      $"&status=active&sort=start_time:asc&min_start_time={nowIso}&count=50";

            _logger.LogInformation("Solicitando eventos a Calendly: {Url}", url);

            var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            var payload = await resp.Content.ReadFromJsonAsync<ScheduledEventsResponse>(cancellationToken: ct);

            if (payload?.collection == null || payload.collection.Count == 0)
                return new List<CalendlyEvent>();

            var result = new List<CalendlyEvent>();

            foreach (var item in payload.collection)
            {
                if (string.IsNullOrWhiteSpace(item.uri))
                    continue;

                var existing = await _db.CalendlyEvents
                    .FirstOrDefaultAsync(e => e.EventUri == item.uri, ct);

                if (existing == null)
                {
                    existing = new CalendlyEvent
                    {
                        EventUri = item.uri,
                        StartUtc = item.start_time,
                        EndUtc   = item.end_time,
                        Status   = item.status,
                        RawJson  = null
                    };

                    _db.CalendlyEvents.Add(existing);
                }
                else
                {
                    existing.StartUtc = item.start_time;
                    existing.EndUtc   = item.end_time;
                    existing.Status   = item.status;
                }

                result.Add(existing);
            }

            await _db.SaveChangesAsync(ct);
            return result;
        }


        // ================================
        // 2) Leer próximos eventos SOLO desde tu DB
        //    (lo usarás en PERFIL -> "Mis eventos")
        // ================================
        public async Task<List<CalendlyEvent>> GetUpcomingFromDbAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            return await _db.CalendlyEvents
                .Where(e => e.StartUtc >= now)
                .OrderBy(e => e.StartUtc)
                .ToListAsync(ct);
        }

        // ================================
        // 3) Métodos genéricos (por si los quieres seguir usando)
        // ================================
        // Obtener algo crudo desde una URL de Calendly (por ejemplo un userUri)
        public async Task<T?> GetUserEventsAsync<T>(string userUri, CancellationToken ct = default)
        {
            var resp = await _http.GetAsync(userUri, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }

        // Crear un evento programado (placeholder, ajústalo a lo que pida Calendly)
        public async Task<HttpResponseMessage> CreateInviteAsync(object payload, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("scheduled_events", payload, ct);
            return resp;
        }

        // ================================
        // Clases internas para mapear la respuesta de Calendly
        // ================================
        private class ScheduledEventsResponse
        {
            public List<ScheduledEventItem> collection { get; set; } = new();
        }

        private class ScheduledEventItem
        {
            public string uri { get; set; } = string.Empty;
            public string? status { get; set; }
            public DateTime start_time { get; set; }
            public DateTime end_time { get; set; }
        }
        // ================================
// Clases para /users/me de Calendly
// ================================
        private class UserMeResponse
        {
            public UserResource resource { get; set; } = new();
        }

        private class UserResource
        {
            public string uri { get; set; } = string.Empty;
        }

    }
}