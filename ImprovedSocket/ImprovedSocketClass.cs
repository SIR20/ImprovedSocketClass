using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace ImprovedSocket
{
    public enum ProtocolT
    {
        TCP,
        UDP
    }

    public enum ProgramStruct
    {
        Server,
        Client
    }

    public enum Message{
        Begin = 1,
        End = 0
    }

    public sealed class ImprovedSocketClass
    {
        private Socket selfSocket;
        private Thread ConnectListenThread;
        private Thread MessageListenThread;
        private bool bindingFlag = false;

        public delegate void MessageHadnler(byte[] message);
        public event MessageHadnler newMessage;

        public delegate void SocketHandler(ImprovedSocketClass improvedSocket);
        public event SocketHandler newConnect;

        public ImprovedSocketClass(ProgramStruct pStruct, ProtocolT pType)
        {
            _ProgramStruct = pStruct;
            _ProtocolType = pType;

            if (_ProtocolType == ProtocolT.TCP)
                selfSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                selfSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        private ImprovedSocketClass(Socket self)
        {
            switch (self.ProtocolType)
            {
                case ProtocolType.Tcp:
                    _ProtocolType = ProtocolT.TCP;
                    break;

                case ProtocolType.Udp:
                    _ProtocolType = ProtocolT.UDP;
                    break;

                default:
                    throw new Exception("Данный сокет не поддерживается классом");
            }
            selfSocket = self;
        }

        public void Bind(EndPoint endPoint)
        {
            if (_ProgramStruct == ProgramStruct.Client)
                throw new Exception("Данный метод не предназначен для Клиента");

            if (_ProtocolType == ProtocolT.UDP)
                throw new Exception("Данный метод не предназначен для протокола UDP");

            selfSocket.Bind(endPoint);
            _EndPoint = endPoint;
            bindingFlag = true;

        }

        public void StartListen()
        {
            if (_ProtocolType == ProtocolT.UDP)
                throw new Exception("Данный метод не предназначен для протокола UDP");

            if (_ProgramStruct == ProgramStruct.Client)
                throw new Exception("Данный метод не предназначен для Клиента");

            if (!bindingFlag)
                throw new Exception("Перед вызовом этого метода нужно вызвать метод Bind");

            selfSocket.Listen(_ListenCount);
            ConnectListenThread = new Thread(() =>
            {
                while (true)
                {
                    Socket client = selfSocket.Accept();
                    ImprovedSocketClass imprSocket = new ImprovedSocketClass(client);
                    newConnect?.Invoke(imprSocket);
                }
            });
            ConnectListenThread.Start();
        }

        public void Connect(EndPoint endPoint)
        {
            if (_ProgramStruct == ProgramStruct.Server)
                throw new Exception("Данный метод не предназначен для Сервера");

            if (_ProtocolType == ProtocolT.UDP)
                throw new Exception("Данный метод не предназначен для протокола UDP");

            selfSocket.Connect(endPoint);
            _EndPoint = endPoint;
        }

        public void Recieve()
        {
            MessageListenThread = new Thread(() =>
            {
                byte[] data = new byte[256];
                while (true)
                {
                    selfSocket.Receive(data);
                    newMessage?.Invoke(data);
                }
            });
            MessageListenThread.Start();
        }
        public int Send(byte[] msg) => selfSocket.Send(msg);
        public int Send(string msg) => Send(msg.GetBytes());

        public int _ListenCount { get; set; } = 10;
        public ProtocolT _ProtocolType { get; set; }
        public ProgramStruct _ProgramStruct { get; set; }
        public Socket _SelfSocket { get { return selfSocket; } }
        public EndPoint _EndPoint { get; set; }
    }

    public static class Extensions
    {
        public static byte[] GetBytes(this string s) => Encoding.UTF8.GetBytes(s);
        public static string GetString(this byte[] b) => Encoding.UTF8.GetString(b);
    }
}
