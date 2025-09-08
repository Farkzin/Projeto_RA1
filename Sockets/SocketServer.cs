#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>

#pragma comment(lib, "ws2_32.lib")

using System.Linq;
using System.Net.Sockets;

void json_log(const std::string& event, const std::string& detail = "")
{
    std::cout << "{ \"event\": \"" << event << "\", \"detail\": \"" << detail << "\" }" << std::endl;
}

int main()
{
    WSADATA wsaData;
    SOCKET sock = INVALID_SOCKET;
    sockaddr_in serverAddr{ }
    ;
    char buffer[512];
    int port = 12345;

    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        json_log("error", "WSAStartup failed");
        return 1;
    }

    sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock == INVALID_SOCKET)
    {
        json_log("error", "Failed to create socket");
        WSACleanup();
        return 1;
    }
    json_log("client_started");

    serverAddr.sin_family = AF_INET;
    inet_pton(AF_INET, "127.0.0.1", &(serverAddr.sin_addr));
    serverAddr.sin_port = htons(port);

    if (connect(sock, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        json_log("error", "Erro ao conectar no servidor");
        closesocket(sock);
        WSACleanup();
        return 1;
    }
    json_log("connected_to_server", "127.0.0.1:12345");

    std::string msg;
    std::cout << "Digite uma mensagem para enviar ao servidor (ou 'sair'):\n";
    while (std::getline(std::cin, msg))
    {
        if (msg == "sair") break;

        send(sock, msg.c_str(), (int)msg.size(), 0);
        json_log("message_sent", msg);

        int bytesReceived = recv(sock, buffer, sizeof(buffer) - 1, 0);
        if (bytesReceived > 0)
        {
            buffer[bytesReceived] = '\0';
            json_log("message_received", buffer);
        }
        else
        {
            json_log("server_disconnected");
            break;
        }
        std::cout << "Digite uma mensagem para enviar ao servidor (ou 'sair'):\n";
    }
    closesocket(sock);
    WSACleanup();
    json_log("client_shutdown");
    return 0;
}