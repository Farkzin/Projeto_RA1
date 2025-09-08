using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class Program
    {
        static void JsonLog(string evento, string detalhe = "")
        {
            Console.WriteLine("{ \"event\": \"" + evento + "\", \"detail\": \"" + detalhe + "\" }");
        }

        static void Main(string[] args)
        {
            int port = 12345;
            TcpListener servidor = null;
            TcpClient cliente = null;

            try
            {
                servidor = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                servidor.Start();
                JsonLog("server_started", "Socket criado");
                JsonLog("bind_success", "127.0.0.1:12345");
                JsonLog("waiting_connection");

                cliente = servidor.AcceptTcpClient();
                JsonLog("client_connected");

                NetworkStream stream = cliente.GetStream();
                byte[] buffer = new byte[512];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    JsonLog("message_received", mensagem);

                    string reply = "Eco: " + mensagem;
                    byte[] replyBytes = Encoding.UTF8.GetBytes(reply);
                    stream.Write(replyBytes, 0, replyBytes.Length);
                    JsonLog("message_sent", reply);
                }

                JsonLog("client_disconnected");
                stream.Close();
                cliente.Close();
            }
            catch (Exception ex)
            {
                JsonLog("error", ex.Message);
            }
            finally
            {
                if (servidor != null)
                    servidor.Stop();
                JsonLog("server_shutdown");
            }
        }
    }
}