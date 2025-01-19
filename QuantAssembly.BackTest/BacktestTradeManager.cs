using QuantAssembly.BackTesting.DataProvider;
using QuantAssembly.BackTesting.Utility;
using QuantAssembly.Ledger;
using QuantAssembly.Common.Logging;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;
using QuantAssembly.TradeManager;
using QuantAssembly.Utility;

namespace QuantAssembly.BackTesting.TradeManager
{
    public class BacktestTradeManager : ITradeManager
{
    private readonly ILedger ledger;
    private readonly ILogger logger;
    private readonly BacktestAccountDataProvider accountDataProvider;
    private readonly string accountId;
    private readonly TimeMachine timeMachine;

    public BacktestTradeManager(ILedger ledger, ILogger logger, BacktestAccountDataProvider accountDataProvider, TimeMachine timeMachine, string accountId)
    {
        this.ledger = ledger;
        this.logger = logger;
        this.accountDataProvider = accountDataProvider;
        this.accountId = accountId;
        this.timeMachine = timeMachine;
    }

    public async Task<TransactionResult> OpenPositionAsync(Position position, OrderType orderType)
    {
        Validator.AssertPropertiesNonNull(
            position,
            new List<string> {
                "PositionGuid",
                "Symbol",
                "InstrumentType",
                "State",
                "Currency",
                "CurrentPrice",
                "Quantity"
            }
        );

        logger.LogInfo($"[BacktestTradeManager::OpenPositionAsync] Attempting to open position:\n{position}\nOrderType:{orderType}");

        try
        {
            // Set the open price of the position
            position.OpenPrice = position.CurrentPrice;
            position.OpenTime = timeMachine.GetCurrentTime();

            // Simulate opening a position by recording it in the ledger
            ledger.AddOpenPosition(position);

            // Update account data
            var accountData = await accountDataProvider.GetAccountDataAsync(accountId);
            accountData.Equity += position.OpenPrice * position.Quantity;
            accountData.Liquidity -= position.OpenPrice * position.Quantity;
            accountDataProvider.SetAccountData(accountData);

            // Create a simulated transaction result
            var result = new TransactionResult
            {
                OrderId = Guid.NewGuid().ToString(),
                TransactionState = TransactionState.Completed,
            };

            logger.LogInfo($"[BacktestTradeManager::OpenPositionAsync] Order placed successfully. Order ID: {result.OrderId}, Avg Fill Price: {position.OpenPrice} positionId: {position.PositionGuid}");

            Validator.AssertPropertiesNonNull(
                position,
                new List<string> {
                    "State",
                    "Currency",
                    "PlatformOrderId",
                    "OpenTime",
                    "OpenPrice",
                    "CurrentPrice",
                    "Quantity"
                }
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"[BacktestTradeManager::OpenPositionAsync] Exception occurred while placing order for {position.Symbol}");
            logger.LogError(ex);
            throw;
        }
    }

    public async Task<TransactionResult> ClosePositionAsync(Position position, OrderType orderType)
    {
        Validator.AssertPropertiesNonNull(
            position,
            new List<string> {
                "PositionGuid",
                "PlatformOrderId",
                "Symbol",
                "State",
                "Currency",
                "OpenPrice",
                "CurrentPrice",
                "Quantity"
            }
        );

        logger.LogInfo($"[BacktestTradeManager::ClosePositionAsync] Attempting to close position:\n{position}\nOrderType:{orderType}");

        try
        {
            // Set the close price and close time of the position
            position.ClosePrice = position.CurrentPrice;
            position.CloseTime = timeMachine.GetCurrentTime();

            // Simulate closing a position by recording it in the ledger
            ledger.ClosePosition(position);

            // Update account data
            var accountData = await accountDataProvider.GetAccountDataAsync(accountId);
            accountData.Equity -= position.ClosePrice * position.Quantity;
            accountData.Liquidity += position.ClosePrice * position.Quantity;
            accountData.TotalPortfolioValue = accountData.Liquidity + accountData.Equity;
            accountDataProvider.SetAccountData(accountData);

            // Create a simulated transaction result
            var result = new TransactionResult
            {
                OrderId = Guid.NewGuid().ToString(),
                TransactionState = TransactionState.Completed,
            };

            logger.LogInfo($"[BacktestTradeManager::ClosePositionAsync] Order placed successfully. Order ID: {result.OrderId}, Avg Fill Price: {position.ClosePrice} positionId: {position.PositionGuid}");

            Validator.AssertPropertiesNonNull(
                position,
                new List<string> {
                    "PositionGuid",
                    "PlatformOrderId",
                    "Symbol",
                    "State",
                    "Currency",
                    "OpenPrice",
                    "CurrentPrice",
                    "Quantity",
                    "ClosePrice",
                    "CloseTime"
                }
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"[BacktestTradeManager::ClosePositionAsync] Exception occurred while placing order for {position.Symbol}");
            logger.LogError(ex);
            throw;
        }
    }

    public async Task<TransactionResult> GetTransactionStatusAsync()
    {
        // Simulate retrieving the transaction status
        var result = new TransactionResult
        {
            OrderId = Guid.NewGuid().ToString(),
            TransactionState = TransactionState.Completed
        };

        logger.LogInfo($"[BacktestTradeManager::GetTransactionStatusAsync] Transaction status retrieved. Order ID: {result.OrderId}, Status: {result.TransactionState}");

        return await Task.FromResult(result);
    }
}

}