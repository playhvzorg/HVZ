using Amazon.Runtime.Internal;
using HVZ.Persistence;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HVZ.Web.Data;

[Route("api/v1/[action]")]
public class RestApiController : ControllerBase
{
    private IMongoDatabase _mongoDatabase;

    public RestApiController(IMongoDatabase mongoDatabase)
    {
        _mongoDatabase = mongoDatabase;
    }

    [HttpGet]
    public IActionResult health()
    {
        return _mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000)
            ? new JsonResult("Healthy")
            : new JsonResult("Unable to reach database") { StatusCode = 500 };
    }
}