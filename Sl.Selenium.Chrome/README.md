<h3 align="center">Sl.Selenium.Extensions.Chrome</h3>

<div align="center">

[![NuGet](https://img.shields.io/nuget/v/Sl.Selenium.Extensions.Chrome.svg)](https://www.nuget.org/packages/Sl.Selenium.Extensions.Chrome)

</div>

---

Chrome Driver using Selenium.Extensions at https://github.com/emre-gon/Selenium.Extensions

Usage:


```cs
using (var driver = ChromeDriver.Instance("profile_name"))
{
    driver.GoTo("https://google.com")
}
```