namespace HVZ.Web.Settings; 

public class MongoConfig {
    public string? Name { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; }
    public string ConnectionString => $"mongodb://{Host}:{Port}";
}