using System.IO.MemoryMappedFiles;

class Reader
{
    static void Main()
    {
        // Declara variáveis para a memória e mutex incialmente nulas para realizar tentativas de abertura de objetos.
        MemoryMappedFile? mmf = null;
        Mutex? mutex = null;
        int tentativas = 0;   // Contador de tentativas

        // Laço para tentar abrir memória compartilhada, máximo de 50 tentativas.
        while (mmf == null && tentativas < 50)
        {
            try { mmf = MemoryMappedFile.OpenExisting(MemIPC.NomeMem); } // Tenta abrir memória já existente
            catch { Thread.Sleep(200); tentativas++; } // Se ainda não existir, espera 200 ms e incrementa contador de tentativas
        }

        // Se não abriu após 50 tentativas, encerra o programa com erro.
        if (mmf == null) { Console.WriteLine("Erro ao abrir memória após 50 tentativas."); return; }

        tentativas = 0; // Reinicia o contador de tenttivas

        // Laço para tentar abrir o mutex, máximo de 50 tentativas.
        while (mutex == null && tentativas < 50)
        {
            try { mutex = Mutex.OpenExisting(MemIPC.NomeMutex); } // Tenta abrir mutex já existente
            catch { Thread.Sleep(200); tentativas++; } // Se ainda não existir, espera 200 ms e incrementa contador de tentativas
        }

        // Se não abriu após 50 tentativas, encerra o programa com erro.
        if (mutex == null) { Console.WriteLine("Erro ao abrir mutex após 50 tentativas."); return; }

        using (mmf)    // using (mmf) e using (mutex) garantem que, ao sair desse bloco,
        using (mutex)  // os objetos serão liberados autoamticamente pelo C#
        {
            // Laço para leitura de mensagens. Termina quando o usuário digitar 'sair' no escritor e a ExitFlag se tornar 1.
            while (true)
            {
                mutex.WaitOne();  // Aguarda o mutex ficar disponível

                // Apenas var teria que chamar Dispose(), com using o C# garante que o recurso é limpo após uso.
                using (var reader = mmf.CreateViewAccessor()) // Cria um view accessor para acessar a memória compartilhada
                {
                    byte exit = reader.ReadByte(MemIPC.ExitFlag); // Lê a flag de saída, na posição 512
                    if (exit == 1)  // Se a flag = 1, o escritor sinalizou para encerrar
                    {
                        Console.WriteLine("Escritor finalizado. Encerrando leitor.");
                        mutex.ReleaseMutex();  // Libera o mutexantes de sair
                        break;
                    }

                    // Cria um buffer fixo de 256 chars.
                    var buffer = new char[MemIPC.NumChars];

                    // Copia array da memória pro buffer.
                    reader.ReadArray(
                        0,                // Posição inicial dentro da memória compartilhada
                        buffer,           // O array que será gravado 
                        0,                // Posição inicial dentro do array 
                        MemIPC.NumChars   // Quantidade de chars que serão copiados
                        );

                    var mensagem = new string(buffer).TrimEnd('\0'); // COnverte o buffer em string removendo '\0' do final

                    // Só imprime se a mensagem não for nula ou vazia, evita que a mensagem seja lida e impressa em loop infinito.
                    if (!string.IsNullOrEmpty(mensagem))
                    {
                        Console.WriteLine($"Mensagem lida: {mensagem}");

                        // Limpa mensagem no buffer.
                        Array.Clear(
                            buffer,         // Array a ser limpo
                            0,              // Posição inicial dentro do array
                            buffer.Length   // Quantidade de char a limpar
                            );

                        // Limpa mensagem na memória compartilhada.
                        reader.WriteArray(
                            0,               // Posição inicial dentro da memória compartilhada
                            buffer,          // O array que será gravado
                            0,               // Posição inicial dentro do array
                            MemIPC.NumChars  // Quantidade de chars que serão copiados
                            );
                    }
                }
                mutex.ReleaseMutex(); // Libera o mutex, permitindo que o escritor acesse a memória
                Thread.Sleep(200); // Evita que o loop rode em alta velocidade ocupando 100 % da CPU quando não há mensagens novas
            }
        }
    }
}