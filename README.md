<h3 align="center">Sl.Selenium.Extensions.Drivers</h3>

<div align="center">

[![Status](https://img.shields.io/badge/status-active-success.svg)]()
[![License](https://img.shields.io/github/license/emre-gon/Sl.Selenium.Extensions.Drivers)](/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Sl.Selenium.Extensions.Firefox.svg)](https://www.nuget.org/packages/Sl.Selenium.Extensions.Firefox)
[![NuGet](https://img.shields.io/nuget/v/Sl.Selenium.Extensions.Chrome.svg)](https://www.nuget.org/packages/Sl.Selenium.Extensions.Chrome)


</div>

---

Firefox and Chrome Drivers using Selenium.Extensions at https://github.com/emre-gon/Selenium.Extensions

Automatically downloads latest drivers.

Usage:


```cs
using (var driver = FirefoxDriver.Instance("profile_name"))
{
    driver.GoTo("https://google.com")
}


using (var driver = ChromeDriver.Instance("profile_name"))
{
    driver.GoTo("https://google.com")
}
```