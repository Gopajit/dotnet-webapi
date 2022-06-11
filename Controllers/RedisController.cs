using Microsoft.AspNetCore.Mvc;
using Models;
using Services;
using StackExchange.Redis;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class RedisController : ControllerBase
{



    private readonly ILogger<WeatherForecastController> _logger;
    private readonly RedisCacheService _redisService;
    public RedisController(ILogger<WeatherForecastController> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redisService = new RedisCacheService(redis);
    }

    [HttpGet()]
    [Route("/redis/ping")]
    public ActionResult Get()
    {

        bool isRedisOk = _redisService.IsRedisOk();
        if (!isRedisOk)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new { Message = "Redis is responding slowly (timeout) or it is offline" });
        }

        var redisResponseTime = (int)_redisService.ReturnRedisResponseTime();
        return Ok(new { Message = $"Redis responding perfectly. Response time {redisResponseTime} ms." });

    }



    [HttpGet()]
    [Route("/redis/insert_test/{qty_keys_to_insert}")]
    public ActionResult GetStressTest(int qty_keys_to_insert)
    {
        if (qty_keys_to_insert > 120_000)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"The value {qty_keys_to_insert} is too big. Try a value less than 120000." });
        }

        bool isRedisOk = _redisService.IsRedisOk();
        if (!isRedisOk)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new { Message = "Operation canceled. Redis is responding slowly (timeout) or it is offline." });
        }

        
        DateTime startTime = DateTime.Now;
        string returnText = $"Inserting {qty_keys_to_insert} key-pairs in Redis. ";
        
        string[] entryKey = new string[qty_keys_to_insert];
        string[] entryValue = new string[qty_keys_to_insert];

        for (int i = 0; i < qty_keys_to_insert; i++)
        {
            entryKey[i] = "key-test-" + i.ToString();
            entryValue[i] = "sample-test-value-" + Guid.NewGuid().ToString();
            _redisService.SetCache(entryKey[i], entryValue[i], 60);
        }
        TimeSpan totalArrayCreationTime = DateTime.Now - startTime;
        returnText += $"Sample Array created in {totalArrayCreationTime.TotalMilliseconds} ms. ";



        startTime = DateTime.Now;
        for (int i = 0; i < qty_keys_to_insert; i++)
        {
            _redisService.SetCache(entryKey[i], entryValue[i], 60);
        }
        TimeSpan totalRedisInsertTime = DateTime.Now - startTime;
        returnText += $"Total Redis Insertions {totalRedisInsertTime.TotalMilliseconds} ms.";

        return Ok(new { Message = returnText});

    }

}
