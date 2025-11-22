using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;
using UpsaMe_API.Models;
using UpsaMe_API.Models.Enums;
using UpsaMe_API.Services;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendlyWebhookController : ControllerBase
    {
        private readonly UpsaMeDbContext _db;
        private readonly ILogger<CalendlyWebhookController> _logger;
        private readonly NotificationService _notifications;

        public CalendlyWebhookController(
            UpsaMeDbContext db,
            ILogger<CalendlyWebhookController> logger,
            NotificationService notifications)
        {
            _db = db;
            _logger = logger;
            _notifications = notifications;
        }

        /// <summary>
        /// Endpoint que recibe los webhooks enviados por Calendly.
        /// Configura en Calendly la URL:
        ///   https://TU-DOMINIO/api/CalendlyWebhook/webhook
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // Calendly no manda tu JWT
        public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload, CancellationToken ct)
        {
            // 1) SIEMPRE guardamos el log crudo para debug
            var log = new WebhookLog
            {
                Id = Guid.NewGuid(),
                Source = "Calendly",
                Payload = payload.GetRawText(),
                ReceivedAtUtc = DateTime.UtcNow
            };

            _db.WebhookLogs.Add(log);
            await _db.SaveChangesAsync(ct);

            try
            {
                // =========================
                // 2) Leemos tipo de evento
                // =========================
                var eventName = payload.TryGetProperty("event", out var evProp)
                    ? evProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(eventName))
                {
                    _logger.LogWarning("Calendly webhook sin 'event'. LogId={Id}", log.Id);
                    return Ok(new { ok = true });
                }

                if (!payload.TryGetProperty("payload", out var p))
                {
                    _logger.LogWarning("Calendly webhook sin 'payload'. LogId={Id}", log.Id);
                    return Ok(new { ok = true });
                }

                // Campos comunes
                var inviteeEmail = GetStringSafe(p, "invitee", "email");
                var inviteeUri = GetStringSafe(p, "invitee", "uri");
                var eventUri = GetStringSafe(p, "event", "uri");
                var startStr = GetStringSafe(p, "event", "start_time");
                var endStr = GetStringSafe(p, "event", "end_time");
                var meetingUrl = GetStringSafe(p, "event", "location", "join_url");

                DateTime? startsUtc = ParseCalendlyDate(startStr);
                DateTime? endsUtc = ParseCalendlyDate(endStr);

                // Intentamos obtener el host (mentor) desde el payload
                var hostEmail = GetStringSafe(p, "event", "event_memberships", "0", "user", "email");
                if (string.IsNullOrWhiteSpace(hostEmail))
                    hostEmail = GetStringSafe(p, "event_memberships", "0", "user", "email");

                var mentor = !string.IsNullOrWhiteSpace(hostEmail)
                    ? await _db.Users.FirstOrDefaultAsync(u => u.Email == hostEmail, ct)
                    : null;

                var student = !string.IsNullOrWhiteSpace(inviteeEmail)
                    ? await _db.Users.FirstOrDefaultAsync(u => u.Email == inviteeEmail, ct)
                    : null;

                // =========================
                // 3) INVITEE CREATED
                // =========================
                if (eventName.Contains("invitee.created", StringComparison.OrdinalIgnoreCase))
                {
                    if (mentor == null || !startsUtc.HasValue || !endsUtc.HasValue)
                    {
                        _logger.LogWarning(
                            "No se pudo crear Session desde Calendly (mentor/fechas faltan). LogId={Id}",
                            log.Id);
                        return Ok(new { ok = true });
                    }

                    var session = new Session
                    {
                        Id = Guid.NewGuid(),
                        MentorUserId = mentor.Id,
                        InviteeEmail = inviteeEmail ?? string.Empty,
                        StartsAtUtc = startsUtc.Value,
                        EndsAtUtc = endsUtc.Value,
                        MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl,
                        Status = SessionStatus.Scheduled,
                        DealId = !string.IsNullOrWhiteSpace(inviteeUri) ? inviteeUri : eventUri,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    _db.Sessions.Add(session);
                    await _db.SaveChangesAsync(ct);

                    // Notificación al mentor
                    var title = "Nueva sesión agendada";
                    var body = student != null
                        ? $"{student.FirstName} {student.LastName} agendó una tutoría para el {session.StartsAtUtc:dd/MM HH:mm}."
                        : $"Tienes una nueva tutoría con {inviteeEmail} el {session.StartsAtUtc:dd/MM HH:mm}.";

                    await _notifications.CreateAsync(
                        mentor.Id,
                        title,
                        body,
                        new
                        {
                            sessionId = session.Id,
                            fromCalendly = true,
                            type = NotificationType.SessionBooked.ToString()
                        });

                    _logger.LogInformation("Session creada desde Calendly. SessionId={SessionId}, LogId={LogId}",
                        session.Id, log.Id);
                }
                // =========================
                // 4) INVITEE CANCELED
                // =========================
                else if (eventName.Contains("invitee.canceled", StringComparison.OrdinalIgnoreCase))
                {
                    Session? session = null;

                    if (!string.IsNullOrWhiteSpace(inviteeUri))
                    {
                        session = await _db.Sessions
                            .FirstOrDefaultAsync(s => s.DealId == inviteeUri, ct);
                    }

                    if (session == null && !string.IsNullOrWhiteSpace(inviteeEmail) && startsUtc.HasValue)
                    {
                        var min = startsUtc.Value.AddMinutes(-1);
                        var max = startsUtc.Value.AddMinutes(1);

                        session = await _db.Sessions.FirstOrDefaultAsync(
                            s => s.InviteeEmail == inviteeEmail &&
                                 s.StartsAtUtc >= min &&
                                 s.StartsAtUtc <= max,
                            ct);
                    }

                    if (session != null)
                    {
                        session.Status = SessionStatus.Canceled;
                        session.UpdatedAtUtc = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);

                        if (mentor != null)
                        {
                            await _notifications.CreateAsync(
                                mentor.Id,
                                "Sesión cancelada",
                                $"La tutoría con {inviteeEmail} del {session.StartsAtUtc:dd/MM HH:mm} fue cancelada.",
                                new
                                {
                                    sessionId = session.Id,
                                    fromCalendly = true,
                                    type = NotificationType.SessionCanceled.ToString()
                                });
                        }

                        _logger.LogInformation(
                            "Session cancelada desde Calendly. SessionId={SessionId}, LogId={LogId}",
                            session.Id, log.Id);
                    }
                    else
                    {
                        _logger.LogWarning("No se encontró Session para cancelar (Calendly). LogId={Id}", log.Id);
                    }
                }
                else
                {
                    // Otros eventos que no usamos aún
                    _logger.LogInformation("Webhook Calendly ignorado (event={Event}). LogId={Id}",
                        eventName, log.Id);
                }

                // A Calendly casi siempre conviene responder 200
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando webhook de Calendly. LogId={Id}", log.Id);
                // Para no romper el webhook en Calendly, devolvemos 200 igual.
                return Ok(new { ok = true });
            }
        }

        // ======================
        // Helpers privados
        // ======================
        private static DateTime? ParseCalendlyDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var dt))
            {
                return dt;
            }

            return null;
        }

        /// <summary>
        /// Navega por un JsonElement usando una ruta de propiedades (y/o índices de array) y devuelve string.
        /// Si algo falla, devuelve null.
        /// </summary>
        private static string? GetStringSafe(JsonElement root, params string[] path)
        {
            try
            {
                JsonElement current = root;

                foreach (var segment in path)
                {
                    // Si el segmento es numérico y estamos en un array
                    if (segment.All(char.IsDigit) && current.ValueKind == JsonValueKind.Array)
                    {
                        if (!int.TryParse(segment, out var idx)) return null;
                        if (idx < 0 || idx >= current.GetArrayLength()) return null;

                        current = current[idx];
                    }
                    else
                    {
                        if (!current.TryGetProperty(segment, out var next))
                            return null;

                        current = next;
                    }
                }

                return current.ValueKind == JsonValueKind.String
                    ? current.GetString()
                    : current.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
