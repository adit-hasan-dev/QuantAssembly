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
        private const string csvUrl = "https://raw.githubusercontent.com/datasets/s-and-p-500-companies/refs/heads/main/data/constituents.csv";
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(InitStep)}] Calling API to retrieve company data for S&P500");
            
            var analystConfig = config as Models.Config;
            var companyDataFilterConfig = analystConfig.companyDataFilterConfig;
            List<Company> companies = new List<Company>();
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(csvUrl))
            using (var reader = new StreamReader(response.Content.ReadAsStream()))
            {
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header
                        .Replace(" ", string.Empty)
                        .Replace("-", string.Empty),
                };
                var csv = new CsvReader(reader, csvConfig);
                companies = csv.GetRecords<Company>()
                    .ToList();
            }
            logger.LogInfo($"[{nameof(InitStep)}] Retrieve company data for {companies.Count} companies");

            int minAge = companyDataFilterConfig.minimumCompanyAge ?? 0;
            int maxAge = companyDataFilterConfig.maximumCompanyAge ?? 1000;
            companies = companies.Where(c => {
                int foundedYear = ParseFoundedYear(c.Founded);
                int age = DateTime.UtcNow.Year - foundedYear;
                return age >= minAge && age <= maxAge;
            }).ToList();

            // Leaving the sectors and/or subIndustries empty means no filtering by these fields.
            if (companyDataFilterConfig.sectors.Any())
            {
                companies = companies.Where(c => companyDataFilterConfig.sectors.Contains(c.GICSSector)).ToList();
            }

            if (companyDataFilterConfig.subIndustries.Any())
            {
                companies = companies.Where(c => companyDataFilterConfig.subIndustries.Contains(c.GICSSubIndustry)).ToList();
            }
            context.companies = companies;
            logger.LogInfo($"[{nameof(InitStep)}] After filtration, {companies.Count} companies are remaining.");
        }

        private int ParseFoundedYear(string foundedField)
        {
            // The field might be a simple year like "1902" or a combination like "2013 (1888)".
            // Here we extract the first 4-digit number.
            var digits = new string(foundedField.Where(char.IsDigit).ToArray());
            if (digits.Length >= 4 && int.TryParse(digits.Substring(0, 4), out int year))
            {
                return year;
            }
            return DateTime.Now.Year; // Fallback if parsing fails.
        }
    }
}