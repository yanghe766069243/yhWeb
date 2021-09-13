using NewSchoolLive.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewSchoolLive.IService
{
    public interface IOSUserService : IDbFirstBaseService<OS_User>
    {
        Task<object> GetAll();
    }
}
