using System.Text;
using Newtonsoft.Json;

namespace HVZ.Web.Services;

public class BotApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public BotApiService(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    private async Task<string> Get(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/resources/{endpoint}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e);
            return e.Message;
        }
    }

    private async Task<string> Post(string endpoint, string data)
    {
        try
        {
            var content = new StringContent(data, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e);
            return e.Message;
        }
    }

    /// <summary>
    /// Sets user's roles in discord for a given server. Does not throw an exception if
    /// </summary>
    /// <returns></returns>
    public async Task<string> SetRoles(SetRolesArgs args) =>
        await Post("updateRoles", JsonConvert.SerializeObject(args));
}

public struct SetRolesArgs
{
    /// <value>the server ID</value>;
    public string discordServer;

    /// <value>a list of arrays which are userId/roleName pairs. { [userid1, role1], [userid2, role2] }</value>
    public List<string[]> users;
}