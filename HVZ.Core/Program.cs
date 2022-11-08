using HVZ.Web;
namespace HVZ.Core;

internal static class Program
{
    static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    private static async Task MainAsync(string[] args)
    {
        Webapp web = new Webapp(args);
        await web.RunAsnyc();
    }
}