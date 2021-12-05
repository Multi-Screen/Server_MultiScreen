using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ServerSocket
{
    public class SocketManager
    {
        public Socket mSocket;
        /// <summary>
        /// 所有客户端Socket集合
        /// </summary>
        public List<ClientSocket> mAllClientSocketList = new List<ClientSocket>();
        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            //创建一个Socket
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //创建IP和端口
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("172.22.20.231"), 9000);
            Console.WriteLine("绑定IP和端口");
            //绑定IP端口
            mSocket.Bind(endPoint);
            Console.WriteLine("启动服务器...");
            //开启监听
            mSocket.Listen(100);
            Console.WriteLine("服务器启动成功 监听人数 100...");
            while (true)
            {
                Console.WriteLine("等待客户端连接...");
                Socket socket = mSocket.Accept();
                Console.WriteLine("一个客户端连接过来了!");
                ClientSocket clientSocket = new ClientSocket();
                clientSocket.Init(socket,this);
                mAllClientSocketList.Add(clientSocket);
                Console.WriteLine("当前连接的数量："+ mAllClientSocketList.Count);
            }
        }
    }
}
