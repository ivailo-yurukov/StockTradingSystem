using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioService.Data;
using PortfolioService.Models;
using Contracts.Events;

namespace PortfolioService.Services
{
    public class PriceUpdatedConsumer : IConsumer<IPriceUpdatedEvent>
    {
        private static readonly ActivitySource ActivitySource = new("PortfolioService.PriceUpdatedConsumer");

        private readonly PortfolioDbContext _dbContext;
        private readonly ILogger<PriceUpdatedConsumer> _logger;

        public PriceUpdatedConsumer(PortfolioDbContext dbContext, ILogger<PriceUpdatedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPriceUpdatedEvent> context)
        {
            // Extract message and identifiers
            var message = context.Message;
            var ticker = (message.Ticker ?? string.Empty).Trim().ToUpperInvariant();

            // Try to extract correlation/tracing info
            context.Headers.TryGetHeader("X-Correlation-ID", out var corrObj);
            var correlationId = corrObj?.ToString()
                ?? context.CorrelationId?.ToString()
                ?? Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString();

            // Start a consumer span if not present
            using (var activity = ActivitySource.StartActivity("ConsumePriceUpdated", ActivityKind.Consumer))
            {
                activity?.AddTag("correlation_id", correlationId);
                activity?.AddTag("ticker", ticker);
                var traceId = activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString();

                // Create logging scope
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["TraceId"] = traceId,
                    ["MessageTicker"] = ticker
                }))
                {
                    _logger.LogInformation("Received PriceUpdatedEvent: Ticker={Ticker}, Price={Price}, Timestamp={Timestamp}", ticker, message.Price, message.Timestamp);

                    // DB lookup span
                    using (var dbSpan = ActivitySource.StartActivity("LookupPriceInDb", ActivityKind.Internal))
                    {
                        var price = await _dbContext.Prices.FirstOrDefaultAsync(p => p.Ticker == ticker);

                        if (price == null)
                        {
                            price = new Price
                            {
                                Ticker = ticker,
                                CurrentPrice = message.Price,
                                UpdatedAt = message.Timestamp
                            };
                            _dbContext.Prices.Add(price);
                            _logger.LogDebug("Inserted new price record: Ticker={Ticker}, Price={Price}", ticker, message.Price);
                        }
                        else
                        {
                            price.CurrentPrice = message.Price;
                            price.UpdatedAt = message.Timestamp;
                            _logger.LogDebug("Updated price record: Ticker={Ticker}, Price={Price}", ticker, message.Price);
                        }

                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("Price persisted for Ticker={Ticker}", ticker);
                    }
                }
            }
        }
    }
}
