using System;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class writer
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            using var client = new TcpClient("127.0.0.1", 12345);
            using var stream = client.GetStream();
            stream.ReadTimeout = 200;            // evita bloqueio
            var rx = new byte[1024];

            while (true)
            {
                var line = Console.ReadLine();   // recebido do WPF
                if (line == null) break;

                var tx = Encoding.UTF8.GetBytes(line);
                stream.Write(tx, 0, tx.Length);
                Console.WriteLine("Mensagem enviada: " + line);

                if (line.Equals("sair", StringComparison.OrdinalIgnoreCase))
                    break;                       // NÃO ler resposta, apenas sair

                try
                {
                    int n = stream.Read(rx, 0, rx.Length);
                    if (n > 0)
                        Console.WriteLine("Mensagem recebida: " + Encoding.UTF8.GetString(rx, 0, n));
                }
                catch (IOException) { /* timeout, segue o loop */ }
            }
        }
    }
}
