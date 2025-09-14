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
    public class OrderExecutedConsumer : IConsumer<IOrderExecutedEvent>, IConsumer<IPriceUpdatedEvent>
    {
        private static readonly ActivitySource ActivitySource = new("PortfolioService.OrderExecutedConsumer");

        private readonly PortfolioDbContext _dbContext;
        private readonly ILogger<OrderExecutedConsumer> _logger;

        public OrderExecutedConsumer(PortfolioDbContext dbContext, ILogger<OrderExecutedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderExecutedEvent> context)
        {
            var message = context.Message;

            context.Headers.TryGetHeader("X-Correlation-ID", out var corrObj);
            var correlationId = corrObj?.ToString()
                ?? context.CorrelationId?.ToString()
                ?? Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString();

            using (var activity = ActivitySource.StartActivity("ConsumeOrderExecuted", ActivityKind.Consumer))
            {
                activity?.AddTag("correlation_id", correlationId);
                activity?.AddTag("user_id", message.UserId ?? string.Empty);
                activity?.AddTag("ticker", message.Ticker ?? string.Empty);

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["TraceId"] = activity?.TraceId.ToString(),
                    ["UserId"] = message.UserId,
                    ["Ticker"] = message.Ticker
                }))
                {
                    _logger.LogInformation("Received OrderExecutedEvent: UserId={UserId}, Ticker={Ticker}, Side={Side}, Quantity={Quantity}, Price={Price}",
                        message.UserId, message.Ticker, message.Side, message.Quantity, message.Price);

                    var holding = await _dbContext.Holdings
                        .FirstOrDefaultAsync(h => h.UserId == message.UserId && h.Ticker == message.Ticker);

                    if (holding == null)
                    {
                        holding = new Holding
                        {
                            UserId = message.UserId,
                            Ticker = message.Ticker,
                            Quantity = 0,
                            AveragePrice = 0
                        };
                        _dbContext.Holdings.Add(holding);
                        _logger.LogDebug("Created new holding for UserId={UserId}, Ticker={Ticker}", message.UserId, message.Ticker);
                    }

                    if (message.Side == "buy")
                    {
                        var totalCost = (holding.AveragePrice * holding.Quantity) + (message.Price * message.Quantity);
                        holding.Quantity += (int)message.Quantity;
                        holding.AveragePrice = holding.Quantity > 0 ? totalCost / holding.Quantity : 0;
                        _logger.LogInformation("Applied buy: UserId={UserId}, Ticker={Ticker}, NewQuantity={Quantity}, NewAveragePrice={AveragePrice}",
                            message.UserId, message.Ticker, holding.Quantity, holding.AveragePrice);
                    }
                    else if (message.Side == "sell")
                    {
                        holding.Quantity -= (int)message.Quantity;
                        if (holding.Quantity < 0) holding.Quantity = 0;
                        _logger.LogInformation("Applied sell: UserId={UserId}, Ticker={Ticker}, NewQuantity={Quantity}",
                            message.UserId, message.Ticker, holding.Quantity);
                    }

                    await _dbContext.SaveChangesAsync();
                }
            }
        }

        public async Task Consume(ConsumeContext<IPriceUpdatedEvent> context)
        {
            var message = context.Message;

            context.Headers.TryGetHeader("X-Correlation-ID", out var corrObj);
            var correlationId = corrObj?.ToString()
                ?? context.CorrelationId?.ToString()
                ?? Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString();

            using (var activity = ActivitySource.StartActivity("ConsumePriceUpdatedViaOrderExecuted", ActivityKind.Consumer))
            {
                activity?.AddTag("correlation_id", correlationId);
                activity?.AddTag("ticker", message.Ticker ?? string.Empty);

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["TraceId"] = activity?.TraceId.ToString(),
                    ["MessageTicker"] = message.Ticker
                }))
                {
                    _logger.LogInformation("Received PriceUpdatedEvent (via OrderExecutedConsumer): Ticker={Ticker}, Price={Price}, Timestamp={Timestamp}",
                        message.Ticker, message.Price, message.Timestamp);

                    var holding = await _dbContext.Holdings
                        .FirstOrDefaultAsync(h => h.Ticker == message.Ticker);

                    if (holding != null)
                    {
                        holding.AveragePrice = message.Price;
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("Updated holding average price: Ticker={Ticker}, NewAveragePrice={AveragePrice}", holding.Ticker, holding.AveragePrice);
                    }
                    else
                    {
                        _logger.LogWarning("No holding found for Ticker={Ticker} when processing price update", message.Ticker);
                    }
                }
            }
        }
    }
}
