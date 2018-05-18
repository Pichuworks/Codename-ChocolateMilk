using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer_Client
{
    public partial class FormClientMain : Form
    {
        // 连接的服务器 - 实例对象
        private Socket Server;
        // 服务器IP
        private string ServerIP;
        // 服务器端口
        private string ServerPort;
        // 是否处于连接状态
        private bool isConnectStart;
        // 接收到的消息字符串
        private string ReceiveMsgStr = "";
        // 学号
        private string stdNo = "";
        // 姓名
        private string stdName = "";

        public FormClientMain()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            label5.Text = "[离线]";
            label6.Text = "[离线]";
            label7.Text = "[离线]";

            label8.Text = "[未启用]";

            label13.Text = "[未启用]";
            label14.Text = "[未启用]";
            label15.Text = "[未启用]";
            label16.Text = "[未启用]";
        }

        /// <summary>
        /// 客户端上线/下线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (isConnectStart)
            {
                // 调用关闭服务器方法
                DisconConnect();
                return;
            }

            new Thread(ConnectServerThread).Start();

            OutputLog("[客户端正在上线]");
        }

        /// <summary>
        /// 服务器连接线程
        /// </summary>
        public void ConnectServerThread()
        {
            ServerIP = textBox1.Text;

            ServerPort = textBox2.Text;

            if (ConnectServer(ServerIP, ServerPort))
            {
                new Thread(ReceiveMessage).Start();
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="serverIP">服务器IP</param>
        /// <param name="serverPort">服务器端口</param>
        /// <returns></returns>
        public bool ConnectServer(string serverIP, string serverPort)
        {
            try
            {
                // 设定服务器IP地址  
                IPAddress ip = IPAddress.Parse(ServerIP);
                // 实例化服务器对象
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // 连接服务器
                Server.Connect(ip, int.Parse(ServerPort));
                // 调用方法检查连接状态 返回结果
                return CheckConnectResult();
            }
            catch (Exception ex)
            {
                OutputLog("[客户端无法上线] " + ex.Message.ToString(), false, true);
                return false;
            }
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>连接成功/失败</returns>
        public bool CheckConnectResult()
        {
            // 接收消息字符串
            string receiveResult = ReceiveMessageStr();

            // 判断接收到的消息是否包含 #connect#ok# 是则表示连接成功
            if (Regex.IsMatch(receiveResult, "#connect#ok#"))
            {
                // 输出日志
                OutputLog("[客户端上线] 服务器IP: " + ServerIP + " Port: " + ServerPort);

                // 设置按钮文本及窗口标题
                button1.Text = "断开";
                Text = "[在线] 客户端仪表盘 - 服务器IP: " + ServerIP + " Port: " + ServerPort;

                // 仪表盘状态
                label5.Text = ServerIP;
                label6.Text = ServerPort;
                label7.Text = "[联机]";

                // 设置连接状态
                isConnectStart = true;

                return true;
            }
            else // 否则连接失败
            {
                // 输出
                OutputLog("[客户端无法上线] 服务器IP: " + ServerIP + " Port: " + ServerPort);

                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisconConnect()
        {
            // 通知服务器断开连接
            Server.Send(Encoding.Unicode.GetBytes("#connect#discon#" + Server.Handle.ToString() + "#"));

            // 释放相关资源
            Server.Dispose();
            // 关闭连接
            Server.Close();

            // 重置服务器对象
            Server = null;
            // 重置连接状态
            isConnectStart = false;

            OutputLog("[客户端下线]");
            button1.Text = "连接";
            Text = "[离线] 客户端仪表盘";
        }

        /// <summary>
        /// 释放服务器连接资源
        /// </summary>
        public void DisconServerSocket()
        {
            // 重置服务器对象
            Server = null;
            // 重置连接状态
            isConnectStart = false;

            OutputLog("[客户端下线]");
            button1.Text = "连接";
            Text = "[离线] 客户端仪表盘";
        }

        /// <summary>
        /// 接收服务器信息
        /// </summary>
        public void ReceiveMessage()
        {
            try
            {
                // 定义Byte数组
                byte[] result = new byte[9000];

                // 接收
                Server.Receive(result);

                // 转换成字符串
                string receiveResult = Encoding.Unicode.GetString(result);

                // 消息 - 连接关闭
                if (Regex.IsMatch(receiveResult, "#connect#close#"))
                {
                    // 输出
                    OutputLog("[服务器已关闭连接]");
                    // 调用方法断开连接
                    DisconConnect();

                    return;
                }

                // 保存消息字符串 用于异步访问
                ReceiveMsgStr = receiveResult;

                // 开辟新线程运行本方法
                new Thread(ReceiveMessage).Start();

                // 结束线程
                Thread.CurrentThread.Suspend();
                Thread.CurrentThread.Abort();

            }
            catch (Exception ex)
            {
                // 丢弃错误
                if (ex.GetType().Name == "SocketException")
                {
                    SocketException sEx = ex as SocketException;

                    if (sEx.ErrorCode == 10053)
                    {
                        return;
                    }
                }

                // 断开连接
                DisconServerSocket();

                // 输出
                OutputLog(ex.Message.ToString(), false, true);
            }
        }

        /// <summary>
        /// 接收服务器消息 [字符串]
        /// </summary>
        /// <returns></returns>
        public string ReceiveMessageStr()
        {
            try
            {
                // 定义字节数组
                byte[] result = new byte[9000];

                // 接收消息
                Server.Receive(result);

                // 将Byte转换成字符串
                string receiveResult = Encoding.Unicode.GetString(result);

                // 返回转换后的字符串
                return receiveResult;
            }
            catch (Exception ex)
            {
                // 输出
                OutputLog(ex.Message.ToString(), false, true);

                return "";
            }
        }

        /// <summary>
        /// 打日志
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="isErrorLog">是否是错误日志</param>
        /// <param name="logFrom">日志来源</param>
        public void OutputLog(string log, bool isFromServer = false, bool isErrorLog = false, string logFrom = "")
        {
            if (isFromServer == true)
            {
                textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][Server] " + log);
                textBoxLog.AppendText("\r\n");

                return;
            }

            if (isErrorLog == true)
            {
                textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][错误] " + log);
                textBoxLog.AppendText("\r\n");

                return;
            }

            if (logFrom == "")
            {
                textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][消息] " + log);
                textBoxLog.AppendText("\r\n");

                return;
            }

            textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + logFrom + "] " + log);
            textBoxLog.AppendText("\r\n");
        }

        private void FormClientMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isConnectStart)
            {
                DisconConnect();
            }

            Dispose(true);
            Environment.Exit(0);
            Application.Exit();
            Application.ExitThread();
        }
    }
}
