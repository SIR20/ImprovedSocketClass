using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Linq;

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

    public sealed class ImprovedSocketClass
    {
        private Socket selfSocket;
        private Thread ConnectListenThread;
        private Thread MessageListenThread;
        private bool bindingFlag = false;
        private EndPoint remoteEndPoint;

        public delegate void MessageHadnler(byte[] message);
        public event MessageHadnler newMessage;

        public delegate void SocketHandler(ImprovedSocketClass improvedSocket);
        public event SocketHandler newConnect;

        public delegate void SocketCloseHadnler(ImprovedSocketClass imprSocket);
        public event SocketCloseHadnler socketDisconnect;

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

            selfSocket.Bind(endPoint);
            _EndPoint = endPoint;
            if (_ProtocolType == ProtocolT.UDP)
                ReceiveFrom();
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
                    imprSocket.Recieve();
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
            try
            {
                selfSocket.Connect(endPoint);
                _EndPoint = endPoint;
            }
            catch
            {
                throw new Exception("Не удалось подключиться");
            }
            Recieve();
        }

        static byte[] MessageComplete(byte[] msg)
        {
            byte[] result = new byte[1];
            string messageLength = msg.Length.ToString();
            byte[] messageLengthBytes = messageLength.GetBytes();
            result[0] = (byte)messageLengthBytes.Length;
            result = result.Concat(messageLengthBytes).ToArray();
            result = result.Concat(msg).ToArray();
            return result;
        }

        private void Recieve()
        {
            MessageListenThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        byte[] message = new byte[0];
                        bool firstMessage = false;
                        int msgLength = 256;
                        int msgLengthCount = 0;
                        int currLength = 0;
                        while (true)
                        {
                            byte[] currData = new byte[msgLength];
                            int currByteCount = selfSocket.Receive(currData);
                            message = message.Concat(currData).ToArray();
                            currLength += currByteCount;
                            if (!firstMessage)
                            {
                                msgLengthCount = currData[0];
                                msgLength = int.Parse(currData.Skip(1).Take(msgLengthCount).ToArray().GetString());
                                firstMessage = true;
                            }
                            if (currLength >= msgLength)
                            {
                                break;
                            }
                        }
                        newMessage?.Invoke(message.Skip(1 + msgLengthCount).Take(msgLength).ToArray());
                        firstMessage = false;
                    }
                }
                catch
                {
                    // socketDisconnect(this);
                }
            });
            MessageListenThread.Start();
        }

        private void ReceiveFrom()
        {

            MessageListenThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        byte[] message = new byte[0];
                        bool firstMessage = false;
                        int msgLength = 256;
                        int msgLengthCount = 0;
                        int currLength = 0;
                        while (true)
                        {
                            byte[] currData = new byte[msgLength];
                            EndPoint remoteIp = remoteEndPoint == null ? new IPEndPoint(IPAddress.Any, 0) : remoteEndPoint;
                            int currByteCount = selfSocket.ReceiveFrom(currData, ref remoteIp);
                            _RemoteEndPoint = remoteIp;
                            message = message.Concat(currData).ToArray();
                            currLength += currByteCount;
                            if (!firstMessage)
                            {
                                msgLengthCount = currData[0];
                                msgLength = int.Parse(currData.Skip(1).Take(msgLengthCount).ToArray().GetString());
                                firstMessage = true;
                            }
                            if (currLength >= msgLength)
                            {
                                break;
                            }
                        }
                        newMessage?.Invoke(message.Skip(1 + msgLengthCount).Take(msgLength).ToArray());
                        firstMessage = false;
                    }
                }
                catch
                {
                    socketDisconnect(this);
                }
            });
            MessageListenThread.Start();
        }

        public void Disconnect(bool reuseSocket) => selfSocket.Disconnect(reuseSocket);
        public void Close() => selfSocket.Close();
        public int Send(byte[] msg) => selfSocket.Send(MessageComplete(msg));
        public int Send(string msg) => Send(msg.GetBytes());
        public int SendTo(byte[] msg, EndPoint remote_ip)
        {
            _RemoteEndPoint = remote_ip;
            int bCount = selfSocket.SendTo(MessageComplete(msg), remote_ip);
            ReceiveFrom();
            return bCount;
        }
        public int SendTo(string msg, EndPoint remote_ip) => SendTo(msg.GetBytes(), remote_ip);

        public int _ListenCount { get; set; } = 10;
        public bool IsConnect { get; set; }
        public ProtocolT _ProtocolType { get; set; }
        public ProgramStruct _ProgramStruct { get; set; }
        public Socket _SelfSocket { get { return selfSocket; } }
        public EndPoint _EndPoint { get; set; }
        public EndPoint _RemoteEndPoint
        {
            get
            {
                if (_ProtocolType == ProtocolT.TCP)
                    throw new Exception("Это свойство приминимо только для UDP-протокола");
                return remoteEndPoint;
            }
            set
            {
                if (_ProtocolType == ProtocolT.TCP)
                    throw new Exception("Это свойство приминимо только для UDP-протокола");
                remoteEndPoint = value;
            }
        }
    }

    public static class Extensions
    {
        public static byte[] GetBytes(this string s) => Encoding.UTF8.GetBytes(s);
        public static string GetString(this byte[] b) => Encoding.UTF8.GetString(b);
    }
}
