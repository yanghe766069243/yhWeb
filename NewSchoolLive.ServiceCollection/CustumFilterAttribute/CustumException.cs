using System;
using System.Collections.Generic;
using System.Text;

namespace NewSchoolLive.ServiceCollection.CustumFilterAttribute
{
    public class CustumException : ApplicationException
    {
        public int errCode;
        private Exception innerException;
        public CustumException(string msg, int code = -1) : base(msg)
        {
            this.errCode = code;
        }

        //带有一个字符串参数和一个内部异常信息参数的构造函数
        public CustumException(string msg, Exception innerException, int code = -1) : base(msg)
        {
            this.innerException = innerException;
            this.errCode = code;
        }
        public int GetErrorCode()
        {
            return errCode;
        }
    }
}
