namespace QuantAssembly.Analyst
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var analyst = new Analyst();
            await analyst.Run();
        }
    }
}