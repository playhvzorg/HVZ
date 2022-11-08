namespace HVZ.Web;
public class Webapp
{
    WebApplication app;
    public Webapp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        app = builder.Build();
        app.MapGet("/", () => "Hello World!");
    }

    public Task RunAsnyc()
    {
        return app.RunAsync();
    }
}