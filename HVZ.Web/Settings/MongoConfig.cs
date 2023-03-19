using System;

namespace HVZ.Web.Settings
{
    public class MongoConfig
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? DatabaseName { get; set; }
        public string? Host1 { get; set; }
        public int Port1 { get; set; }
        public string? Host2 { get; set; }
        public int Port2 { get; set; }
        public string? Host3 { get; set; }
        public int Port3 { get; set; }

        public string ConnectionString =>
            "mongodb://" +
            (UserName is not null ? $"{UserName}:{Password}@" : "") +
            $"{Host1}:{Port1}" +
            (Host2 is not null ? $"{Host2}:{Port2}" : "") + 
            (Host3 is not null ? $"{Host3}:{Port3}" : "");
    }
}