using Fortnox.SDK;
using Fortnox.SDK.Auth;
using Fortnox.SDK.Authorization;
using Fortnox.SDK.Exceptions;
using Ivy.Hooks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Ivy.Auth.Fortnox;

public class FortnoxOAuthException(string? error, string? errorCode, string? errorDescription)
    : Exception($"Fortnox error: '{error}', code '{errorCode}' - {errorDescription}")
{
    public string? Error { get; } = error;
    public string? ErrorCode { get; } = errorCode;
    public string? ErrorDescription { get; } = errorDescription;
}

public class FortnoxAuthProvider : IAuthProvider
{
    private readonly FortnoxAuthClient _authClient;

    private readonly Scope[] _scopes;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private readonly List<AuthOption> _authOptions = [];


    public FortnoxAuthProvider() : this([Scope.Profile]) { }

    public FortnoxAuthProvider(Scope[] scopes)
    {
        _scopes = scopes;
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetEntryAssembly()!)
            .Build();

        _clientId = configuration.GetValue<string>("FORTNOX_CLIENT_ID") ?? throw new Exception("FORTNOX_CLIENT_ID is required");
        _clientSecret = configuration.GetValue<string>("FORTNOX_CLIENT_SECRET") ?? throw new Exception("FORTNOX_CLIENT_SECRET is required");

        _authClient = new FortnoxAuthClient();
    }

    public Task<AuthToken?> LoginAsync(string email, string password) => throw new InvalidOperationException("Fortnox login with email/password is not supported");

    public async Task<Uri> GetOAuthUriAsync(AuthOption option, WebhookEndpoint callback)
    {
        var uri = _authClient.StandardAuthWorkflow.BuildAuthUri(_clientId, _scopes, callback.Id, callback.GetUri(false).ToString());
        return await Task.FromResult(uri);
    }

    public async Task<AuthToken?> HandleOAuthCallbackAsync(HttpRequest request)
    {
        var code = request.Query["code"].ToString();
        var error = request.Query["error"].ToString();
        var errorDescription = request.Query["error_description"].ToString();

        if (error.Length > 0 || errorDescription.Length > 0)
        {
            throw new FortnoxOAuthException(error, null, errorDescription);
        }
        else if (code.Length == 0)
        {
            throw new Exception("Received no authorization code from Fortnox.");
        }

        var redirectUri = $"{request.Scheme}://{request.Host}{request.Path}";

        var tokenInfo = await _authClient.StandardAuthWorkflow.GetTokenAsync(code, _clientId, _clientSecret, redirectUri);
        var accessToken = tokenInfo.AccessToken;
        var refreshToken = tokenInfo.RefreshToken;

        return new AuthToken(
            accessToken,
            refreshToken,
            DateTime.Now + TimeSpan.FromSeconds(tokenInfo.ExpiresIn)
        );
    }

    public Task LogoutAsync(string _) => Task.CompletedTask;

    public async Task<AuthToken?> RefreshJwtAsync(AuthToken jwt)
    {
        if (jwt.ExpiresAt is null || jwt.RefreshToken is null || DateTimeOffset.UtcNow < jwt.ExpiresAt)
        {
            // Refresh not needed (or not possible).
            return jwt;
        }

        try
        {
            var info = await _authClient.StandardAuthWorkflow.RefreshTokenAsync(jwt.Jwt, _clientId, _clientSecret);
            return info.AccessToken is null
                ? null
                : new AuthToken(info.AccessToken, info.RefreshToken,
                    DateTime.Now + TimeSpan.FromSeconds(info.ExpiresIn));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> ValidateJwtAsync(string jwt)
    {
        try
        {
            // Try to access a protected resource to validate the JWT
            var auth = new StandardAuth(jwt);
            var client = new FortnoxClient(auth);
            var profile = await client.ProfileConnector.GetAsync();
            return profile is not null;
        }
        catch (FortnoxApiException)
        {
            // If any exception occurs during validation, consider the token invalid
            return false;
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string jwt)
    {
        var auth = new StandardAuth(jwt);
        var c = new FortnoxClient(auth);
        var profile = await c.ProfileConnector.GetAsync();

        if (profile?.Id is null || profile.Email is null)
        {
            return null;
        }

        return new UserInfo(
            profile.Id,
            profile.Email,
            profile.Name,
            null
        );
    }

    public FortnoxAuthProvider UseFortnox()
    {
        _authOptions.Add(new AuthOption(AuthFlow.OAuth, "Fortnox", "fortnox"));
        return this;
    }

    public AuthOption[] GetAuthOptions() => [.. _authOptions];
}
