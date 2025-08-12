using System.Reflection;
using System.Text;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.Identity;
using FileCategorization_Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FileCategorization_Api.Common;

/// <summary>
/// Provides extension methods for configuring application services.
/// </summary>
public static class ServiceExtensions
{

    /// <summary>
    /// Adds application-specific services to the specified <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or its configuration is null.</exception>
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (builder.Configuration == null) throw new ArgumentNullException(nameof(builder.Configuration));

        // Adding the database context with memory optimizations
        builder.Services.AddDbContext<ApplicationContext>(configure =>
        {
            configure.UseSqlite(builder.Configuration.GetConnectionString("sqliteConnection"));
            configure.EnableSensitiveDataLogging(false);
            configure.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
        });

        // Register repositories
        builder.Services.AddScoped(typeof(IRepository<>), typeof(FileCategorization_Api.Infrastructure.Data.Repositories.Repository<>));
        builder.Services.AddScoped<IFilesDetailRepository, FileCategorization_Api.Infrastructure.Data.Repositories.FilesDetailRepository>();
        builder.Services.AddScoped<IUtilityRepository, FileCategorization_Api.Infrastructure.Repositories.UtilityRepository>();
        builder.Services.AddScoped<IConfigRepository, FileCategorization_Api.Infrastructure.Data.Repositories.ConfigRepository>();

        // Register new services
        builder.Services.AddScoped<IFilesQueryService, FilesQueryService>();
        builder.Services.AddScoped<IConfigQueryService, ConfigQueryService>();

        // Register existing services
        builder.Services.AddScoped<IFilesDetailService, FilesDetailService>();
        builder.Services.AddScoped<IConfigsService, ConfigsService>();
        builder.Services.AddScoped<IUtilityServices, UtilityServices>();
        builder.Services.AddScoped<IHangFireJobService, HangFireJobService>();
        builder.Services.AddScoped<IMachineLearningService, MachineLearningService>();
        builder.Services.AddScoped<IDDService, DDService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IIdentityService, IdentityService>();
            
        // Adding validators from the current assembly
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add AutoMapper with profiles from the current assembly
        builder.Services.AddAutoMapper(typeof(FilesDetailProfile), typeof(ConfigProfile));

        // Register ILogger for endpoint injection
        builder.Services.AddSingleton<ILogger>(provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger("EndpointLogger"));

        // For Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationContext>()
            .AddDefaultTokenProviders();

        //Create a symmetric security key using the secret key from the configuration.
        SymmetricSecurityKey authSigningKey;
            
        if (builder.Environment.IsDevelopment())
        {
            //for debug only
            authSigningKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]));

        }
        else
        {
            authSigningKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")));
        }

        builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            )
            .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        ClockSkew = TimeSpan.Zero
                        , IssuerSigningKey = authSigningKey
                    };
                }
            );





        // Register exception handler
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        // Add problem details for standardized error responses
        builder.Services.AddProblemDetails();
    }
}