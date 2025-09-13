using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Events
{
    public interface IOrderExecutedEvent
    {
        string UserId { get; }
        string Ticker { get; }
        decimal Quantity { get; }
        string Side { get; }
        decimal Price { get; }
        DateTime ExecutedAt { get; }
    }
}
