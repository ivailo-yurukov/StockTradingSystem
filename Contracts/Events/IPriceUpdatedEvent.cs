using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Events
{
    public interface IPriceUpdatedEvent
    {
        string? Ticker { get; }
        decimal Price { get; }
        DateTime Timestamp { get; }
    }
}
