using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TradeCopilot.Api.Security;
using TradeCopilot.Application.Abstractions;
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
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddCors(options =>
        {
            options.AddPolicy("local-client", policy =>
                policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
        services.AddTradeCopilotAuthentication(configuration);

        services.AddTradeCopilotApplication();
        services.AddTradeCopilotInfrastructure(configuration);

        return services;
    }

    private static IServiceCollection AddTradeCopilotAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
        {
            services.AddAuthorization();
            return services;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                var metadataAddress = configuration["Authentication:MetadataAddress"];
                if (!string.IsNullOrWhiteSpace(metadataAddress))
                {
                    options.MetadataAddress = metadataAddress;
                }

                var backchannelAuthority = configuration["Authentication:BackchannelAuthority"];
                if (!string.IsNullOrWhiteSpace(backchannelAuthority))
                {
                    options.BackchannelHttpHandler = new KeycloakBackchannelHandler(authority, backchannelAuthority);
                }

                options.RequireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);

                var audience = configuration["Authentication:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateIssuer = true
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }

    private sealed class KeycloakBackchannelHandler(string publicAuthority, string backchannelAuthority) : HttpClientHandler
    {
        private readonly Uri publicAuthorityUri = new(publicAuthority.TrimEnd('/'));
        private readonly Uri backchannelAuthorityUri = new(backchannelAuthority.TrimEnd('/'));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null &&
                publicAuthorityUri.IsBaseOf(request.RequestUri))
            {
                var relativePath = publicAuthorityUri.MakeRelativeUri(request.RequestUri);
                request.RequestUri = new Uri(backchannelAuthorityUri, relativePath);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
