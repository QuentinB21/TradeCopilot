using System.Text.Json.Serialization;
using TradeCopilot.Application;
using TradeCopilot.Infrastructure;

namespace TradeCopilot.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradeCopilotApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddCors(options =>
        {
            options.AddPolicy("local-client", policy =>
                policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        services.AddTradeCopilotApplication();
        services.AddTradeCopilotInfrastructure(configuration);

        return services;
    }
}
