using System.Collections.Concurrent;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MemoLens.Data;
using MemoLens.Models.Auth;
using MemoLens.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace MemoLens.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtIssuer = "MemoLens.Testing";
    private const string JwtAudience = "MemoLens.Mobile.Testing";
    private const string JwtSecretKey = "MemoLens-Testing-Only-Key-At-Least-32-Bytes-2026";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _testContentRootPath = Path.Combine(
        Path.GetTempPath(),
        "MemoLens.Tests",
        Guid.NewGuid().ToString("N"));

    public TestEmailSender EmailSender => Services.GetRequiredService<TestEmailSender>();

    public string TestUploadRootPath => Path.Combine(_testContentRootPath, "App_Data", "uploads");

    protected virtual string TestEnvironmentName => "Testing";

    public string CreateTestAccessToken(
        string? userId,
        DateTime expiresAtUtc,
        string? signingSecret = null,
        string? email = null,
        IReadOnlyCollection<string>? roles = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        if (roles is not null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret ?? JwtSecretKey));
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: expiresAtUtc.AddMinutes(-5),
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void ClearTestUploadStorage()
    {
        if (Directory.Exists(_testContentRootPath))
        {
            Directory.Delete(_testContentRootPath, recursive: true);
        }

        Directory.CreateDirectory(_testContentRootPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironmentName);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SecretKey"] = JwtSecretKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "30",
                ["RefreshTokenCleanup:Enabled"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<TestEmailSender>();
            services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<TestEmailSender>());

            services.RemoveAll<IImageStorageService>();
            services.AddSingleton<IImageStorageService>(_ => new LocalImageStorageService(
                new IsolatedWebHostEnvironment(_testContentRootPath)));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = JwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { IdentitySeedData.AdminRole, IdentitySeedData.UserRole })
        {
            if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                var result = roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create the {roleName} test role.");
                }
            }
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();

            if (Directory.Exists(_testContentRootPath))
            {
                Directory.Delete(_testContentRootPath, recursive: true);
            }
        }

        base.Dispose(disposing);
    }

    public sealed class TestEmailSender : IEmailSender
    {
        private readonly ConcurrentQueue<CapturedEmail> _messages = new();

        public IReadOnlyCollection<CapturedEmail> Messages => _messages.ToArray();

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _messages.Enqueue(new CapturedEmail(email, subject, htmlMessage));
            return Task.CompletedTask;
        }

        public void Clear()
        {
            while (_messages.TryDequeue(out _))
            {
            }
        }
    }

    public sealed record CapturedEmail(string Email, string Subject, string HtmlMessage);

    private sealed class IsolatedWebHostEnvironment : IWebHostEnvironment
    {
        public IsolatedWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");
        }

        public string ApplicationName { get; set; } = "MemoLens.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; }

        public string EnvironmentName { get; set; } = "Testing";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
