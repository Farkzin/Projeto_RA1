using System.IO.MemoryMappedFiles;  // Fornece classes para trabalhar com memória compartilhada

class Writer
{
    static void Main()
    {
        // Cria ou abre uma área de memória compartilhada no Windows. Se já existir apenas abre, se não, cria.
        // Retorna um objeto MemoryMappedFile que internamente guarda um handle.
        // using var: atalho pra criar o objeto e garantir que será liberado no final com Dispose()
        using var mmf = MemoryMappedFile.CreateOrOpen(
            MemIPC.NomeMem,     // Nome da área de memória
            MemIPC.TamMemoria   // Tamanho da área de memória
            );

        // Cria ou abre um mutex. Se já existir apenas abre, se não, cria.
        // Retorna um objeto Mutex que internamente guarda um handle.
        // using var: atalho pra criar o objeto e garantir que será liberado no final com Dispose()
        using var mutex = new Mutex(
            false,             // Não começa com o mutex já bloqueado
            MemIPC.NomeMutex   // Nome do mutex
            );

        Console.WriteLine("[SharedMemoryWriter] Iniciando escrita..."); 

        // Laço para escrita de mensagens. Termina quando o usuário digitar 'sair'.
        while (true)
        {
            string mensagem = Console.ReadLine() ?? "";

            mutex.WaitOne(); // Trava o mutex e garante que apenas este processo irá acessar a memória.

            // Apenas var teria que chamar Dispose(), com using o C# garante que o recurso é limpo após uso.
            using (var writer = mmf.CreateViewAccessor()) // Cria um view accessor que ficará responsável por escrever na memória compartilhada.
            {
                if (string.Equals(mensagem, "sair", StringComparison.OrdinalIgnoreCase)) // Caso a mensagem digitada seja 'sair'
                {
                    writer.Write(MemIPC.ExitFlag, (byte)1); // Escreve 1 no byte da flag de saída na posição 512
                    mutex.ReleaseMutex();                   // Libera o mutex, permitindo que o leitor acesse a memória
                    break;
                }

                // Cria um buffer fixo de 256 chars.
                var buffer = new char[MemIPC.NumChars];   // Cria array de char de tamanho 256
                int n = Math.Min(                         // Calcula quantos caracteres vão ser copiados
                    mensagem.Length,   // Se for menor que 256, copia todos
                    MemIPC.NumChars    // Se for maior que 256, copia só os 256 primeiros
                    );
                mensagem.CopyTo(                          // Copia mensagem pro buffer
                    0,                 // Começa no índice 0 da string original
                    buffer,            // Copiar para variável buffer  
                    0,                 // Começa a copiar desde o índice 0 do buffer
                    n                  // Quantidade de caracteres a copiar
                    );

                // Escreve a mensagem na memória compartilhada e zera flag de saída.
                writer.WriteArray(
                    0,               // Posição inicial dentro da memória compartilhada
                    buffer,          // O array que será gravado
                    0,               // Posição inicial dentro do array
                    MemIPC.NumChars  // Quantidade de chars que serão copiados
                    );
                writer.Write(
                    MemIPC.ExitFlag, // Posição na memória compartilhada onde gravar
                    (byte)0          // Valor a ser escrito, aqui um byte de valor 0
                    );

                Console.WriteLine($"[SharedMemoryWriter] Mensagem escrita: {mensagem}");
            }
            mutex.ReleaseMutex(); // Libera o mutex, permitindo que o leitor acesse a memória
        }
    }
}