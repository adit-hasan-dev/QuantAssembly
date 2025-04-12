using System.Text.Json;
using System.Text.Json.Nodes;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common
{
    public class Utility
    {
        public static TimeSpan GetTimeSpanFromTimePeriod(TimePeriod timePeriod)
        {
            return timePeriod switch
            {
                TimePeriod.Day => TimeSpan.FromDays(1),
                TimePeriod.FiveDays => TimeSpan.FromDays(5),
                TimePeriod.OneMonth => TimeSpan.FromDays(30),
                TimePeriod.ThreeMonths => TimeSpan.FromDays(90),
                TimePeriod.SixMonths => TimeSpan.FromDays(180),
                TimePeriod.OneYear => TimeSpan.FromDays(365),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static string ReadJsonAsMinifiedString(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("The specified JSON file was not found.", fileName);
            }

            string jsonString = File.ReadAllText(fileName);
            var jsonNode = JsonNode.Parse(jsonString);
            return jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        public static double CalculateStandardDeviation(IEnumerable<double> dataset, bool sample = false)
        {
            List<double> squaredDistances = new List<double>();
            double meanSquaredDistances = 0;

            int datapointCount = sample ? dataset.Count() - 1 : dataset.Count();
            double mean = dataset.Sum() / datapointCount;

            foreach (double datapoint in dataset)
            {
                double distance = Math.Pow(Math.Abs(datapoint - mean), 2);
                squaredDistances.Add(distance);
            }

            meanSquaredDistances = squaredDistances.Sum() / datapointCount;

            return Math.Sqrt(meanSquaredDistances);
        }
    }
}