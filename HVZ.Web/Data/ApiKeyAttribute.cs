using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HVZ.Web.Data;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ApiKeyAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER_NAME = "X-API-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        string? submittedApiKey = GetSubmittedApiKey(context.HttpContext);

        string apiKey = GetApiKey(context.HttpContext);

        if (!IsApiKeyValid(apiKey, submittedApiKey))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    private static string? GetSubmittedApiKey(HttpContext context)
    {
        string? key = context.Request.Headers[API_KEY_HEADER_NAME];
        return key;
    }

    private static string GetApiKey(HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        string? key = configuration.GetValue<string>($"ApiKey");

        if (key is null)
            throw new InvalidOperationException("API key not configured");

        return key;
    }

    private static bool IsApiKeyValid(string apiKey, string? submittedApiKey)
    {
        if (string.IsNullOrEmpty(submittedApiKey)) return false;

        var apiKeySpan = MemoryMarshal.Cast<char, byte>(apiKey.AsSpan());

        var submittedApiKeySpan = MemoryMarshal.Cast<char, byte>(submittedApiKey.AsSpan());

        return CryptographicOperations.FixedTimeEquals(apiKeySpan, submittedApiKeySpan);
    }
}