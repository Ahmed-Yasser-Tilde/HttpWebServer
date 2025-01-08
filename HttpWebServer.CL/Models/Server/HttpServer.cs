using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HttpWebServer.CL.Models.Server
{
    public class HttpServer
    {
        private static readonly Lazy<HttpServer> _instance = new Lazy<HttpServer>(() => new HttpServer());

        private HttpServer(){}

        public static HttpServer Instance => _instance.Value;

        private TcpListener _tcpListener;

        public void Initialize(string ipAddress, string port)
        {
            IPEndPoint endPoint = CreateIPEndPoint(ipAddress, port);
            _tcpListener = new TcpListener(endPoint);
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
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Request:\n" + request);

                    string response = ProcessRequestAsync(request);
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseData, 0, responseData.Length);
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

        public string ProcessRequestAsync(string request)
        {
            string[] requestLines = request.Split('\n');
            string[] requestLineParts = requestLines[0].Split(' ');
            string method = requestLineParts[0];
            string path = requestLineParts[1];
            return "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n<h1>Welcome to the Home Page!</h1>";
        }
    }
}
