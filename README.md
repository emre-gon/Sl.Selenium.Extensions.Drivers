<h3 align="center">Sl.Selenium.Extensions.Firefox</h3>

<div align="center">

[![Status](https://img.shields.io/badge/status-active-success.svg)]()
[![License](https://img.shields.io/github/license/emre-gon/Sl.Selenium.Extensions.Firefox)](/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Sl.Selenium.Extensions.Firefox.svg)](https://www.nuget.org/packages/Sl.Selenium.Extensions.Firefox)

</div>

---

Firefox Driver using Selenium.Extensions at https://github.com/emre-gon/Selenium.Extensions

Usage:


```cs
using (var driver = SlFirefoxDriver.Instance("profile_name"))
{
    driver.GoTo("https://google.com")
}
```