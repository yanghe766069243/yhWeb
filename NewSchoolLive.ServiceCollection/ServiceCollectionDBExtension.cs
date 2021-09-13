using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using NewSchoolLive.IOC;

namespace NewSchoolLive.Common
{
    public static class ServiceCollectionDBExtension
    {

        /// <summary>
        /// 注入接口到容器 业务 和 仓储  
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddAutoCollection(this IServiceCollection services)
        {
            //var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            string path = AppContext.BaseDirectory;
            var referencedAssemblies = Directory.GetFiles(path, "*.dll").Select(Assembly.LoadFrom).Cast<Assembly>().Where(T => T.FullName.Contains("NewSchoolLive")).ToArray();
            //Assembly[] referencedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Cast<Assembly>().Where(T => T.FullName.Contains("NewSchoolLive")).ToArray();
            AddIOCCollection(services, typeof(IDbScoped), referencedAssemblies);
            AddIOCCollection(services, typeof(IDbTransient), referencedAssemblies);
            AddIOCCollection(services, typeof(IDbSingleton), referencedAssemblies);
            return services;
        }

        
        private static void AddIOCCollection(this IServiceCollection services, Type iType, Assembly[] AssemblyInfo)
        {
            //注入业务 和 仓储 业务以Service结尾 仓储以 Repository 结尾
            var ServiceAssembly = AssemblyInfo.Where(x => x.FullName.Contains("Service")); //注入业务接口和实现类 名称以Service结尾
            Type[] DependencyTypes = ServiceAssembly.SelectMany(s => s.GetTypes()).Where(p => iType.IsAssignableFrom(p) && p != iType).ToArray();
            foreach (var item in DependencyTypes)
            {
                var d = ServiceAssembly.SelectMany(s => s.GetTypes()).FirstOrDefault(x => x.FullName.Contains(item.Name));
                if (d != null)
                {
                    //services.AddSingleton(d, item);

                    if (iType.Name == nameof(IDbScoped))
                    {
                        services.AddScoped(d, item);
                    }
                    else if (iType.Name == nameof(IDbTransient))
                    {
                        services.AddTransient(d, item);
                    }
                    else if (iType.Name == nameof(IDbSingleton))
                    {
                        services.AddSingleton(d, item);
                    }
                }
            }
            var RepositoryAssembly = AssemblyInfo.Where(x => x.FullName.Contains("Repository")); //注入仓储接口和实现类 名称以Repository结尾
            DependencyTypes = RepositoryAssembly.SelectMany(s => s.GetTypes()).Where(p => iType.IsAssignableFrom(p) && p != iType).ToArray();
            foreach (var item in DependencyTypes)
            {
                var d = RepositoryAssembly.SelectMany(s => s.GetTypes()).FirstOrDefault(x => x.FullName.Contains(item.Name));
                //services.AddSingleton(d, item);
                if (iType.Name == nameof(IDbScoped))
                {
                    services.AddScoped(d, item);
                }
                else if (iType.Name == nameof(IDbTransient))
                {
                    services.AddTransient(d, item);
                }
                else if (iType.Name == nameof(IDbSingleton))
                {
                    services.AddSingleton(d, item);
                }
            }


        }

    }
}
