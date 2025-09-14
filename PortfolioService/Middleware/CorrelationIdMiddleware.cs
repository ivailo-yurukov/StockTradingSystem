using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace PortfolioService.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        public const string HeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Ensure Activity exists
            if (Activity.Current == null)
            {
                var root = new Activity("incoming-http");
                root.SetIdFormat(ActivityIdFormat.W3C);
                root.Start();
            }

            // Get or create correlation id
            if (!context.Request.Headers.TryGetValue(HeaderName, out var cid) || string.IsNullOrWhiteSpace(cid))
            {
                cid = Guid.NewGuid().ToString();
                context.Request.Headers[HeaderName] = cid;
            }

            var correlationId = cid.ToString();
            context.Items[HeaderName] = correlationId;

            // Tag activity
            Activity.Current?.AddTag("correlation_id", correlationId);

            // Push into Serilog LogContext and create logging scope for Microsoft ILogger
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty))
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = Activity.Current?.TraceId.ToString()
            }))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[HeaderName] = correlationId;
                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }
    }
}
