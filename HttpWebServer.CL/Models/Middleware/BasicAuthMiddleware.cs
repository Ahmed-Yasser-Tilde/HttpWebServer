using HttpWebServer.CL.Models.Request;
using HttpWebServer.CL.Models.Response;

namespace HttpWebServer.CL.Models.Middleware
{
    public class BasicAuthMiddleware : IMiddleware
    {

        public async Task<bool> Handle(string request , RequestHelper requestHelper , ResponseHelper responseHelper)
        {
            string[] authHeader = requestHelper.GetAuthorizationLine(request);
            if (authHeader is null)
            {
                return await Task.FromResult(false);
            }
            return await Task.FromResult(true);
        }
    }
}
