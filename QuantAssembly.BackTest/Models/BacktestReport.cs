namespace QuantAssembly.BackTesting.Models
{
    public class BacktestReport
    {
        public string StrategyName { get; set; }
        public int StopLossConditionsHit { get; set; }
        public int ExitConditionsHit { get; set; }
        public int EntryConditionsHit { get; set; }
        public int TakeProfitConditionsHit { get; set; }

        public override string ToString()
        {
            return $"Strategy Name: {StrategyName}\n" +
                   $"Stop Loss Conditions Hit: {StopLossConditionsHit}\n" +
                   $"Exit Conditions Hit: {ExitConditionsHit}\n" +
                   $"Entry Conditions Hit: {EntryConditionsHit}\n" +
                   $"Take Profit Conditions Hit: {TakeProfitConditionsHit}";
        }
    }

}