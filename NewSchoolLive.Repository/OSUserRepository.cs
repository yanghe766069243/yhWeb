using NewSchoolLive.Common;
using NewSchoolLive.IOC;
using NewSchoolLive.IRepository;
using NewSchoolLive.Model;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewSchoolLive.Repository
{
    public class OSUserRepository : BaseDbFirstRepository<OS_User>, IOSUserRepository, IDbScoped
    {
        public OSUserRepository(IRedisHelper redisCache) : base(redisCache)
        {

        }
    }
}
