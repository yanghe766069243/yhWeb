using System;
using System.Collections.Generic;
using System.Text;

namespace NewSchoolLive.DtoModel.Respone
{
    public class BaseReturn<T>
    {
        public BaseReturn()
        {
            this.Code = 200;
            this.Message = "";
        }
        /// <summary>
        /// 返回结果
        /// </summary>
        public T Data { get; set; }
        /// <summary>
        /// 消息码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }
    }
}
