using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace QuantAssembly.Analyst.LLM
{
    public class MathPlugin
    {
        [KernelFunction("calculate_contract_cost")]
        [Description("Calculates the total capital required for an options contract. Formula: AskPrice * 100 * Quantity.")]
        public double CalculateContractCost(
            [Description("The ask price of the option contract.")] double askPrice,
            [Description("The number of contracts to buy.")] int quantity)
        {
            return askPrice * 100 * quantity;
        }

        [KernelFunction("calculate_total_invested")]
        [Description("Calculates the sum of multiple contract costs.")]
        public double CalculateTotalInvested(
            [Description("An array of contract costs.")] double[] contractCosts)
        {
            return contractCosts.Sum();
        }

        [KernelFunction("calculate_max_profit")]
        [Description("Calculates the maximum profit for a single contract position. Formula: (TakeProfitLevel - AskPrice) * 100 * Quantity.")]
        public double CalculateMaxProfit(
            [Description("Take profit level for the option contract.")] double takeProfitLevel,
            [Description("The ask price of the option contract.")] double askPrice,
            [Description("The number of contracts to buy.")] int quantity)
        {
            return (takeProfitLevel - askPrice) * 100 * quantity;
        }

        [KernelFunction("calculate_max_loss")]
        [Description("Calculates the maximum loss for a single contract position. Formula: (AskPrice - StopLossLevel) * 100 * Quantity.")]
        public double CalculateMaxLoss(
            [Description("Stop loss level for the option contract.")] double stopLossLevel,
            [Description("The ask price of the option contract.")] double askPrice,
            [Description("The number of contracts to buy.")] int quantity)
        {
            return (askPrice - stopLossLevel) * 100 * quantity;
        }

        [KernelFunction("calculate_total_max_profit")]
        [Description("Calculates the total maximum profit for all recommended option contracts by summing the individual max profits for each contract.")]
        public double CalculateTotalMaxProfit(
            [Description("An array of individual contract max profits.")] double[] contractMaxProfits)
        {
            return contractMaxProfits.Sum();
        }

        [KernelFunction("calculate_total_max_loss")]
        [Description("Calculates the total maximum loss for all recommended option contracts by summing the individual max losses for each contract.")]
        public double CalculateTotalMaxLoss(
            [Description("An array of individual contract max losses.")] double[] contractMaxLosses)
        {
            return contractMaxLosses.Sum();
        }

    }
}
