using HttpWebServer.CL.Common;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace HttpWebServer.CL.Models.Server
{
    public class HttpServer
    {
        #region Properites
        private static readonly Lazy<HttpServer> _instance = new Lazy<HttpServer>(() => new HttpServer());
        public static HttpServer Instance => _instance.Value;
        private TcpListener _tcpListener;
        private string projectOutputDirectory;
        #endregion

        #region Constructor
        private HttpServer() { }
        #endregion

        #region Initialize Server
        public void Initialize(string ipAddress, string port)
        {
            IPEndPoint endPoint = CreateIPEndPoint(ipAddress, port);
            _tcpListener = new TcpListener(endPoint);
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            projectOutputDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, @"..\..\..\..\HttpWebServer.CL\static"));
        }

        private IPEndPoint CreateIPEndPoint(string ipAddress, string port)
        {
            if (!int.TryParse(port, out int parsedPort) || parsedPort < IPEndPoint.MinPort || parsedPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentException("Invalid port number. Port must be a valid integer between 0 and 65535.");
            }

            if (!IPAddress.TryParse(ipAddress, out IPAddress parsedIPAddress))
            {
                throw new ArgumentException("Invalid IP address.");
            }

            return new IPEndPoint(parsedIPAddress, parsedPort);
        }
        #endregion

        #region Start/Stop Server
        public void Start()
        {
            try
            {
                if (_tcpListener == null)
                {
                    throw new InvalidOperationException("Server has not been initialized. Call Initialize() first.");
                }

                _tcpListener.Start();
                Console.WriteLine("Server started successfully.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error occurred while starting the server: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while starting the server: {ex.Message}");
                throw;
            }
        }
        public void Stop()
        {
            try
            {
                if (_tcpListener == null)
                {
                    throw new InvalidOperationException("Server has not been initialized. Call Initialize() first.");
                }

                _tcpListener.Stop();
                Console.WriteLine("Server stopped successfully.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error occurred while stopping the server: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while stopping the server: {ex.Message}");
                throw;
            }
        }
        #endregion


        public async Task<TcpClient> AcceptTcpClientAsync()
        {
            TcpClient client = await _tcpListener.AcceptTcpClientAsync();
            return client;
        }

        public async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Request:\n" + request);

                    string requestedResource = GetRequestedResource(request);
                    string contentType = GetContentType(requestedResource);

                    string response = await ProcessRequestAsync(request, requestedResource);
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseData);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Network error occurred while handling client: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public async Task<string> ProcessRequestAsync(string request, string requestedResource)
        {
            string[] requestLines = request.Split('\n');
            string[] requestLineParts = requestLines[0].Split(' ');
            string method = requestLineParts[0];
            string path = requestLineParts[1];
            if (path.StartsWith("/static/"))
            {
                string filePath = Path.Combine(projectOutputDirectory, requestedResource);
                if (!File.Exists(filePath))
                {
                    return await HandleNotFoundPage(filePath);
                }
                else
                {
                    string fileContent = await File.ReadAllTextAsync(filePath);
                    return $"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n{fileContent}";
                }
            }
            return "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n<h1>Welcome to the Home Page!</h1>";
        }

        #region Process Request Helpers
        private string GetRequestedResource(string request)
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

        private string GetContentType(string resource)
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
        #endregion

        private async Task<string> HandleNotFoundPage(string filePath)
        {
            filePath = Path.Combine(projectOutputDirectory, CommonString.NotFoundPage);
            string fileContent = await File.ReadAllTextAsync(filePath);
            return $"HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\n\r\n{fileContent}";
        }

    }
}