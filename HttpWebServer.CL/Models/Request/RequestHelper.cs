using HttpWebServer.CL.Common;
using System.Net;
using System.Text;

namespace HttpWebServer.CL.Models.Request
{
    public class RequestHelper
    {

        public async Task<(string , HttpStatusCode)> ProcessRequestAsync(string httpRequest, string requestedResource , string projectOutputDirectory)
        {
            string[] requestLines = httpRequest.Split('\n');
            string[] requestLineParts = requestLines[0].Split(' ');
            string method = requestLineParts[0];
            string path = requestLineParts[1];
            string filePath = Path.Combine(projectOutputDirectory, requestedResource);
            if (path.StartsWith("/static/"))
            {
                if (!File.Exists(filePath))
                {
                    return (await HandleGlobalPages(filePath , projectOutputDirectory , CommonString.NotFoundPage) , HttpStatusCode.NotFound);
                }
                else
                {
                    string fileContent = await File.ReadAllTextAsync(filePath);
                    return (fileContent , HttpStatusCode.OK);
                }
            }
            else if (path.StartsWith("/BadRequest"))
            {
                return (await HandleGlobalPages(filePath, projectOutputDirectory, CommonString.BadRequest) , HttpStatusCode.BadRequest);
            }
            else if(path.StartsWith("/InternalServerError"))
            {
                return (await HandleGlobalPages(filePath, projectOutputDirectory, CommonString.InternalServerError) , HttpStatusCode.InternalServerError);
            }
            return (string.Empty , HttpStatusCode.NoContent);
        }

        public string GetRequestedResource(string request)
        {
            string[] requestLines = request.Split('\n');
            string[] requestLineParts = requestLines[0].Split(' ');
            string method = requestLineParts[0];
            string path = requestLineParts[1];
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '/')
                {
                    for (int j = i + 1; j < path.Length; j++)
                    {
                        stringBuilder.Append(path[j]);
                    }
                    break;
                }
            }
            return stringBuilder.ToString();
        }

        public string GetContentType(string resource)
        {
            string extension = Path.GetExtension(resource).ToLower();
            return extension switch
            {
                ".html" => "text/html",
                ".js" => "application/javascript",
                ".css" => "text/css",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".xml" => "application/xml",
                _ => "text/plain",
            };
        }

        private async Task<string> HandleGlobalPages(string filePath , string projectOutputDirectory , string pageName)
        {
            filePath = Path.Combine(projectOutputDirectory, pageName);
            string fileContent = await File.ReadAllTextAsync(filePath);
            return fileContent;
        }

    }
}
