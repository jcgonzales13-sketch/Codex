using ERP.Api.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ERP.Api.UnitTests;

public sealed class RuntimeConfigurationValidatorTests
{
    [Fact]
    public void Deve_aceitar_json_file_em_development()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "JsonFile",
            ["Storage:FilePath"] = "App_Data/dev.json",
            ["Jwt:SigningKey"] = "CHANGE_ME_DEVELOPMENT_ONLY_SIGNING_KEY_123456789"
        });

        RuntimeConfigurationValidator.Validate(configuration, new FakeHostEnvironment("Development"));
    }

    [Fact]
    public void Deve_rejeitar_provider_nao_sqlserver_em_production()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "JsonFile",
            ["Storage:FilePath"] = "/data/app.json",
            ["Jwt:SigningKey"] = "segredo-forte-producao"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RuntimeConfigurationValidator.Validate(configuration, new FakeHostEnvironment("Production")));

        Assert.Contains("Storage:Provider", exception.Message);
    }

    [Fact]
    public void Deve_rejeitar_signing_key_padrao_em_production()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "SqlServer",
            ["Storage:ConnectionString"] = "Server=sql;Database=erp;User Id=app;Password=secret;",
            ["Jwt:SigningKey"] = "CHANGE_ME_DEVELOPMENT_ONLY_SIGNING_KEY_123456789"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RuntimeConfigurationValidator.Validate(configuration, new FakeHostEnvironment("Production")));

        Assert.Contains("Jwt:SigningKey", exception.Message);
    }

    [Fact]
    public void Deve_rejeitar_sqlserver_sem_connection_string()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "SqlServer",
            ["Jwt:SigningKey"] = "segredo-forte"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RuntimeConfigurationValidator.Validate(configuration, new FakeHostEnvironment("Development")));

        Assert.Contains("Storage:ConnectionString", exception.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "ERP.Api.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
