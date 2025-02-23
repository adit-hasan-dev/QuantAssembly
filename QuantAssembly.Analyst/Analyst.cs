using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;

namespace QuantAssembly.Analyst
{
    public class Analyst
    {
        private readonly ILogger logger;
        private readonly IConfig config;
        private List<Company> companies = new List<Company>();

        public Analyst()
        {
            //this.config = new Config();
            //this.logger = new Logger(config, isDevEnv: true);
        }

        public async Task Run()
        {
            await RetrieveCompanies();
            // Filter by age
            // Filter by sector
            // Filter by market cap
            // Filter by volume
            // Filter by price

        }

        public async Task RetrieveCompanies()
        {
            string csvUrl = "https://raw.githubusercontent.com/datasets/s-and-p-500-companies/refs/heads/main/data/constituents.csv";

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(csvUrl))
            using (var reader = new StreamReader(response.Content.ReadAsStream()))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header
                        .Replace(" ", string.Empty)
                        .Replace("-", string.Empty),
                };
                var csv = new CsvReader(reader, config);
                companies = csv.GetRecords<Company>()
                    .ToList();

                foreach (var company in companies)
                {
                    company.FoundedYear = ParseFoundedYear(company.Founded);
                }
            }
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