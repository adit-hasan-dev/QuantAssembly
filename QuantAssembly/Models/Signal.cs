using System;

namespace QuantAssembly.Models;

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
