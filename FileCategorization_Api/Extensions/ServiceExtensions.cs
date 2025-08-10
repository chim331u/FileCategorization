using System.Reflection;
using System.Text;
using FileCategorization_Api.AppContext;
using FileCategorization_Api.Exceptions;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Models.Identity;
using FileCategorization_Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FileCategorization_Api.Extensions;

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

        // Adding the database context
        builder.Services.AddDbContext<ApplicationContext>(configure =>
        {
            configure.UseSqlite(builder.Configuration.GetConnectionString("sqliteConnection"));
        });

        // Register services
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

        // For Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationContext>()
            .AddDefaultTokenProviders();

        //Create a symmetric security key using the secret key from the configuration.
        SymmetricSecurityKey authSigningKey;
            
        if (builder.Configuration.GetSection("IsDev").Value != null)
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