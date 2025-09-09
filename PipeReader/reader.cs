using System.IO.Pipes;  // Importa classes para trabalhar com Named Pipes
using System.Text;      // Importa utilidades de codificação e decodificação de texto

class PipeReader
{
    static void Main()
    {
        // Apenas var teria que chamar Dispose(), com using o C# garante que o recurso é limpo após uso.
        using (var pipe = new NamedPipeClientStream(  // Cria um cliente de Named Pipe
            ".",                    // Significa "máquina local", o pipe está no mesmo computador
            PipeConfig.PipeName,    // Nome do pipe, precisa ser igual ao processo escritor
            PipeDirection.In        // Este cliente só vai ler dados do pipe
            ))
        {
            Console.WriteLine("[PipeReader] Conectando ao escritor...");
            pipe.Connect(); // Tenta se conectar ao servidor (escritor). Fica bloqueado até o escritor abrir o pipe
            Console.WriteLine("[PipeReader] Escritor conectado.");

            byte[] buffer = new byte[PipeConfig.TamBuffer];  // Cria um buffer de tamanho fixo para armazenar o que for recebido
            int bytesLidos;  // Vai guardar a quantidade de bytes lidos cada vez

            // pipe.Read(buffer, 0, buffer.Length)
            // Lê dados do pipe
            // Escreve dentro do array buffer
            // Começa no índice 0 do buffer
            // Lê até no máximo o tamanho de bytes do buffer (buffer.Length)
            // Retorna um int com a quantidade de bytes realmente lidos
            while ((bytesLidos = pipe.Read(buffer, 0, buffer.Length)) > 0) // Caso o valor retornado pelo pipe.Read seja 0, significa que o escritor fechou o pipe
            {
                string mensagem = Encoding.UTF8.GetString(  // Converte os bytes do buffer em string
                    buffer,      // Array de bytes a ser lido
                    0,           // Posição inicial no array, começar a enviar os dados pelo início do array
                    bytesLidos   // Quantidade de bytes para converter
                    ); 

                Console.WriteLine("[PipeReader] Mensagem recebida: " + mensagem);
            }
        }
    }
}