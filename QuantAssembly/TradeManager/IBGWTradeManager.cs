using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Ledger;
using QuantAssembly.Common.Logging;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;
using Validator = QuantAssembly.Utility.Validator;

namespace QuantAssembly.TradeManager
{
    public class IBGWTradeManager : ITradeManager
    {
        private IIBGWClient client;
        private ILogger logger;
        private ILedger ledger;
        public IBGWTradeManager(ServiceProvider provider)
        {
            this.client = provider.GetRequiredService<IIBGWClient>();
            this.logger = provider.GetRequiredService<ILogger>();
            this.ledger = provider.GetRequiredService<ILedger>();
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

            logger.LogInfo($"[IBGWTradeManager::ClosePositionAsync] Attempting to close position:\n{position}nOrderType:{orderType}");

            try
            {
                logger.LogInfo($"[IBGWTradeManager::ClosePositionAsync] Placing a sell order for opened position:\n{position}");
                var result = await this.client.PlaceOrder(position, orderType, OrderAction.Sell);

                // Log the result of the order
                if (result.TransactionState == TransactionState.Completed)
                {
                    logger.LogInfo($"[IBGWTradeManager::ClosePositionAsync] Order placed successfully. Order ID: {result.OrderId}, Avg Fill Price: {position.OpenPrice} positionId: {position.PositionGuid}");
                    ledger.ClosePosition(position);
                }
                else
                {
                    logger.LogError($"[IBGWTradeManager::ClosePositionAsync] Order placement failed or was canceled. Order ID: {result.OrderId}, Status: {result.TransactionState} positionId: {position.PositionGuid}");
                }

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

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"[IBGWTradeManager::OpenPositionAsync] Exception occurred while placing order for {position.Symbol}");
                logger.LogError(ex);
                throw;
            }
        }

        public async Task<TransactionResult> GetTransactionStatusAsync()
        {
            throw new NotImplementedException();
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
                    "OpenPrice",
                    "CurrentPrice",
                    "Quantity"
                }
            );

            logger.LogInfo($"[IBGWTradeManager::OpenPositionAsync] Attempting to open position:\n{position}\nOrderType:{orderType}");

            try
            {
                logger.LogInfo($"[IBGWTradeManager::OpenPositionAsync] Placing order for {position.Symbol} with quantity {position.Quantity} and order type {orderType} positionId: {position.PositionGuid}");
                var result = await this.client.PlaceOrder(position, orderType, OrderAction.Buy);

                if (result.TransactionState == TransactionState.Completed)
                {
                    logger.LogInfo($"[IBGWTradeManager::OpenPositionAsync] Order placed successfully. Order ID: {result.OrderId}, Avg Fill Price: {position.OpenPrice} positionId: {position.PositionGuid}");
                    ledger.AddOpenPosition(position);
                }
                else
                {
                    logger.LogError($"[IBGWTradeManager::OpenPositionAsync] Order placement failed or was canceled. Order ID: {result.OrderId}, Status: {result.TransactionState} positionId: {position.PositionGuid}");
                }

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

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"[IBGWTradeManager::OpenPositionAsync] Exception occurred while placing order for {position.Symbol}");
                logger.LogError(ex);
                throw;
            }
        }

    }
}