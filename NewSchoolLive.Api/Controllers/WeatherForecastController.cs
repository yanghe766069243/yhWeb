using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewSchoolLive.Common;
using NewSchoolLive.IService;
using NewSchoolLive.Model;
using NewSchoolLive.Service;
using NewSchoolLive.ServiceCollection.CustumFilterAttribute;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewSchoolLive.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        private IOSUserService baseService;
        private IRedisHelper redisHelper;
        private IRedisHelper redisHelper2;


        private RedisHelper redis;
        public WeatherForecastController(ILogger<WeatherForecastController> logger, IOSUserService oSUserService, IRedisHelper _redisHelper, IRedisHelper _redisHelper2)
        {
            _logger = logger;
            baseService = oSUserService;
            redisHelper = _redisHelper;
            redisHelper2 = _redisHelper2;

            redis = new RedisHelper("127.0.0.1:6379,PassWord=redis", 0);
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpGet, Route("test1"), AllowAnonymous]
        public async Task<dynamic> a()
        {
            var d = await baseService.DbBase().Queryable<OS_User>().Where(x => x.Id < 100).ToListAsync();

            //return d;
            //throw new CustumException("这是一个自定义错误",300);

            //await redisHelper.GetDatabase(0).StringSetAsync("test", "12345667777",TimeSpan.FromSeconds(20));

            //var d = await redisHelper.GetDatabase(0).StringGetAsync("test");

            return d;
        }

        [HttpGet, Route("test2"), AllowAnonymous]
        public async Task<object> a1()
        {
            //var d = baseService1.Equals(baseService);
            var d = await baseService.DbBase().Queryable<OS_User>().Where(x => x.Id < 10).ToListAsync();

            var data = d.GetType().GetProperties().Where(x => x.Name == "Id").Select(x=>x.GetValue(x));



            var entityInfo = baseService.DbBase().EntityMaintenance.GetEntityInfo<OS_User>();
            var column = entityInfo.Columns.FirstOrDefault(x => x.DbColumnName == "Id");
            //foreach (var column in entityInfo.Columns)
            //{
            //    Console.WriteLine(column.DbColumnName);
            //}
            //throw new Exception("这是一个系统错误");

            return entityInfo;
        }

        [HttpGet, Route("test3")]
        [CustumExceptionFilter]
        public async Task<object> a2()
        {
            //var d = baseService1.Equals(baseService);
            throw new DivideByZeroException("值除以零时引发的异常");
        }
    }
}
