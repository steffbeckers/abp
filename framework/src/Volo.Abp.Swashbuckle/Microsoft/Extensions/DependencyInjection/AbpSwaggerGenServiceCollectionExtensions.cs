﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Volo.Abp.Content;

namespace Microsoft.Extensions.DependencyInjection;

public static class AbpSwaggerGenServiceCollectionExtensions
{
    public static IServiceCollection AddAbpSwaggerGen(
        this IServiceCollection services,
        Action<SwaggerGenOptions> setupAction = null)
    {
        return services.AddSwaggerGen(
            options =>
            {
                Func<OpenApiSchema> remoteStreamContentSchemaFactory = () => new OpenApiSchema()
                {
                    Type = "string",
                    Format = "binary"
                };

                options.MapType<RemoteStreamContent>(remoteStreamContentSchemaFactory);
                options.MapType<IRemoteStreamContent>(remoteStreamContentSchemaFactory);

                setupAction?.Invoke(options);
            });
    }

    public static IServiceCollection AddAbpSwaggerGenWithOAuth(
        this IServiceCollection services,
        [NotNull] string authority,
        [NotNull] Dictionary<string, string> scopes,
        Action<SwaggerGenOptions> setupAction = null,
        string authorizationEndpoint = "/connect/authorize",
        string tokenEndpoint = "/connect/token")
    {
        var authorizationUrl = new Uri($"{authority.TrimEnd('/')}{authorizationEndpoint.EnsureStartsWith('/')}");
        var tokenUrl = new Uri($"{authority.TrimEnd('/')}{tokenEndpoint.EnsureStartsWith('/')}");
        
        return services
            .AddAbpSwaggerGen()
            .AddSwaggerGen(
                options =>
                {
                    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = authorizationUrl,
                                Scopes = scopes,
                                TokenUrl = tokenUrl
                            }
                        }
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "oauth2"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });

                    setupAction?.Invoke(options);
                });
    }

    public static IServiceCollection AddAbpSwaggerGenWithOidc(
        this IServiceCollection services,
        [NotNull] string authority,
        Action<SwaggerGenOptions> setupAction = null,
        List<string> flows = null,
        string openIdConnectDiscoveryEndpoint = "/.well-known/openid-configuration")
    {
        var openIdConnectDiscoveryUrl = new Uri($"{authority.TrimEnd('/')}{openIdConnectDiscoveryEndpoint.EnsureStartsWith('/')}");
        flows ??= new List<string> { "authorization_code"};

        services.Configure<SwaggerUIOptions>(swaggerUiOptions =>
        {
            swaggerUiOptions.ConfigObject.AdditionalItems["oidcSupportedFlows"] = flows;
        });
        
        return services
            .AddAbpSwaggerGen()
            .AddSwaggerGen(
                options =>
                {
                    options.AddSecurityDefinition("oidc", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OpenIdConnect,
                        OpenIdConnectUrl = openIdConnectDiscoveryUrl,
                        
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "oidc"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                    setupAction?.Invoke(options);
                });
    }
}
