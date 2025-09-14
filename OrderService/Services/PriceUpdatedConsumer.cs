using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Contracts.Events;
using Serilog.Context;
using LogContext = Serilog.Context.LogContext;

namespace OrderService.Services
{
    public class PriceUpdatedConsumer : IConsumer<IPriceUpdatedEvent>
    {
        private static readonly ActivitySource ActivitySource = new("OrderService.PriceUpdatedConsumer");
        private readonly PriceCache _priceCache;
        private readonly ILogger<PriceUpdatedConsumer> _logger;

        public PriceUpdatedConsumer(PriceCache priceCache, ILogger<PriceUpdatedConsumer> logger)
        {
            _priceCache = priceCache;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<IPriceUpdatedEvent> context)
        {
            var msg = context.Message;
            var ticker = (msg.Ticker ?? string.Empty).Trim().ToUpperInvariant();

            // Extract correlation/tracing info from message headers or Activity
            context.Headers.TryGetHeader("X-Correlation-ID", out var corrObj);
            var correlationId = corrObj?.ToString()
                                ?? context.CorrelationId?.ToString()
                                ?? Activity.Current?.TraceId.ToString()
                                ?? Guid.NewGuid().ToString();

            // Start a consumer span so traces include this work
            using var activity = ActivitySource.StartActivity("ConsumePriceUpdated", ActivityKind.Consumer);
            activity?.AddTag("correlation_id", correlationId);
            activity?.AddTag("ticker", ticker);

            var traceId = activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? string.Empty;

            // Push properties into Serilog LogContext and create an ILogger scope
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", traceId))
            using (_logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = traceId,
                ["MessageTicker"] = ticker
            }))
            {
                _logger.LogInformation("[OrderService] Consumer received {Ticker} => {Price}", ticker, msg.Price);

              
                _priceCache.UpdatePrice(ticker, msg.Price);
            }

            return Task.CompletedTask;
        }
    }
}
