using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class Program
    {
        // Função para registrar eventos no console em formato JSON
        static void JsonLog(string evento, string detalhe = "")
        {
            Console.WriteLine("{ \"event\": \"" + evento + "\", \"detail\": \"" + detalhe + "\" }");
        }

        static void Main(string[] args)
        {
            int port = 12345;              // Porta do servidor
            TcpListener servidor = null;   // Objeto que escuta conexões
            TcpClient cliente = null;      // Objeto para o cliente conectado

            try
            {
                // Cria o servidor TCP ouvindo no IP e porta definidos
                servidor = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                servidor.Start(); // Inicia o servidor
                JsonLog("servidor_começado", "Socket criado");
                JsonLog("porta_vinculada");
                JsonLog("aguardando_conexao");

                // Aguarda até que um cliente se conecte
                cliente = servidor.AcceptTcpClient();
                JsonLog("cliente_conectado");

                // Cria fluxo para troca de mensagens
                NetworkStream stream = cliente.GetStream();
                byte[] buffer = new byte[512]; // Buffer para receber dados

                int bytesRead;
                // Enquanto o cliente mandar dados
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Converte os bytes recebidos em string
                    string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    JsonLog("mensagem_recebida", mensagem);

                    // Cria a resposta (eco da mensagem)
                    string reply = "Eco: " + mensagem;
                    byte[] replyBytes = Encoding.UTF8.GetBytes(reply);

                    // Envia de volta ao cliente
                    stream.Write(replyBytes, 0, replyBytes.Length);
                    JsonLog("mensagem_enviada", reply);
                }

                // Se sair do loop, o cliente fechou a conexão
                JsonLog("cliente_desconectado");
                stream.Close();
                cliente.Close();
            }
            catch (Exception ex)
            {
                // Captura qualquer erro (ex: porta em uso, falha de conexão)
                JsonLog("error", ex.Message);
            }
            finally
            {
                // Garante que o servidor será parado no fim
                if (servidor != null)
                    servidor.Stop();
                JsonLog("servidor_desligado");
            }
        }
    }
}
