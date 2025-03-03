using Alpaca.Markets;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;

namespace QuantAssembly.DataProvider
{
    public class AlpacaOptionsChainDataProvider : IOptionsChainDataProvider
    {
        private readonly AlpacaMarketsClient alpacaDataClient;
        private readonly ILogger logger;

        public AlpacaOptionsChainDataProvider(
            AlpacaMarketsClient alpacaDataClient,
            ILogger logger)
        {
            this.alpacaDataClient = alpacaDataClient;
            this.logger = logger;
        }

        public async Task<List<OptionsContractData>> GetOptionsChainDataAsync(string symbol, double minimumExpirationTimeInDays = 0)
        {
            logger.LogInfo($"[{nameof(AlpacaOptionsChainDataProvider)}] Getting options chain data for underlying asset: {symbol} with minimum expiration in days: {minimumExpirationTimeInDays}");
            var earliestExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(minimumExpirationTimeInDays));
            var optionsSnapshots = await alpacaDataClient.GetOptionsChainDataAsync(symbol, earliestExpirationDate);
            logger.LogInfo($"[{nameof(AlpacaOptionsChainDataProvider)}] Retrieved {optionsSnapshots.Count()} options contracts for underlying asset: {symbol}");

            var optionsContractDataList = new List<OptionsContractData>();

            foreach (var snapshot in optionsSnapshots)
            {
                try
                {
                    var contractSymbol = snapshot.Symbol;
                    var optionsContractDetails = await alpacaDataClient.GetOptionsContractDetails(contractSymbol);
                    var optionsContractData = new OptionsContractData
                    {
                        Symbol = symbol,
                        Name = optionsContractDetails.Name,
                        AskPrice = (double)snapshot.Quote.AskPrice,
                        BidPrice = (double)snapshot.Quote.BidPrice,
                        OptionType = optionsContractDetails.OptionType.ToString(), // "call" or "put"
                        StrikePrice = (double)optionsContractDetails.StrikePrice,
                        ExpirationDate = optionsContractDetails.ExpirationDate,
                        OpenInterest = (double)optionsContractDetails.OpenInterest,
                        ImpliedVolatility = (double)snapshot.ImpliedVolatility,
                        Delta = (double)(snapshot.Greeks?.Delta),
                        Gamma = (double)(snapshot.Greeks?.Gamma),
                        Theta = (double)(snapshot.Greeks?.Theta),
                        Vega = (double)(snapshot.Greeks?.Vega),
                        Rho = (double)(snapshot.Greeks?.Rho)
                    };

                    optionsContractDataList.Add(optionsContractData);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"[{nameof(AlpacaOptionsChainDataProvider)}] Exception occurred while creating OptionsContractDetails. Exception: {ex.Message}, InnerException: {ex.InnerException}");
                }
            }

            return optionsContractDataList;
        }
    }
}