using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NewSchoolLive.Common;
using NewSchoolLive.IOC;
using NewSchoolLive.RabbitMQ;
using NewSchoolLive.ServiceCollection.CustumFilterAttribute;
using SqlSugar;
using SqlSugar.IOC;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NewSchoolLive.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NewSchoolLive.Api", Version = "v1" });

                DirectoryInfo dirs = new DirectoryInfo(AppContext.BaseDirectory);
                FileInfo[] files = dirs.GetFiles("*.xml");
                foreach (var path in files)
                {
                    c.IncludeXmlComments(path.FullName);
                }
            });

            #region SqlServer注入
            var ConnectionStringsWrite = Configuration.GetSection("ConnectionStringsWrite:ConnectionDb1").Value;
            var ConnectionStringsReads = Configuration.GetSection("ConnectionStringsRead").GetChildren().ToList().Select(w => w.Value).ToList();

            List<SlaveConnectionConfig> redConnections = new List<SlaveConnectionConfig>();
            foreach (var item in ConnectionStringsReads)
            {
                redConnections.Add(new SlaveConnectionConfig() { HitRate = 10, ConnectionString = item });
            }
            //注入SqlSugar 主库
            services.AddSqlSugar(new IocConfig()
            {
                ConnectionString = ConnectionStringsWrite,
                DbType = IocDbType.SqlServer,
                IsAutoCloseConnection = true//自动释放
            });
            //注入读库
            services.ConfigurationSugar(db =>
            {
                //db.CurrentConnectionConfig.SlaveConnectionConfigs = new List<SlaveConnectionConfig>() {
                //    new SlaveConnectionConfig{ HitRate = 10, ConnectionString = "Server=.;Database=test;UID=sa;Password=Woshiren.123;MultipleActiveResultSets=true" }, //HitRate 越大走这个从库的概率越大
                //    new SlaveConnectionConfig{ HitRate = 20, ConnectionString = "Server=.;Database=test;UID=sa;Password=Woshiren.123;MultipleActiveResultSets=true" } //HitRate 越大走这个从库的概率越大
                //};
                db.CurrentConnectionConfig.SlaveConnectionConfigs = redConnections;
                db.Aop.OnLogExecuting = (sql, p) =>
                {
                    Console.WriteLine(sql);
                };
                //设置更多连接参数
                //db.CurrentConnectionConfig
            });
            #endregion

            services.AddAutoCollection(); //注入接口到容器 业务 和 仓储  
            #region 注入Redis缓存服务
            //注入Redis
            var section = Configuration.GetSection("Redis1:Default");
            //连接字符串
            var redisConnectionString = section.GetSection("Connection").Value;
            //默认数据库 
            int _defaultDB = int.Parse(section.GetSection("DefaultDB").Value ?? "0");
            services.AddSingleton<IRedisHelper, RedisHelper>(serviceProvider => new RedisHelper(redisConnectionString, _defaultDB));
            var redisConfiguration = Configuration.GetSection("Redis").Get<RedisConfiguration>();
            services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration);
            #endregion

            //services.AddSingleton<RabbitMQInvoker>();
            //services.Configure<RabbitMQOptions>(Configuration.GetSection("RabbitMQOptions"));


            #region AutoMapper
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.ToLower().Contains("Common"));
            services.AddAutoMapper(assemblies);
            #endregion


            #region 原始注入 接口 仓储
            ////业务层注入  
            //Assembly assemblys = Assembly.Load("NewSchoolLive.IService");
            ////接口
            //var typesInterface = assemblys.GetTypes().Where(w => w.Namespace == "NewSchoolLive.IService");

            //Assembly assemblys1 = Assembly.Load("NewSchoolLive.Service");

            ////实现类
            //var typesImpl = assemblys1.GetTypes().Where(w => w.Namespace == "NewSchoolLive.Service");
            //foreach (var item in typesInterface)
            //{
            //    var name = item.Name.Substring(1);
            //    var impl = typesImpl.FirstOrDefault(w => w.Name.Contains(name));
            //    if (impl != null)
            //    {
            //        services.AddTransient(item, impl);
            //    }
            //}

            ////业务层注入  
            //assemblys = Assembly.Load("NewSchoolLive.IRepository");
            ////接口
            //typesInterface = assemblys.GetTypes().Where(w => w.Namespace == "NewSchoolLive.IRepository");

            //assemblys1 = Assembly.Load("NewSchoolLive.Repository");

            ////实现类
            //typesImpl = assemblys1.GetTypes().Where(w => w.Namespace == "NewSchoolLive.Repository");
            //foreach (var item in typesInterface)
            //{
            //    var name = item.Name.Substring(1);
            //    var impl = typesImpl.FirstOrDefault(w => w.Name.Contains(name));
            //    if (impl != null)
            //    {
            //        services.AddTransient(item, impl);
            //    }
            //}
            #endregion

            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = "http://localhost:6001";
                options.RequireHttpsMetadata = false;
                options.Audience = "Api12";
            });
            #region 全局注册自定义Filter
            services.AddMvc(options =>
            {
                options.Filters.Add<CustumExceptionFilter>();//全局注册自定义异常提示
                options.Filters.Add<ApiResultActionFilter>();
            });
            #endregion



        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseRedisInformation();
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NewSchoolLive.Api v1"));
            }
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();



            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

