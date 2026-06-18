using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiPdfCsv.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fiscal2csv_test;Username=postgres;Password=postgres",
                ["Jwt:Key"] = "integration-test-secret-key-minimum-32-chars",
                ["Jwt:Issuer"] = "ApiPdfCsv",
                ["Jwt:Audience"] = "ApiPdfCsv",
                ["Storage:Provider"] = "Local"
            });
        });
    }
}
