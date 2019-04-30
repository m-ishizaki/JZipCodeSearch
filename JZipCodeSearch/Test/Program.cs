using RKSoftware.JZipCodeSearch;
using System;
using System.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                var result = ZipSearchClient.ZipToAddress("101").Result;
                Console.WriteLine(string.Join("\n", result.Select(a => a.ToString())));
            }

            {
                var result = ZipSearchClient.AddressToZip("千代田区神田花岡町").Result;
                Console.WriteLine(string.Join("\n", result.Select(a => a.ToString())));
            }

            Console.ReadKey();
        }
    }
}
