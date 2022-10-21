namespace HVZ.Web;
public class Webapp
{
    public Webapp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Run();
    }
}