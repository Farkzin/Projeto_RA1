using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 12345;
            TcpListener servidor = null;
            TcpClient cliente = null;

            try
            {
                servidor = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                servidor.Start();
                Console.WriteLine("Servidor iniciado: Socket criado");
                Console.WriteLine("Porta vinculada");
                Console.WriteLine("Aguardando conexao...");

                cliente = servidor.AcceptTcpClient();
                Console.WriteLine("Cliente conectado");

                NetworkStream stream = cliente.GetStream();
                byte[] buffer = new byte[512];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Mensagem recebida: " + mensagem);

                    string reply = "Eco: " + mensagem;
                    byte[] replyBytes = Encoding.UTF8.GetBytes(reply);

                    stream.Write(replyBytes, 0, replyBytes.Length);
                    Console.WriteLine("Mensagem enviada: " + reply);
                }

                Console.WriteLine("Cliente desconectado");
                stream.Close();
                cliente.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
            finally
            {
                if (servidor != null)
                    servidor.Stop();
                Console.WriteLine("Servidor desligado");
            }
        }
    }
}
