using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class reader
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var listener = new TcpListener(IPAddress.Loopback, 12345);
            listener.Start();
            Console.WriteLine("[SocketReader] Servidor iniciado. Aguardando conexao...");

            TcpClient client = null;
            NetworkStream stream = null;

            try
            {
                client = listener.AcceptTcpClient();
                Console.WriteLine("[SocketReader] Cliente conectado.");
                stream = client.GetStream();

                var buf = new byte[1024];
                int n;
                while ((n = stream.Read(buf, 0, buf.Length)) > 0)
                {
                    var msg = Encoding.UTF8.GetString(buf, 0, n).TrimEnd('\r', '\n');
                    Console.WriteLine("[SocketReader] Mensagem recebida: " + msg);

                    // só ecoa mensagens "normais" para liberar o Read do cliente
                    bool isEcho = msg.StartsWith("Eco:", StringComparison.OrdinalIgnoreCase);
                    if (!isEcho)
                        stream.Write(buf, 0, n);

                    // encerra em qualquer variante que contenha "sair"
                    if (msg.IndexOf("sair", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine("[SocketReader] Encerrando por comando.");
                        break;
                    }
                }

                Console.WriteLine("[SocketReader] Cliente desconectado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SocketReader] Erro: " + ex.Message);
            }
            finally
            {
                try { stream?.Close(); } catch { }
                try { client?.Close(); } catch { }
                try { listener.Stop(); } catch { }
                Console.WriteLine("[SocketReader] Servidor desligado.");
            }
        }
    }
}
