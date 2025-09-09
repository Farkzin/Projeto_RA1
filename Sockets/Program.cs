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
                JsonLog("servidor_começado", "Socket criado");
                JsonLog("porta_vinculada", "127.0.0.1:12345");
                JsonLog("aguardando_conexao");

                cliente = servidor.AcceptTcpClient();
                JsonLog("cliente_conectado");

                NetworkStream stream = cliente.GetStream();
                byte[] buffer = new byte[512];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    JsonLog("mensagem_recebida", mensagem);

                    string reply = "Eco: " + mensagem;
                    byte[] replyBytes = Encoding.UTF8.GetBytes(reply);
                    stream.Write(replyBytes, 0, replyBytes.Length);
                    JsonLog("mensagem_enviada", reply);
                }

                JsonLog("cliente_desconectado");
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
                JsonLog("servidor_desligado");
            }
        }
    }
}