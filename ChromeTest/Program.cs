using Sl.Selenium.Extensions.Chrome;
using Sl.Selenium.Extensions.Firefox;
using System;

namespace ChromeTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var driver = ChromeDriver.Instance())
            {
                driver.GoTo("https://google.com");
            }


            using (var driver = FirefoxDriver.Instance())
            {
                driver.GoTo("https://google.com");
            }


            Console.ReadLine();
        }
    }
}
