using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewSchoolLive.Common
{
    public interface IRedisHelper : IDisposable
    {
        IDatabase GetDatabase(int db = -1);

        //void Dispose();
    }
}
