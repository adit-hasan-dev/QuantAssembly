﻿namespace QuantAssembly
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var quant = new Quant();

            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                quant.Terminate();
            };
            quant.Run();
        }
    }
}
