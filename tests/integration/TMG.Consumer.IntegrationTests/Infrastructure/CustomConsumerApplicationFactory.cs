using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TMG.Consumer.IntegrationTests.Infrastructure;

public sealed class CustomConsumerApplicationFactory(
    string postgresConnectionString,
    string redisConnectionString,
    string rabbitMqHostName,
    int rabbitMqPort,
    string rabbitMqUserName,
    string rabbitMqPassword,
    string rabbitMqVirtualHost) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresWrite"] = postgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = postgresConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Messaging:RabbitMq:ServiceName"] = "TMG.Consumer.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = rabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = rabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = rabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = rabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = rabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = "x.events.tmg.integrationtests",
                ["Messaging:RabbitMq:CommandsExchange"] = "x.commands.tmg.integrationtests",
                ["Notifications:Email:FromAddress"] = "no-reply@integrationtests.local",
                ["Notifications:Email:FromName"] = "TMG Integration Tests",
                ["OpenTelemetry:ServiceName"] = "TMG.Consumer.IntegrationTests",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddHostedService<TestReadyWorker>();
        });
    }
}
