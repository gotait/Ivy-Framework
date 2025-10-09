using Ivy.Hooks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ivy.Auth.Facebook;

public enum Scope
{
    AdsManagement,
    AdsRead,
    AttributionRead,
    BusinessManagement,
    CatalogManagement,
    CommerceAccountManageOrders,
    CommerceAccountReadOrders,
    CommerceAccountReadReports,
    CommerceAccountReadSettings,
    CommerceManageAccounts,
    Email,
    FacebookCreatorMarketplaceDiscovery,
    GamingUserLocale,
    InstagramBasic,
    InstagramBrandedContentAdsBrand,
    InstagramBrandedContentBrand,
    InstagramBrandedContentCreator,
    InstagramBusinessBasic,
    InstagramBusinessContentPublish,
    InstagramBusinessManageComments,
    InstagramBusinessManageMessages,
    InstagramCreatorMarketplaceDiscovery,
    InstagramCreatorMarketplaceMessaging,
    InstagramContentPublish,
    InstagramManageComments,
    InstagramManageEvents,
    InstagramManageInsights,
    InstagramManageMessages,
    InstagramShoppingTagProducts,
    InstagramManageUpcomingEvents,
    LeadsRetrieval,
    ManageAppSolutions,
    ManageFundraisers,
    PagesEvents,
    PagesManageAds,
    PagesManageCta,
    PagesManageInstantArticles,
    PagesManageEngagement,
    PagesManageMetadata,
    PagesManagePosts,
    PagesMessaging,
    PagesReadEngagement,
    PagesReadUserContent,
    PagesShowList,
    PagesUserGender,
    PagesUserLocale,
    PagesUserTimezone,
    PagesUtilityMessaging,
    PublicProfile,
    PublishVideo,
    ReadAudienceNetworkInsights,
    ReadInsights,
    ThreadsBasic,
    ThreadsBusinessBasic,
    ThreadsContentPublish,
    ThreadsDelete,
    ThreadsKeywordSearch,
    ThreadsLocationTagging,
    ThreadsManageInsights,
    ThreadsManageMentions,
    ThreadsManageReplies,
    ThreadsProfileDiscovery,
    ThreadsReadReplies,
    UserAgeRange,
    UserBirthday,
    UserFriends,
    UserGender,
    UserHometown,
    UserLikes,
    UserLink,
    UserLocation,
    UserMessengerContact,
    UserPhotos,
    UserPosts,
    UserVideos,
    WhatsappBusinessManageEvents,
    WhatsappBusinessManagement,
    WhatsappBusinessMessaging,
}

public static class FacebookScopeExtensions
{
    public static string ToSnakeCaseString(this Scope scope)
    {
        var name = scope.ToString();
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}

class TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

class TokenDetails
{
    [JsonPropertyName("expires_at")]
    public required int ExpiresAt { get; set; }
}

class DebugTokenResponse
{
    [JsonPropertyName("data")]
    public required TokenDetails Data { get; set; }
}

class FacebookUserResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("picture")]
    public required FacebookPicture Picture { get; set; }
}

class FacebookPicture
{
    [JsonPropertyName("data")]
    public required FacebookPictureData Data { get; set; }
}

class FacebookPictureData
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

public class FacebookOAuthException(string? error, string? errorCode, string? errorDescription)
    : Exception($"Facebook error: '{error}', code '{errorCode}' - {errorDescription}")
{
    public string? Error { get; } = error;
    public string? ErrorCode { get; } = errorCode;
    public string? ErrorDescription { get; } = errorDescription;
}

public class FacebookAuthProvider : IAuthProvider
{
    private HttpClient _client = new HttpClient();
    private Scope[] _scopes;
    private readonly string _appId;
    private readonly string _appSecret;

    private readonly List<AuthOption> _authOptions = [];

    public FacebookAuthProvider() : this([Scope.PublicProfile, Scope.Email]) { }

    public FacebookAuthProvider(Scope[] scopes)
    {
        _scopes = scopes;
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetEntryAssembly()!)
            .Build();

