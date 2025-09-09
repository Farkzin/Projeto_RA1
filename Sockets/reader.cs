using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class reader
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
                Console.WriteLine("[SocketReader] Servidor iniciado: Socket criado.");
                Console.WriteLine("[SocketReader] Porta vinculada.");
                Console.WriteLine("[SocketReader] Aguardando conexao...");

                cliente = servidor.AcceptTcpClient();
                Console.WriteLine("[SocketReader] Cliente conectado.");

                NetworkStream stream = cliente.GetStream();
                byte[] buffer = new byte[512];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {                   
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("[SocketReader] Mensagem recebida: " + mensagem);
                }

                Console.WriteLine("[SocketReader] Cliente desconectado.");
                stream.Close();
                cliente.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SocketReader] Erro: " + ex.Message);
            }
            finally
            {
                if (servidor != null)
                    servidor.Stop();
                Console.WriteLine("[SocketReader] Servidor desligado.");
            }
        }
    }
}
