using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    /// <summary>
    /// Retrieves the list of companies to analyze and does any filtration that can be done
    /// based on Company data.
    /// </summary>
    [PipelineStep]
    [PipelineStepOutput(nameof(AnalystContext.companies))]
    public class InitStep : IPipelineStep<AnalystContext>
    {
        private const string csvUrl = "https://raw.githubusercontent.com/datasets/s-and-p-500-companies-financials/refs/heads/main/data/constituents-financials.csv";

        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(InitStep)}] Downloading extended S&P 500 company data");

            var analystConfig = config as Models.Config;
            var companyDataFilterConfig = analystConfig.companyDataFilterConfig;
            List<Company> companies = new List<Company>();

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(csvUrl))
            using (var reader = new StreamReader(response.Content.ReadAsStream()))
            {
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    PrepareHeaderForMatch = args => args.Header
                        .Replace(" ", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace("/", string.Empty),
                };

                var csv = new CsvReader(reader, csvConfig);

                await csv.ReadAsync();
                csv.ReadHeader();

                while (await csv.ReadAsync())
                {
                    try
                    {
                        var company = new Company
                        {
                            Symbol = csv.GetField("Symbol"),
                            Name = csv.GetField("Name"),
                            Sector = csv.GetField("Sector"),
                            Price = ParseDouble(csv.GetField("Price")),
                            PriceToEarningsRatio = ParseDouble(csv.GetField("P/E")),
                            DividendYield = ParseDouble(csv.GetField("Dividend %")),
                            EarningsPerShare = ParseDouble(csv.GetField("EPS")),
                            YearlyLow = ParseDouble(csv.GetField("52 Week Low")),
                            YearlyHigh = ParseDouble(csv.GetField("52 Week High")),
                            MarketCap = ParseMarketCap(csv.GetField("Market Cap")),
                            EBITDA = ParseDouble(csv.GetField("EBITDA")),
                            PriceToSalesRatio = ParseDouble(csv.GetField("Price/Sales")),
                            PriceToBookRatio = ParseDouble(csv.GetField("Price/Book")),
                            SECFilings = csv.GetField("SEC Filings")
                        };

                        companies.Add(company);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarn($"Skipping row due to parse error: {ex.Message}");
                    }
                }
            }

            logger.LogInfo($"[{nameof(InitStep)}] Parsed {companies.Count} companies from CSV.");

            // Apply all configured filters
            companies = companies.Where(c =>
                (companyDataFilterConfig.MinimumMarketCap == null || c.MarketCap >= companyDataFilterConfig.MinimumMarketCap) &&
                (companyDataFilterConfig.MaximumMarketCap == null || c.MarketCap <= companyDataFilterConfig.MaximumMarketCap) &&

                (companyDataFilterConfig.MinimumPERatio == null || c.PriceToEarningsRatio >= companyDataFilterConfig.MinimumPERatio) &&
                (companyDataFilterConfig.MaximumPERatio == null || c.PriceToEarningsRatio <= companyDataFilterConfig.MaximumPERatio) &&

                (companyDataFilterConfig.MinimumDividendYield == null || c.DividendYield >= companyDataFilterConfig.MinimumDividendYield) &&
                (companyDataFilterConfig.MaximumDividendYield == null || c.DividendYield <= companyDataFilterConfig.MaximumDividendYield) &&

                (companyDataFilterConfig.MinimumEPS == null || c.EarningsPerShare >= companyDataFilterConfig.MinimumEPS) &&
                (companyDataFilterConfig.MaximumEPS == null || c.EarningsPerShare <= companyDataFilterConfig.MaximumEPS) &&

                (companyDataFilterConfig.MinimumPriceToSalesRatio == null || c.PriceToSalesRatio >= companyDataFilterConfig.MinimumPriceToSalesRatio) &&
                (companyDataFilterConfig.MaximumPriceToSalesRatio == null || c.PriceToSalesRatio <= companyDataFilterConfig.MaximumPriceToSalesRatio) &&

                (companyDataFilterConfig.MinimumPriceToBookRatio == null || c.PriceToBookRatio >= companyDataFilterConfig.MinimumPriceToBookRatio) &&
                (companyDataFilterConfig.MaximumPriceToBookRatio == null || c.PriceToBookRatio <= companyDataFilterConfig.MaximumPriceToBookRatio)
            ).ToList();

            if (companyDataFilterConfig.Sectors.Any())
            {
                companies = companies.Where(c => companyDataFilterConfig.Sectors.Contains(c.Sector)).ToList();
            }

            context.companies = companies;
            logger.LogInfo($"[{nameof(InitStep)}] After filtration, {companies.Count} companies remain.");
        }

        private double ParseDouble(string input)
        {
            return double.TryParse(input?.Replace("%", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private double ParseMarketCap(string marketCapStr)
        {
            if (string.IsNullOrWhiteSpace(marketCapStr)) return 0;

            marketCapStr = marketCapStr.Trim().ToUpperInvariant();

            if (marketCapStr.EndsWith("B") && double.TryParse(marketCapStr[..^1], out var b))
                return b * 1_000_000_000;
            if (marketCapStr.EndsWith("M") && double.TryParse(marketCapStr[..^1], out var m))
                return m * 1_000_000;
            if (marketCapStr.EndsWith("T") && double.TryParse(marketCapStr[..^1], out var t))
                return t * 1_000_000_000_000;

            return double.TryParse(marketCapStr, out var raw) ? raw : 0;
        }
    }

}