using NUnit.Framework;
using Sl.Selenium.Extensions;
using System;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            ChromeDriver.KillAllChromeProcesses();
        }

        [TearDown]
        public void TearDown()
        {
            ChromeDriver.KillAllChromeProcesses();
        }

        
        [Test]
        public void Test_Chrome_Directory_Copy_Disabled_Throws_Error()
        {
            ChromeDriver.ENABLE_MULTI_PROFILE_DIRECTORY_COPY = false;

            try
            {
                using (var driver = ChromeDriver.Instance("test1"))
                {
                    driver.GoTo("https://google.com");

                    using (var driver2 = ChromeDriver.Instance("test2"))
                    {
                        driver2.GoTo("https://google.com");
                    }
                }

                Assert.IsTrue(false);
            }
            catch (Exception ex)            
            {
                var bx = ex.GetBaseException();

                StringAssert.Contains("user data directory is already in use", bx.Message);
            }
        }


        [Test]
        public void Test_Chrome_Directory_Copy_Enabled()
        {
            ChromeDriver.ENABLE_MULTI_PROFILE_DIRECTORY_COPY = true;

            try
            {
                using (var driver = ChromeDriver.Instance("test1"))
                {
                    driver.GoTo("https://google.com");

                    using (var driver2 = ChromeDriver.Instance("test2"))
                    {
                        driver2.GoTo("https://google.com");
                    }
                }

                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(false); //should not throw exception
                throw;
            }
        }
    }
}