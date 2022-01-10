Пример кода для сервера:

```
using System;
using ImprovedSocket;
using System.Net;
 
namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string ips = "ip";
            EndPoint ip = new IPEndPoint(IPAddress.Parse(ips), 13000);
            ImprovedSocketClass server = new ImprovedSocketClass(ProgramStruct.Server, ProtocolT.TCP);
            server.Bind(ip);
            server.StartListen();
            server.newConnect += client => //Новое подключение
            {
                client.newMessage += msg =>//Сообщение от пользователя
                  {
                      Console.WriteLine(msg.GetString());
                      client.Send("Тест");//Отправка сообщения пользователю
                  };
            };
        }
    }
}
```

Пример кода для клиента:
```
using System;
using System.Net;
using ImprovedSocket;
 
namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string ips = "ip";
            EndPoint ip = new IPEndPoint(IPAddress.Parse(ips), 13000);
            ImprovedSocketClass client = new ImprovedSocketClass(ProgramStruct.Client, ProtocolT.TCP);
            client.Connect(ip);
            client.newMessage += msg =>//Сообщение от сервера
            {
                Console.WriteLine(msg.GetString());
                client.Send("Тест");//Отправка сообщения серверу
            };
        }
    }
}
```
