namespace QuantAssembly.Analyst
{
    public class Company
    {
        public string Symbol { get; set; }
        public string Security { get; set; }
        public string GICSSector { get; set; }
        public string GICSSubIndustry { get; set; }
        public string HeadquartersLocation { get; set; }
        public DateTime Dateadded { get; set; }
        public string CIK { get; set; }
        public string Founded { get; set; }
        public int? FoundedYear { get; set; }
    }
}