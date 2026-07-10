using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoLens.Tests;

public class AuthApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "MemoLens1";
    private const string NewPassword = "MemoLens2";

    private readonly CustomWebApplicationFactory _factory;

    public AuthApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsSuccessWithoutTokens_AndUnconfirmedLoginFails()
    {
        using var client = CreateClient();
        var email = NewEmail();

        using var registerResponse = await RegisterAsync(client, email);
        var registerJson = await ReadJsonAsync(registerResponse);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.True(registerJson.RootElement.GetProperty("success").GetBoolean());
        Assert.False(registerJson.RootElement.TryGetProperty("data", out _));
        Assert.DoesNotContain("accessToken", await registerResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", await registerResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        using var loginResponse = await PostJsonAsync(client, "/api/v1/auth/login", new
        {
            email,
            password = Password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidRequestAndWrongPassword_ReturnExpectedFailures()
    {
        using var client = CreateClient();

        using var invalidResponse = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        using var invalidJson = await ReadJsonAsync(invalidResponse);

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.False(invalidJson.RootElement.GetProperty("success").GetBoolean());
        Assert.True(invalidJson.RootElement.TryGetProperty("errors", out _));

        var email = NewEmail();
        await RegisterAndConfirmAsync(client, email);

        using var wrongPasswordResponse = await PostJsonAsync(client, "/api/v1/auth/login", new
        {
            email,
            password = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmedLogin_AndAccountMe_ReturnOnlyCurrentUserSummary()
    {
        using var client = CreateClient();
        var email = NewEmail();

        await RegisterAndConfirmAsync(client, email);
        var tokens = await LoginAsync(client, email, Password);

        Assert.False(tokens.AccessToken.Length == 0);
        Assert.False(tokens.RefreshToken.Length == 0);
        Assert.False(tokens.ResponseHeaders.Contains("Set-Cookie"));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var accountResponse = await client.GetAsync("/api/v1/account/me");
        var accountBody = await accountResponse.Content.ReadAsStringAsync();
        using var accountJson = JsonDocument.Parse(accountBody);

        Assert.Equal(HttpStatusCode.OK, accountResponse.StatusCode);
        Assert.True(accountJson.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(email, accountJson.RootElement.GetProperty("data").GetProperty("email").GetString());
        Assert.DoesNotContain("passwordHash", accountBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", accountBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", accountBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", accountBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_RotatesToken_PreventsReuse_AndStoresOnlyHashes()
    {
        using var client = CreateClient();
        var email = NewEmail();

        await RegisterAndConfirmAsync(client, email);
        var loginTokens = await LoginAsync(client, email, Password);

        using var refreshResponse = await PostJsonAsync(client, "/api/v1/auth/refresh", new
        {
            refreshToken = loginTokens.RefreshToken
        });
        var refreshedTokens = await ExtractTokensAsync(refreshResponse);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotEqual(loginTokens.RefreshToken, refreshedTokens.RefreshToken);

        using var reusedTokenResponse = await PostJsonAsync(client, "/api/v1/auth/refresh", new
        {
            refreshToken = loginTokens.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, reusedTokenResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var storedTokens = await dbContext.UserRefreshTokens
            .Where(token => token.User.Email == email)
            .OrderBy(token => token.Id)
            .ToListAsync();

        Assert.Equal(2, storedTokens.Count);
        Assert.All(storedTokens, token =>
        {
            Assert.NotEqual(loginTokens.RefreshToken, token.TokenHash);
            Assert.NotEqual(refreshedTokens.RefreshToken, token.TokenHash);
        });
        Assert.Contains(storedTokens, token =>
            token.TokenHash == tokenService.HashRefreshToken(loginTokens.RefreshToken) &&
            token.RevokedAt.HasValue);
        Assert.Contains(storedTokens, token =>
            token.TokenHash == tokenService.HashRefreshToken(refreshedTokens.RefreshToken) &&
            !token.RevokedAt.HasValue);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        using var client = CreateClient();
        var email = NewEmail();

        await RegisterAndConfirmAsync(client, email);
        var tokens = await LoginAsync(client, email, Password);

        using var logoutResponse = await PostJsonAsync(client, "/api/v1/auth/logout", new
        {
            refreshToken = tokens.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        using var refreshResponse = await PostJsonAsync(client, "/api/v1/auth/refresh", new
        {
            refreshToken = tokens.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotAndResetPassword_RevokesExistingRefreshTokens()
    {
        using var client = CreateClient();
        var unknownEmail = NewEmail();

        _factory.EmailSender.Clear();
        using var unknownResponse = await PostJsonAsync(client, "/api/v1/auth/forgot-password", new
        {
            email = unknownEmail
        });

        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
        Assert.Empty(_factory.EmailSender.Messages);

        var email = NewEmail();
        await RegisterAndConfirmAsync(client, email);
        var tokens = await LoginAsync(client, email, Password);

        _factory.EmailSender.Clear();
        using var forgotResponse = await PostJsonAsync(client, "/api/v1/auth/forgot-password", new
        {
            email
        });

        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        var resetEmail = Assert.Single(_factory.EmailSender.Messages);
        Assert.Equal(email, resetEmail.Email);

        using var resetResponse = await PostJsonAsync(client, "/api/v1/auth/reset-password", new
        {
            email,
            token = ExtractQueryValue(resetEmail.HtmlMessage, "token"),
            password = NewPassword,
            confirmPassword = NewPassword
        });

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        using var oldPasswordResponse = await PostJsonAsync(client, "/api/v1/auth/login", new
        {
            email,
            password = Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);

        using var revokedRefreshResponse = await PostJsonAsync(client, "/api/v1/auth/refresh", new
        {
            refreshToken = tokens.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, revokedRefreshResponse.StatusCode);

        var newTokens = await LoginAsync(client, email, NewPassword);
        Assert.False(string.IsNullOrWhiteSpace(newTokens.AccessToken));
    }

    private async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
    {
        return await PostJsonAsync(client, "/api/v1/auth/register", new
        {
            email,
            password = Password,
            confirmPassword = Password,
            displayName = "API Test"
        });
    }

    private async Task RegisterAndConfirmAsync(HttpClient client, string email)
    {
        using var registerResponse = await RegisterAsync(client, email);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        string userId;
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);

            Assert.NotNull(user);
            userId = user.Id;
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        using var confirmResponse = await PostJsonAsync(client, "/api/v1/auth/confirm-email", new
        {
            userId,
            token = encodedToken
        });

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
    }

    private async Task<AuthTokens> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await PostJsonAsync(client, "/api/v1/auth/login", new
        {
            email,
            password,
            deviceName = "Integration Test"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ExtractTokensAsync(response);
    }

    private static async Task<AuthTokens> ExtractTokensAsync(HttpResponseMessage response)
    {
        using var document = await ReadJsonAsync(response);
        var data = document.RootElement.GetProperty("data");

        return new AuthTokens(
            data.GetProperty("accessToken").GetString() ?? string.Empty,
            data.GetProperty("refreshToken").GetString() ?? string.Empty,
            response.Headers);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object body)
    {
        return client.PostAsJsonAsync(path, body);
    }

    private static string ExtractQueryValue(string htmlMessage, string parameterName)
    {
        var linkMatch = Regex.Match(htmlMessage, "href=\"(?<url>[^\"]+)\"");
        Assert.True(linkMatch.Success, "The fake email did not contain a reset link.");

        var uri = new Uri(System.Net.WebUtility.HtmlDecode(linkMatch.Groups["url"].Value));
        var values = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var pair = values
            .Select(value => value.Split('=', 2))
            .SingleOrDefault(value => string.Equals(value[0], parameterName, StringComparison.Ordinal));

        Assert.NotNull(pair);
        Assert.Equal(2, pair.Length);
        return Uri.UnescapeDataString(pair[1]);
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static string NewEmail()
    {
        return $"api-auth-{Guid.NewGuid():N}@example.test";
    }

    private sealed record AuthTokens(string AccessToken, string RefreshToken, HttpResponseHeaders ResponseHeaders);
}
