using NewSchoolLive.IOC;
using NewSchoolLive.IRepository;
using NewSchoolLive.IService;
using NewSchoolLive.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewSchoolLive.Service
{
    
    public class OSUserService : DbFirstBaseService<OS_User>, IOSUserService, IDbScoped
    {
        public IOSUserRepository baseService;
        public OSUserService(IOSUserRepository _baseService) : base(_baseService)
        {
            baseService = _baseService;
        }

        public async Task<object> GetAll()
        {
            return await baseService.DbBase().Queryable<OS_User>().Where(x => x.Id < 100).ToListAsync();
        }
    }
}
