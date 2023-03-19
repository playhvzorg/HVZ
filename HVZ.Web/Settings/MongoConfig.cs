using System;

namespace HVZ.Web.Settings
{
    public class MongoConfig
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? DatabaseName { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string ConnectionString => UserName is not null ?
            $"mongodb://{UserName}:{Password}@{Host}:{Port}" :
            $"mongodb://{Host}:{Port}";
    }
}