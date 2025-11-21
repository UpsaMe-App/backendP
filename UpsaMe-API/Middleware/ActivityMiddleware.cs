using UpsaMe_API.Services;

namespace UpsaMe_API.Middleware
{
    public class ActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public ActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IConnectionService conn)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdString = context.User.FindFirst("sub")?.Value;

                if (Guid.TryParse(userIdString, out var userId))
                {
                    await conn.UpdateActivityAsync(userId);
                }
            }

            await _next(context);
        }
    }

    public static class ActivityMiddlewareExtensions
    {
        public static IApplicationBuilder UseActivityTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ActivityMiddleware>();
        }
    }
}