using FileCategorization_Api.Domain.Entities.DD_Web;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// ReSharper disable All

namespace FileCategorization_Api.Infrastructure.Data;

/// <summary>
/// Represents the database context for the application.
/// </summary>
public class ApplicationContext(DbContextOptions<ApplicationContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    // // Default schema for the database context
    // private const string DefaultSchema = "fc_minimalApi";

    /// <summary>
    /// Gets or sets the DbSet representing the collection of file details in the database.
    /// </summary>
    public DbSet<FilesDetail> FilesDetail { get; set; }

    /// <summary>
    /// Gets or sets the DbSet representing the collection of configurations in the database.
    /// </summary>
    public DbSet<Configs> Configuration { get; set; }
        
    /// <summary>
    /// Gets or sets the DbSet representing the collection of DD_Threads in the database.
    /// </summary>
    public DbSet<DD_Threads> DDThreads { get; set; }
        
    /// <summary>
    /// Gets or sets the DbSet representing the collection of DD_LinkEd2k in the database.
    /// </summary>
    public DbSet<DD_LinkEd2k> DDLinkEd2 { get; set; }
        
        
    public  DbSet<TokenInfo> TokenInfo { get; set; }
        
    /// <summary>
    /// Configures the model and relationships for the database context.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for the database context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //modelBuilder.HasDefaultSchema(DefaultSchema);
            
        // Apply configurations from the current assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);

        // // Apply configurations from the current assembly again (duplicate call, consider removing if unnecessary).
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);
    }
}