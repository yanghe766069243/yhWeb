using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewSchoolLive.DtoModel.Respone;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewSchoolLive.ServiceCollection.CustumFilterAttribute
{
    public class CustumExceptionFilter : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is CustumException)
            {
                var exception = (CustumException)context.Exception;
                BaseReturn<object> result = new BaseReturn<object>()
                {
                    Code = exception.GetErrorCode(),
                    Message = exception.Message
                };
                string exceptionStr = JsonConvert.SerializeObject(result);
                context.Result = new JsonResult(result);
            }
            //else if(context.Exception is Exception)
            //{
            //    string msg = string.IsNullOrWhiteSpace(context.Exception.Message) ? "" : context.Exception.Message;
            //    BaseReturn<object> result = new BaseReturn<object>()
            //    {
            //        code = 500,
            //        msg = msg
            //    };
            //    context.Result = new JsonResult(result);
            //}
            else
            {
                string msg = string.IsNullOrWhiteSpace(context.Exception.Message) ? "" : context.Exception.Message;
                BaseReturn<object> result = new BaseReturn<object>()
                {
                    Code = -1,
                    Message = msg
                };
                context.Result = new JsonResult(result);
            }
            context.ExceptionHandled = true;
        }
    }
}