        _appId = configuration.GetValue<string>("Facebook:AppId") ?? throw new Exception("Facebook:AppId is required");
        _appSecret = configuration.GetValue<string>("Facebook:AppSecret") ?? throw new Exception("Facebook:AppSecret is required");
    }

    public Task<AuthToken?> LoginAsync(string email, string password) => throw new InvalidOperationException("Facebook login with email/password is not supported");

    public async Task<Uri> GetOAuthUriAsync(AuthOption option, WebhookEndpoint callback)
    {
        var appId = Uri.EscapeDataString(_appId);
        var redirectUri = Uri.EscapeDataString(callback.GetUri(false).ToString());
        var state = Uri.EscapeDataString(callback.Id);
        var scope = Uri.EscapeDataString(string.Join(",", _scopes.Select(s => s.ToSnakeCaseString())));

        var uriString = $"https://www.facebook.com/v24.0/dialog/oauth?client_id={appId}&redirect_uri={redirectUri}&state={state}&scope={scope}";
        return await Task.FromResult(new Uri(uriString));
    }

    public async Task<AuthToken?> HandleOAuthCallbackAsync(HttpRequest request)
    {
        var code = request.Query["code"].ToString();
        var error = request.Query["error"].ToString();
        var errorDescription = request.Query["error_description"].ToString();

        if (error.Length > 0 || errorDescription.Length > 0)
        {
            throw new FacebookOAuthException(error, null, errorDescription);
        }
        else if (code.Length == 0)
        {
            throw new Exception("Received no authorization code from Facebook.");
        }

        var appId = Uri.EscapeDataString(_appId);
        var redirectUri = Uri.EscapeDataString($"{request.Scheme}://{request.Host}{request.Path}");
        var appSecret = Uri.EscapeDataString(_appSecret);

        var uriString = $"https://graph.facebook.com/v24.0/oauth/access_token?client_id={appId}&redirect_uri={redirectUri}&client_secret={appSecret}&code={code}";
        try
        {
            HttpResponseMessage response = await _client.GetAsync(uriString);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<TokenResponse>(jsonString)
                ?? throw new Exception("Failed to deserialize token JSON response.");

            return new AuthToken(
                token.AccessToken,
                null,
                DateTime.Now + TimeSpan.FromSeconds(token.ExpiresIn)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public Task LogoutAsync(string _) => Task.CompletedTask;

    public Task<AuthToken?> RefreshJwtAsync(AuthToken jwt)
    {
        if (jwt.ExpiresAt is null || jwt.RefreshToken is null || DateTimeOffset.UtcNow < jwt.ExpiresAt)
        {
            // Refresh not needed (or not possible).
            return Task.FromResult<AuthToken?>(jwt);
        }

        return Task.FromResult<AuthToken?>(null);
    }

    private async Task<string?> GetAppAccessToken()
    {
        var appId = Uri.EscapeDataString(_appId);
        var appSecret = Uri.EscapeDataString(_appSecret);

        var uriString = $"https://graph.facebook.com/oauth/access_token?client_id={appId}&client_secret={appSecret}&grant_type=client_credentials";
        try
        {
            HttpResponseMessage response = await _client.GetAsync(uriString);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();

            var token = JsonSerializer.Deserialize<TokenResponse>(jsonString)
                ?? throw new Exception("Failed to deserialize token JSON response.");

            return token.AccessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ValidateJwtAsync(string jwt)
    {
        var appToken = await this.GetAppAccessToken();
        if (appToken is null)
        {
            throw new Exception("Failed to get Facebook app token");
        }

        var encodedJwt = Uri.EscapeDataString(jwt);
        var encodedAppToken = Uri.EscapeDataString(appToken);
        var uriString = $"https://graph.facebook.com/debug_token?input_token={encodedJwt}&access_token={encodedAppToken}";

        try
        {
            HttpResponseMessage response = await _client.GetAsync(uriString);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<DebugTokenResponse>(jsonString)
                ?? throw new Exception("Failed to deserialize debug token JSON response.");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string jwt)
    {
        var encodedJwt = Uri.EscapeDataString(jwt);
        var uriString = $"https://graph.facebook.com/v24.0/me?fields=id,name,email,picture&access_token={encodedJwt}";

        try
        {
            HttpResponseMessage response = await _client.GetAsync(uriString);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<FacebookUserResponse>(jsonString);

            if (user is null)
            {
                return null;
            }

            return new UserInfo(user.Id, user.Email ?? "", user.Name, user.Picture.Data.Url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetUserInfoAsync: {ex.Message}");
            return null;
        }
    }

    public FacebookAuthProvider UseFacebook()
    {
        _authOptions.Add(new AuthOption(AuthFlow.OAuth, "Facebook", "facebook"));
        return this;
    }

    public FacebookAuthProvider WithScopes(Scope[] scopes)
    {
        _scopes = scopes;
        return this;
    }

    public AuthOption[] GetAuthOptions() => [.. _authOptions];
}
