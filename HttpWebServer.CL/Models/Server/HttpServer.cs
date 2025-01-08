using HttpWebServer.CL.Common;
using HttpWebServer.CL.Models.Request;
using HttpWebServer.CL.Models.Response;
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
        private RequestHelper _requestHelper;
        private ResponseHelper _responseHelper;
        private TcpListener _tcpListener;
        private string _projectOutputDirectory;
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
            _projectOutputDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, @"..\..\..\..\HttpWebServer.CL\static"));
            _requestHelper = new RequestHelper();
            _responseHelper = new ResponseHelper();
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

        #region Client Handling
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
                    string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Request:\n" + httpRequest);

                    string requestedResource = _requestHelper.GetRequestedResource(httpRequest);
                    string contentType = _requestHelper.GetContentType(requestedResource);

                    (string , HttpStatusCode) response = await _requestHelper.ProcessRequestAsync(httpRequest, requestedResource , _projectOutputDirectory);
                    string responseBody = response.Item1;
                    HttpStatusCode responseStatus = response.Item2;

                    byte[] responseData = _responseHelper.ProcessResponseAsync(contentType, responseBody , responseStatus);
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
        #endregion
    }
}