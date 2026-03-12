using ERP.Api.Application;
using ERP.Api.Application.Security;
using ERP.Api.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ERP.Api.IntegrationTests;

public sealed class ErpApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IErpStore>();
            services.AddSingleton<IErpStore, InMemoryErpStore>();
            services.PostConfigure<WebhookOptions>(options =>
            {
                options.SharedSecret = "integration-webhook-secret";
                options.SignatureHeaderName = WebhookOptions.DefaultSignatureHeaderName;
            });
        });
    }
}
