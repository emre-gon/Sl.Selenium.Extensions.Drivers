<h3 align="center">Sl.Selenium.Extensions.Firefox</h3>

<div align="center">

[![NuGet](https://img.shields.io/nuget/v/Sl.Selenium.Extensions.Firefox.svg)](https://www.nuget.org/packages/Sl.Selenium.Extensions.Firefox)

</div>

---

Firefox Driver using Selenium.Extensions at https://github.com/emre-gon/Selenium.Extensions

Usage:


```cs
using (var driver = FirefoxDriver.Instance("profile_name"))
{
    driver.GoTo("https://google.com")
}
```