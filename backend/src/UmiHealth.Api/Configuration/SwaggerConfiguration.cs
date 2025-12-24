using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UmiHealth.Api.Configuration
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    new OpenApiInfo
                    {
                        Title = "Umi Health API",
                        Version = description.ApiVersion.ToString(),
                        Description = "Comprehensive pharmacy management system API with multi-tenant architecture",
                        Contact = new OpenApiContact
                        {
                            Name = "Umi Health Support",
                            Email = "support@umihealth.com",
                            Url = new Uri("https://umihealth.com/support")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT License",
                            Url = new Uri("https://opensource.org/licenses/MIT")
                        }
                    });
            }

            // Add JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML Comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add custom schema filters
            options.SchemaFilter<CustomSchemaFilters>();
        }
    }

    public class CustomSchemaFilters : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Add example values for common types
            if (schema.Type == "string" && schema.Format == "email")
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiString("user@example.com");
            }
            else if (schema.Type == "string" && schema.Format == "date-time")
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiDateTime(DateTime.UtcNow);
            }
            else if (schema.Type == "boolean")
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiBoolean(true);
            }
            else if (schema.Type == "integer")
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiInteger(1);
            }
            else if (schema.Type == "number")
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiFloat(99.99f);
            }
        }
    }
}
