using Microsoft.Extensions.Options;
using AspNetCoreCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace TMG.WebAPI.Infrastructure.Cors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.AddCors();
        services.AddSingleton<IConfigureOptions<AspNetCoreCorsOptions>>(serviceProvider =>
            new ConfigureOptions<AspNetCoreCorsOptions>(corsOptions =>
            {
                var allowedOrigins = serviceProvider
                    .GetRequiredService<IOptions<CorsOptions>>().Value.AllowedOrigins;

                // No configured origins means no cross-origin caller is allowed; the policy
                // still exists so UseCors can be wired unconditionally.
                corsOptions.AddPolicy(CorsPolicyNames.Frontend, policy =>
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            }));

        return services;
    }
}
