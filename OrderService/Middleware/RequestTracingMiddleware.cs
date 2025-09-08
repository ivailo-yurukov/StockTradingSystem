using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace OrderService.Middleware
{
    public class RequestTracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTracingMiddleware> _logger;
        private static readonly ActivitySource ActivitySource = new("OrderService.Tracing");

        public RequestTracingMiddleware(RequestDelegate next, ILogger<RequestTracingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Accept incoming request id or create a new one
            var incoming = context.Request.Headers.TryGetValue("X-Request-ID", out StringValues v) ? v.ToString() : Guid.NewGuid().ToString();

            // Start an Activity so OpenTelemetry/Apm can pick it up
            using var activity = ActivitySource.StartActivity($"{context.Request.Method} {context.Request.Path}", ActivityKind.Server);
            if (activity != null)
            {
                activity.SetTag("http.method", context.Request.Method);
                activity.SetTag("http.path", context.Request.Path);
                activity.SetTag("request.id", incoming);
                activity.SetTag("service.name", "OrderService");
            }

            // Ensure response includes the request id so callers can correlate
            context.Response.Headers["X-Request-ID"] = incoming;

            // Add a logging scope with trace/request identifiers
            var scopeState = new Dictionary<string, object?>
            {
                ["TraceId"] = activity?.TraceId.ToHexString(),
                ["SpanId"] = activity?.SpanId.ToHexString(),
                ["RequestId"] = incoming
            };

            using (_logger.BeginScope(scopeState))
            {
                await _next(context);
            }
        }
    }

    public static class RequestTracingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestTracing(this IApplicationBuilder app)
            => app.UseMiddleware<RequestTracingMiddleware>();
    }
}