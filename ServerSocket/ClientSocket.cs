using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using LitJson;
using System.IO;
namespace ServerSocket
{
    public class ClientSocket
    {
        /// <summary>
        /// 所有客户端管理类
        /// </summary>
        public SocketManager mSocketManager;

        public Socket mSocket;
        /// <summary>
        /// 消息接收线程   
        /// </summary>
        public Thread mReceiveTherad;
        /// <summary>
        /// 处理消息的线程
        /// </summary>
        public Thread mHanderRecvMsgThread;
        /// <summary>
        /// 接收到的消息队列
        /// </summary>
        public Queue<string> mRecvMsgQueue = new Queue<string>();
         /// <summary>
        /// 数据缓冲池
        /// </summary>
        public byte[] mBuffer;

        //是否在放间内
        public bool mIsInRoom;

        //是否是hololens
        public bool isHolo;

        /// <summary>
        /// 数据拼接池
        /// </summary>
        public MemoryStream mMemoryStream = new MemoryStream();
        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <param name="socket"></param>
        public void Init(Socket socket, SocketManager socketManager)
        {
            mSocketManager = socketManager;
            mSocket = socket;
            //创建一个线程
            mReceiveTherad = new Thread(ReceiveClientMsg);
            //开启线程
            mReceiveTherad.Start();

            mHanderRecvMsgThread = new Thread(HanderRecvMsg);
            mHanderRecvMsgThread.Start();
        }
        /// <summary>
        /// 接收客户端消息
        /// </summary>
        public void ReceiveClientMsg()
        {
            while (mSocket.Connected)
            {
                try
                {
                    mBuffer = new byte[512];
                    int lenght = mSocket.Receive(mBuffer, 0, mBuffer.Length, SocketFlags.None);
                    if (lenght==0)
                    {
                        CloseSocket();
                    }
                    //写入数据拼接池
                    mMemoryStream.Write(mBuffer,0,lenght);
                    //读取到的下标
                    int redIndex = 0;
                    //是否开始拆包
                    bool isUnpackFinsh = false;
                    //接收的数据
                    byte[] recvMsgBytes=  mMemoryStream.ToArray();
                    //消息内容大小
                    int msgContentSize = 0;
                    //开始拆包
                    while (isUnpackFinsh == false)
                    {
                        //判断当前接收到的数据长度，是否小于包头的长度
                        if (recvMsgBytes.Length<4+redIndex)
                        {
                            //如果小于 拆包完成 进行数据拼接
                            // 数据清掉
                            mMemoryStream.Dispose();
                            mMemoryStream.Close();
                            mMemoryStream = new MemoryStream();
                            mMemoryStream.Write(recvMsgBytes, redIndex,recvMsgBytes.Length-redIndex);
                            isUnpackFinsh = true;
                        }
                        else
                        {
                            //取出包头 得到消息的长度
                            msgContentSize= BitConverter.ToInt32(recvMsgBytes, redIndex);
                            //如果接收的数据的长度，小于消息的长度 说明消息不完整
                            //获取剩余的长度
                            int overLength = recvMsgBytes.Length - redIndex;
                            if (overLength < msgContentSize)
                            {
                                //如果小于 拆包完成 进行数据拼接
                                mMemoryStream.Dispose();
                                mMemoryStream.Close();
                                mMemoryStream = new MemoryStream();
                                mMemoryStream.Write(recvMsgBytes, redIndex, recvMsgBytes.Length - redIndex);
                                isUnpackFinsh = true;
                            }
                            else
                            {
                                //否则说明数据是完整的 
                                string jsonmsg = System.Text.Encoding.UTF8.GetString(recvMsgBytes, redIndex+4, msgContentSize-4);
                                mRecvMsgQueue.Enqueue(jsonmsg);
                                //读取下标往拨
                                redIndex += msgContentSize;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    CloseSocket();
                }
            }
        }
        /// <summary>
        /// 处理接收消息
        /// </summary>
        public void HanderRecvMsg()
        {
            while (true)
            {
                if (mRecvMsgQueue.Count>0)
                {
                    string jsonmsg = mRecvMsgQueue.Dequeue();
                    MsgData msgData = JsonMapper.ToObject<MsgData>(jsonmsg);
                    switch (msgData.msgType)
                    {

                        case MsgType.Login:
                            mIsInRoom = true;
                            isHolo = msgData.name.Equals("holo")?true:false;
                            MsgData data = new MsgData();
                            data.msgType = MsgType.Login;
                            data.name = msgData.name;
                            data.msg = msgData.msg;
                            string jmsg = JsonMapper.ToJson(data);
                            SendMsgToRoomAllClient(jmsg);
                            break;
                        case MsgType.HideCube:
                            MsgData chatdata = new MsgData();
                            chatdata.msgType = MsgType.HideCube;
                            chatdata.msg = msgData.msg;
                            chatdata.name = msgData.name;
                            string jsMsg = JsonMapper.ToJson(chatdata);
                            SendMsgToRoomAllClient(jsMsg);
                            break;
                    }
                    Console.WriteLine("收到客户端发来的消息：" + jsonmsg);
                }
            }
        }
        /// <summary>
        /// 发送消息到客户端
        /// </summary>
        public void SendMsgToClient(string msg)
        {
            try
            {
                byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(msg);

                //取出消息体的长度 +4  4代表的是包体在Byte字节内所占的长度
                int msglength = msgBytes.Length + 4;
                //把包头转为byte数组
                byte[] headBytes = BitConverter.GetBytes(msglength);
                MemoryStream stream = new MemoryStream();
                //写入包头
                stream.Write(headBytes, 0, headBytes.Length);
                //写入包体
                stream.Write(msgBytes, 0, msgBytes.Length);
                //要发送的字节数据
                byte[] sendbytes = stream.ToArray();
                stream.Dispose();
                stream.Close();

                int length = mSocket.Send(sendbytes, 0, sendbytes.Length, SocketFlags.None);
                Console.WriteLine("发送一条消息到客户端消息长度:" + length);
                Thread.Sleep(50);
            }
            catch (Exception e)
            {
                CloseSocket();
            }
        }
        /// <summary>
        /// 发送消息到房间内所有的hololens客户端
        /// </summary>
        public void SendMsgToRoomAllClient(string jsonmsg)
        {
            for (int i = 0; i < mSocketManager.mAllClientSocketList.Count; i++)
            {
                ClientSocket clientSocket = mSocketManager.mAllClientSocketList[i];
                //如果这个用户在房间内 我们才进行消息的转发
                //如果这个用户在房间内 我们才进行消息的转发
                if (clientSocket != null&& clientSocket.mIsInRoom&&clientSocket.isHolo == true)
                /*if (clientSocket != null)*/
                {
                    clientSocket.SendMsgToClient(jsonmsg);
                }
            }
        }
        /// <summary>
        /// 关闭客户端
        /// </summary>
        public void CloseSocket()
        {
            Console.WriteLine("关闭一个Socket...");
            if (mSocket==null)
            {
                return;
            }
            mSocket.Close();
            mSocket = null;
            mSocketManager.mAllClientSocketList.Remove(this);
            Console.WriteLine(" 移除一个客户端 客户端数量：" + mSocketManager.mAllClientSocketList.Count);
            // 关闭线程
            mReceiveTherad.Abort();
        }
    }
}
