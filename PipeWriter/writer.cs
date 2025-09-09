using System.IO.Pipes;  // Importa classes para trabalhar com Named Pipes
using System.Text;      // Importa utilidades de codificação e decodificação de texto

class PipeWriter
{
    static void Main()
    {
        // Apenas var teria que chamar Dispose(), com using o C# garante que o recurso é limpo após uso.
        using (var pipe = new NamedPipeServerStream(  // Cria um servidor de Named Pipe
            PipeConfig.PipeName,         // Nome do pipe, precisa ser igual ao processo leitor
            PipeDirection.Out,           // Este servidor só vai escrever dados no pipe
            1,                           // Quantidade máxima de conxões permitidas (1 leitor)
            PipeTransmissionMode.Byte    // Transmissão de bytes
            ))
        {
            Console.WriteLine("[PipeWriter] Aguardando conexao do leitor...");
            pipe.WaitForConnection(); // Bloqueia até que um cliente (leitor) se conecte
            Console.WriteLine("[PipeWriter] Leitor conectado.");

            Console.WriteLine("[PipeWriter] Iniciando escrita...");

            // Laço para ficar enviando mensagens até digitar "sair"
            while (true)
            {
                string mensagem = Console.ReadLine();

                // Se digitar "sair" finaliza o laço e o programa
                if (mensagem == "sair") break;

                byte[] buffer = Encoding.UTF8.GetBytes(mensagem); // Converte a string em array de bytes
                pipe.Write(        // Envia os bytes para o leitor
                    buffer,        // Array com os dados a serem enviados
                    0,             // Posição inicial no array, começar a enviar os dados pelo início do array
                    buffer.Length  // Número total de bytes a serem enviados
                    );
                pipe.Flush(); // Os dados são enviados imediatamente ao leitor

                Console.WriteLine($"[PipeWriter] Mensagem escrita: {mensagem}");
            }
        }
    }
}