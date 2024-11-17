using System;

namespace TradingBot.Models;

public abstract class ISignal
{
    public string Ticker { get; }
}

public class EntrySignal : ISignal
{

}

public class ExitSignal : ISignal
{
    
}
