using HttpWebServer.CL.Models.Request;
using HttpWebServer.CL.Models.Response;

namespace HttpWebServer.CL.Models.Middleware
{
    public interface IMiddleware
    {
        Task<bool> Handle(string request, RequestHelper requestHelper, ResponseHelper responseHelper);
    }
}
