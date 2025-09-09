using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class writer
    {
        static void Main(string[] args)
        {
            int port = 12345;
            string ip = "127.0.0.1";

            TcpClient client = null;
            NetworkStream stream = null;

            try
            {
                client = new TcpClient();
                Console.WriteLine("[SocketWriter] Cliente iniciado.");
                client.Connect(IPAddress.Parse(ip), port);
                Console.WriteLine("[SocketWriter] Cliente conectado ao servidor.");

                stream = client.GetStream();
                byte[] buffer = new byte[512];
                

                while (true)
                {
                    string msg = Console.ReadLine();
                    if (msg == "sair") break;

                    byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                    stream.Write(msgBytes, 0, msgBytes.Length);
                    Console.WriteLine("[SocketWriter] Mensagem enviada: " + msg);

                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("[SocketWriter] Mensagem recebida: " + resposta);
                    }
                    else
                    {
                        Console.WriteLine("[SocketWriter] Servidor desconectado.");
                        break;
                    }

                    Console.WriteLine("[SocketWriter] Digite uma mensagem para enviar ao servidor:");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
                Console.WriteLine("Cliente desligado.");
            }
        }
    }
}
