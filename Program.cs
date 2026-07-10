using System.Text;
using System.Text.Json;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Api;
using MemoLens.Models.Auth;
using MemoLens.Models.Email;
using MemoLens.Services;
using MemoLens.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
    .Validate(
        options => Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
        "Jwt:SecretKey must contain at least 32 bytes.")
    .Validate(options => options.AccessTokenMinutes > 0, "Jwt:AccessTokenMinutes must be positive.")
    .Validate(options => options.RefreshTokenDays > 0, "Jwt:RefreshTokenDays must be positive.")
    .ValidateOnStart();

builder.Services
    .AddOptions<RefreshTokenCleanupOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenCleanupOptions.SectionName))
    .Validate(options => options.CleanupIntervalHours > 0, "RefreshTokenCleanup:CleanupIntervalHours must be positive.")
    .Validate(options => options.RevokedTokenRetentionDays >= 0, "RefreshTokenCleanup:RevokedTokenRetentionDays must be zero or greater.")
    .Validate(options => options.ExpiredTokenRetentionDays >= 0, "RefreshTokenCleanup:ExpiredTokenRetentionDays must be zero or greater.")
    .Validate(options => options.BatchSize is > 0 and <= 5000, "RefreshTokenCleanup:BatchSize must be between 1 and 5000.")
    .ValidateOnStart();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

// Identity cookies remain the default for MVC. Future API controllers must
// explicitly use JwtBearerDefaults.AuthenticationScheme for bearer tokens.
builder.Services
    .AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwtOptions.SecretKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new ApiResponse
                {
                    Success = false,
                    Message = "Bạn cần đăng nhập bằng Bearer token để truy cập tài nguyên này."
                });
            }
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));

var emailOptions = builder.Configuration
    .GetSection(EmailOptions.SectionName)
    .Get<EmailOptions>() ?? new EmailOptions();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender, DevelopmentEmailSender>();
}
else if (string.Equals(emailOptions.Mode, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddTransient<IEmailSender, UnconfiguredEmailSender>();
}

builder.Services.AddScoped<IImageStorageService, LocalImageStorageService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokenCleanupService, RefreshTokenCleanupService>();
builder.Services.AddHostedService<RefreshTokenCleanupHostedService>();
builder.Services
    .AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => string.IsNullOrWhiteSpace(entry.Key)
                        ? "request"
                        : JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Dữ liệu gửi lên không hợp lệ."
                            : error.ErrorMessage)
                        .ToArray());

            return new BadRequestObjectResult(new ApiValidationErrorResponse
            {
                Success = false,
                Message = "Dữ liệu gửi lên chưa hợp lệ.",
                Errors = errors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MemoLens API",
        Version = "v1",
        Description = "API nền tảng cho ứng dụng mobile MemoLens."
    });
});

var app = builder.Build();
var seedLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeedData");

try
{
    await IdentitySeedData.SeedAsync(app.Services, app.Configuration);
}
catch (Exception ex)
{
    seedLogger.LogWarning(ex, "Identity roles could not be seeded. Run database migrations, then restart the app.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MemoLens API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
