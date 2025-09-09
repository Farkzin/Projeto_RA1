using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class Program
    {
        // Função para mostrar logs em formato JSON (evento + detalhe)
        static void JsonLog(string evento, string detalhe = "")
        {
            Console.WriteLine("{ \"event\": \"" + evento + "\", \"detail\": \"" + detalhe + "\" }");
        }

        static void Main(string[] args)
        {
            int port = 12345;           // Porta usada para a conexão
            string ip = "127.0.0.1";    // IP do servidor (localhost)

            TcpClient client = null;    // Objeto cliente TCP
            NetworkStream stream = null; // Fluxo de dados da conexão

            try
            {
                // Cria o cliente e conecta ao servidor
                client = new TcpClient();
                client.Connect(IPAddress.Parse(ip), port);
                JsonLog("cliente_iniciado");
                JsonLog("conectado_ao_servidor");

                // Obtém o fluxo de dados da conexão
                stream = client.GetStream();
                byte[] buffer = new byte[512]; // Buffer para receber mensagens

                Console.WriteLine("Digite uma mensagem para enviar ao servidor:");
                string msg;

                // Loop para enviar mensagens até digitar "sair"
                while ((msg = Console.ReadLine()) != null)
                {
                    // Converte a mensagem em bytes e envia ao servidor
                    byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                    stream.Write(msgBytes, 0, msgBytes.Length);
                    JsonLog("mensagem_enviada", msg);

                    // Aguarda resposta do servidor
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        // Converte os bytes recebidos em texto
                        string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        JsonLog("mensagem_recebida", resposta);
                    }
                    else
                    {
                        // Se não recebeu nada, servidor desconectou
                        JsonLog("servidor_desconectado");
                        break;
                    }

                    Console.WriteLine("Digite uma mensagem para enviar ao servidor:");
                }
            }
            catch (Exception ex)
            {
                // Caso dê algum erro (ex: servidor não encontrado)
                JsonLog("error", ex.Message);
            }
            finally
            {
                // Fecha o fluxo e a conexão ao final
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
                JsonLog("cliente_desligado");
            }
        }
    }
}
