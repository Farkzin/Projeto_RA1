using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
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
            string ip = "127.0.0.1";

            TcpClient client = null;
            NetworkStream stream = null;

            try
            {
                client = new TcpClient();
                client.Connect(IPAddress.Parse(ip), port);
                JsonLog("cliente_iniciado");
                JsonLog("conectado_ao_servidor", "127.0.0.1:12345");

                stream = client.GetStream();
                byte[] buffer = new byte[512];

                Console.WriteLine("Digite uma mensagem para enviar ao servidor (ou 'sair'):");
                string msg;
                while ((msg = Console.ReadLine()) != null)
                {
                    if (msg == "sair") break;

                    byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                    stream.Write(msgBytes, 0, msgBytes.Length);
                    JsonLog("mensagem_enviada", msg);

                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        JsonLog("mensagem_recebida", resposta);
                    }
                    else
                    {
                        JsonLog("servidor_desconectado");
                        break;
                    }
                    Console.WriteLine("Digite uma mensagem para enviar ao servidor (ou 'sair'):");
                }
            }
            catch (Exception ex)
            {
                JsonLog("error", ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
                JsonLog("cliente_desligado");
            }
        }
    }
}