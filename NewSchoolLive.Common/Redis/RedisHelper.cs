using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NewSchoolLive.Common
{
    public class RedisHelper : IRedisHelper
    {
        ////连接字符串
        private static string _connectionString;
        ////实例名称
        //public static string _instanceName;
        //默认数据库
        private int _defaultDB;
        private ConcurrentDictionary<string, ConnectionMultiplexer> _connections;
        private ConnectionMultiplexer _connectionMultiplexer { get; set; }
        public RedisHelper(string connectionString, int defaultDB = 0)
        {
            this._connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        }
        /// <summary>
        /// 获取数据库
        /// </summary>
        /// <param name="db">数据库序号</param>
        /// <returns></returns>
        public IDatabase GetDatabase(int db = -1)
        {
            return _connectionMultiplexer.GetDatabase(db >= 0 ? db : _defaultDB);
        }

        public void Dispose()
        {
            if (_connections != null && _connections.Count > 0)
            {
                foreach (var item in _connections.Values)
                {
                    item.Close();
                }
            }
        }
    }
}
