using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewSchoolLive.DtoModel.Respone;
using Newtonsoft.Json;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace NewSchoolLive.ServiceCollection.CustumFilterAttribute
{
    public class ApiResultActionFilter: ActionFilterAttribute
    {
        private static IRedisDatabase redis;
        public ApiResultActionFilter(IRedisCacheClient _redis)
        {
            redis = _redis.Db0;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception == null)
            {
                ObjectResult result = context.Result as ObjectResult;
                if (result != null)
                {
                    BaseReturn<object> _result = new BaseReturn<object>
                    {
                        Data = result.Value,
                        Code = 200,
                        Message = "ok"
                    };
                    context.Result = new ObjectResult(_result);
                }
            }
            base.OnActionExecuted(context);
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {

            var exist = context.ActionDescriptor.EndpointMetadata.Any(item => item is AllowAnonymousAttribute);
            if (exist)
            {
                base.OnActionExecuting(context);
            }
            else
            {
                var user = context.HttpContext.User.Claims.Where(w => w.Type == "UserId").FirstOrDefault();
                var userId = long.Parse(user.Value);
                if (userId > 0)
                {
                    var token = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                    //token = EncryptHelper.GetInstance().MD5(token);
                    //验证是否是最新的token
                    var cacheToken = redis.GetAsync<string>("userToken_" + userId).ToString();
                    //var cacheToken = (string)CacheHelper.CacheValue("userToken_" + userId);
                    if (string.IsNullOrWhiteSpace(cacheToken))
                    {
                        throw new CustumException("登录失效，请重新登录");
                    }
                    if (token.Equals(cacheToken))
                    {
                        base.OnActionExecuting(context);
                    }
                    else
                    {
                        //throw new LoginErrorException("该账号已在其他地方登录，您已被迫下线");
                    }
                }
                else
                {
                    base.OnActionExecuting(context);
                }
            }
        }

    }
    public class ApiMessageHandler : MessageProcessingHandler
    {
        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var result = response.Content.ReadAsStringAsync().Result;
                BaseReturn<object> results = new BaseReturn<object>();
                results.Code = 401;
                results.Message = "已拒绝为此请求授权。";
                // 返回消息 进行加密
                response.Content = new StringContent(JsonConvert.SerializeObject(results), Encoding.UTF8, "application/json");
            }
            return response;
        }
    }


}
