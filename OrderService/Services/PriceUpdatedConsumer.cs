using MassTransit;
using OrderService.Events;
using System.Diagnostics;

namespace OrderService.Services
{
    public class PriceUpdatedConsumer : IConsumer<PriceUpdatedEvent>
    {
        private readonly PriceCache _priceCache;
        private readonly ILogger<PriceUpdatedConsumer> _logger;

        public PriceUpdatedConsumer(PriceCache priceCache, ILogger<PriceUpdatedConsumer> logger)
        {
            _priceCache = priceCache;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PriceUpdatedEvent> context)
        {
            // Extract message-level metadata
            var messageId = context.MessageId;
            var correlationId = context.CorrelationId;
            context.Headers.TryGetHeader("trace-id", out var traceHeader);
            context.Headers.TryGetHeader("request-id", out var requestHeader);

            var traceId = traceHeader?.ToString() ?? Activity.Current?.TraceId.ToHexString();
            var requestId = requestHeader?.ToString() ?? Activity.Current?.Id;

            var ticker = (context.Message.Ticker ?? string.Empty).Trim().ToUpperInvariant();
            _priceCache.UpdatePrice(ticker, context.Message.Price);

            _logger.LogInformation("Consumed PriceUpdatedEvent MessageId={MessageId} CorrelationId={CorrelationId} TraceId={TraceId} RequestId={RequestId} Ticker={Ticker} Price={Price}",
                messageId, correlationId, traceId, requestId, ticker, context.Message.Price);

            return Task.CompletedTask;
        }
    }
}
