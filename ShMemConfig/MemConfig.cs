// Classe para definir constantes nos dois programas
public static class MemIPC
{
    public const string NomeMem = "shm";           // Nome do objeto de memória compartilhada
    public const string NomeMutex = "shm_mutex";   // Nome do mutex

    public const int NumChars = 256;               // Tamanho fixo do buffer de mensagens em caracteres
    public const long NumBytes = NumChars * 2L;    // Tamanho em bytes do buffer, cada char ocupa 2 bytes
    public const long ExitFlag = NumBytes;         // Posição da memória onde fica a flag de saída, índice 0 a 511 é a mensagem e o índice 512 é a flag de saída
    public const long TamMemoria = NumBytes + 1;   // Tamanho do bloco de memória, 512 bytes pra mensagem + 1 byte pra flag
}