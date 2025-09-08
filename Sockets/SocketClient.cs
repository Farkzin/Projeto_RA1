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
    SOCKET listenSock = INVALID_SOCKET, clientSock = INVALID_SOCKET;
    sockaddr_in serverAddr{ }
    ;
    char buffer[512];
    int port = 12345;

    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        json_log("error", "WSAStartup failed");
        return 1;
    }

    listenSock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listenSock == INVALID_SOCKET)
    {
        json_log("error", "Failed to create socket");
        WSACleanup();
        return 1;
    }
    json_log("server_started", "Socket criado");

    serverAddr.sin_family = AF_INET;
    inet_pton(AF_INET, "127.0.0.1", &(serverAddr.sin_addr));
    serverAddr.sin_port = htons(port);

    if (bind(listenSock, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        json_log("error", "Bind failed");
        closesocket(listenSock);
        WSACleanup();
        return 1;
    }
    json_log("bind_success", "127.0.0.1:12345");

    if (listen(listenSock, 1) == SOCKET_ERROR)
    {
        json_log("error", "Listen failed");
        closesocket(listenSock);
        WSACleanup();
        return 1;
    }
    json_log("waiting_connection");

    clientSock = accept(listenSock, nullptr, nullptr);
    if (clientSock == INVALID_SOCKET)
    {
        json_log("error", "Accept failed");
        closesocket(listenSock);
        WSACleanup();
        return 1;
    }
    json_log("client_connected");

    int bytesReceived;
    while ((bytesReceived = recv(clientSock, buffer, sizeof(buffer) - 1, 0)) > 0)
    {
        buffer[bytesReceived] = '\0';
        json_log("message_received", buffer);

        std::string reply = "Eco: " + std::string(buffer);
        send(clientSock, reply.c_str(), (int)reply.size(), 0);
        json_log("message_sent", reply);
    }
    if (bytesReceived == 0)
    {
        json_log("client_disconnected");
    }
    else if (bytesReceived < 0)
    {
        json_log("error", "Erro na recep  o de dados");
    }

    closesocket(clientSock);
    closesocket(listenSock);
    WSACleanup();
    json_log("server_shutdown");
    return 0;
}