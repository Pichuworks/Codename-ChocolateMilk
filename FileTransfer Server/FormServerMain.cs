using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer_Server
{
    public partial class FormServerMain : Form
    {
        // Socket服务器实例对象
        static Socket Server;

        // 服务器端口
        static int startPort;

        // 客户端容器
        static List<Socket> ClientList = new List<Socket>();

        // 当前选中的客户端
        static Socket SelectedClient;

        static Socket TempListenClient;

        static int ListenCount;

        static string AddressIP;

        static string ReceiveMsgStr = "";

        static Byte[] ReceiveMsgByte;

        static string ServerDirPath;

        static String[] fileList = null;

        static string serverFileList = "#connect#file#";

        static string[] ClientInfo = null;

        static int fileCounter;

        static bool isServerStart;

        // SB多线程相关
        static string tmp_receivestream;
        static string tmp_filenext;

        public FormServerMain()
        {
            // Fuck the Thread!!!
            CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();
            label6.Text = "[离线]";
            label7.Text = "[离线]";
            label8.Text = "[离线]";
            label9.Text = "[未选择]";
            AcceptButton = btnBrowseDir;
        }

        /// <summary>
        /// 服务器上线/下线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void btnStartServer_Click(object sender, EventArgs e)
        {
            if (isServerStart)
            {
                CloseServer();
                return;
            }

            if(fileList == null)
            {
                MessageBox.Show("目录无法加载！");
                return;
            }

            if (Program.main.textBox1.Text != "")
            {
                startPort = int.Parse(Program.main.textBox1.Text);
                isServerStart = ServerInit(startPort);
                if (!isServerStart)
                {
                    return;
                }

                // 开辟线程 开始侦听连接
                new Thread(ListenConnect).Start();

                // 输出日志
                OutputLog("[开始侦听网络]");

            }
            else
            {
                MessageBox.Show("端口号为空！");
                return;
            }
        }

        /// <summary>
        /// 服务器上线逻辑
        /// </summary>
        /// <param name="port">指定端口</param>
        /// <returns>是否上线？</returns>
        static bool ServerInit(int port)
        {
            try
            {
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                AddressIP = GetAddressIP();
                Server.Bind(new IPEndPoint(IPAddress.Parse(AddressIP), port));
                Server.Listen(10);
                OutputLog("[服务器上线] IP: " + AddressIP + " Port:" + port + "");
                Program.main.Text = "[在线] 服务器仪表盘 - IP: " + AddressIP + " Port: " + port;
                Program.main.label6.Text = AddressIP;
                Program.main.label7.Text = port.ToString();
                Program.main.label8.Text = "[在线]";
                Program.main.btnStartServer.Text = "下线";
                Program.main.btnBrowseDir.Enabled = false;
                return true;
            }
            catch (Exception ex)
            {
                OutputLog("[服务器无法上线] " + ex.Message.ToString(), true);
                return false;
            }
        }

        /// <summary>
        /// 服务器下线逻辑
        /// </summary>
        static public void CloseServer()
        {
            if (!isServerStart)
            {
                return;
            }

            BroadcastMessage("#connect#close#");

            Server.Dispose();
            Server.Close();
            Server = null;
            ClientList = new List<Socket>();

            //UpdateListBoxData();
            //listboxClient.Items.Clear();

            Program.main.label6.Text = "[离线]";
            Program.main.label7.Text = "[离线]";
            Program.main.label8.Text = "[离线]";
            OutputLog("[服务器下线]");
            isServerStart = false;
            Program.main.Text = "[离线] 服务器仪表盘";
            Program.main.btnStartServer.Text = "上线";
            Program.main.btnBrowseDir.Enabled = true;
        }

        /// <summary>
        /// 侦听连接
        /// </summary>
        static void ListenConnect()
        {
            try
            {

                Socket client = Server.Accept();

                OutputLog("[客户端上线] IP: " + client.RemoteEndPoint.ToString());

                client.Send(Encoding.Unicode.GetBytes("#connect#ok#"));

                client.Send(Encoding.Unicode.GetBytes(serverFileList));

                ClientList.Add(client);

                TempListenClient = client;

                new Thread(ReceiveMessage).Start();

                UpdateListBoxData();

                new Thread(ListenConnect).Start();

                Thread.CurrentThread.Suspend();

                Thread.CurrentThread.Abort();

            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "SocketException")
                {
                    SocketException sEx = ex as SocketException;

                    if (sEx.ErrorCode == 10004)
                    {
                        return;
                    }
                }

                OutputLog(ex.Message.ToString(), true);
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        static void ReceiveMessage()
        {

            try
            {
                Socket tmp_client = TempListenClient;
                while (true)
                {
                    Socket CheckTmpClient = ClientList.Find(c => c.Handle == tmp_client.Handle);

                    if (CheckTmpClient == null)
                    {
                        return;
                    }

                    byte[] result = new byte[65535];

                    int receiveLength = tmp_client.Receive(result);

                    string receiveResult = Encoding.Unicode.GetString(result);

                    ReceiveMsgStr = receiveResult;
                    ReceiveMsgByte = result;

                    if (Regex.IsMatch(receiveResult, "#connect#discon#"))
                    {
                        OutputLog("[客户端下线]", logFrom: tmp_client.RemoteEndPoint.ToString());
                        ClientList.Remove(CheckTmpClient);
                        UpdateListBoxData();
                        return;
                    }

                    if (Regex.IsMatch(receiveResult, "#connect#userdata#"))
                    {
                        string[] stdData = receiveResult.Split('#');
                        int stdDataLength = stdData.Length;
                        OutputLog(tmp_client.RemoteEndPoint.ToString() + ": 学号: " + stdData[3] + " 姓名: " + stdData[4]);
                        UpdateListBoxData();
                    }

                    if (Regex.IsMatch(receiveResult, "#file#request#"))
                    {
                        Thread thread = new Thread(() => SendFileThread(receiveResult, tmp_client));

                        // 2018年5月19日，阴，我在上次没连数据库就操作之后，又出现了创建线程没打开的操作，这个要记下来。
                        thread.Start();
                    }

                    if (Regex.IsMatch(receiveResult, "#file#receivestream#"))
                    {
                        OutputLog("[ReceiveMessage 收到 #file#receivestream#] - " + ReceiveMsgStr);
                        tmp_receivestream = ReceiveMsgStr;
                    }

                    ReceiveMsgStr = receiveResult;
                    ReceiveMsgByte = result;
                    ClientInfo = receiveResult.Split('#');
                }
            }
            catch (Exception ex)
            {
                // 显示错误信息
                OutputLog(ex.Message.ToString(), true);
            }
        }

        static void SendFileThread(string receiveResult, Socket tmp_client)
        {
            SendFile(receiveResult, tmp_client);
        }

        static void SendFile(string receiveResult, Socket tmp_client)
        {
            string[] fileRequest = receiveResult.Split('#');
            int frLength = fileRequest.Length;
            OutputLog("---收到文件请求---");
            OutputLog("来自: " + tmp_client.RemoteEndPoint.ToString());
            OutputLog("学号: " + fileRequest[3] + " 姓名: " + fileRequest[4]);
            OutputLog("文件: " + fileRequest[5]);

            // 获得文件大小
            long lSize = 0;
            lSize = new FileInfo(fileRequest[5]).Length;
            OutputLog("[文件大小] " + lSize.ToString());

            string fileId = fileRequest[3] + "_" + fileRequest[4];

            // 计算需要分多少片
            long pkgNum = lSize / 256;
            if (lSize % 256 != 0)
            {
                pkgNum++;
            }

            // 打出发送前数据
            tmp_client.Send(Encoding.Unicode.GetBytes("#filedata#receive#" + fileRequest[5] + "#" + lSize.ToString() + "#" + pkgNum.ToString() + "#"));

            // 分片打出
            OutputLog("[开始发送数据]");

            //

            byte[] buffer = new byte[9000];
            byte[] fileEnd = Encoding.Unicode.GetBytes("#file#end#" + fileId + "#");
            OutputLog("Nya~0");
            FileStream fileStream = new FileInfo(fileRequest[5]).OpenRead();
            OutputLog("Nya~1");
            long length = fileStream.Length;
            OutputLog("Nya~2");

            byte[] fileByte;

            int SendLength = 0;

            while ((SendLength = fileStream.Read(buffer, 0, length <= 8192 ? (int)length : 8192)) != 0)
            {
                OutputLog("Nya~3");

                fileByte = new byte[SendLength];

                Array.Copy(buffer, fileByte, SendLength);

                byte[] fileHead = Encoding.Unicode.GetBytes("#file#stream#" + fileId + "#" + SendLength + "#");

                OutputLog("Nya~4");

                tmp_client.Send(fileHead);

                OutputLog("Nya~5");

           //     LoopCheckReceiveMsgStr("#file#receivestream#");

             //   if (Regex.IsMatch(ReceiveMsgStr, "#file#receivestream#"))
             //   {
                    tmp_client.Send(buffer);
             //   }



                // progressBar.Value += SendLength;

                buffer = new byte[9000];

               // LoopCheckReceiveMsgStr("#file#next#");

               // if (Regex.IsMatch(ReceiveMsgStr, "#file#next#"))
               // {
                    continue;
               // }


            }

            SendLength = 0;


            tmp_client.Send(fileEnd);

            OutputLog("[发送完成]");

            fileStream.Close();
            //

            OutputLog("[服务器响应操作] " + "#filedata#receive#" + fileRequest[5]);
            OutputLog("------------------");
        }

        static void LoopCheckReceiveMsgStr(string checkStr)
        {
            // 循环检测接收到的消息是否包含指定字符串
            while (!Regex.IsMatch(ReceiveMsgStr, checkStr))
            {
                OutputLog("LoopCheckReceiveMsgStr: " + checkStr);
            }
        }

        /// <summary>
        /// 更新列表
        /// </summary>
        static void UpdateListBoxData()
        {
            List<Socket> tempList = new List<Socket>();

            tempList.AddRange(ClientList);

            Program.main.listBoxClientList.DataSource = tempList;
            Program.main.listBoxClientList.DisplayMember = "RemoteEndPoint";
            Program.main.listBoxClientList.ValueMember = "Handle";
        }

        /// <summary>
        /// 向客户端广播报文
        /// </summary>
        /// <param name="Message">报文信息</param>
        static void BroadcastMessage(string Message)
        {
            try
            {
                foreach (Socket client in ClientList)
                {
                    client.Send(Encoding.Unicode.GetBytes(Message));
                }
            }
            catch (Exception ex)
            {
                OutputLog(ex.Message.ToString(), true);
            }
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns></returns>
        static string GetAddressIP()
        {
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }

            return AddressIP;
        }

        /// <summary>
        /// 打日志
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="isErrorLog">是否是错误日志</param>
        /// <param name="logFrom">日志来源</param>
        static void OutputLog(string log, bool isErrorLog = false, string logFrom = "")
        {

            if (isErrorLog == true)
            {
                Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][错误] " + log);
                Program.main.textBoxLog.AppendText("\r\n");
            }
            else if (logFrom == "")
            {
                Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][消息] " + log);
                Program.main.textBoxLog.AppendText("\r\n");
                
            }
            else
            {
                Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + logFrom + "] " + log);
                Program.main.textBoxLog.AppendText("\r\n");
            }
            return;

        }

        /// <summary>
        /// 让端口只接受数字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0x20) e.KeyChar = (char)0;  // 禁止空格键  
            if ((e.KeyChar == 0x2D) && (((TextBox)sender).Text.Length == 0)) return;   // 处理负数  
            if (e.KeyChar > 0x20)
            {
                try
                {
                    double.Parse(((TextBox)sender).Text + e.KeyChar.ToString());
                }
                catch
                {
                    e.KeyChar = (char)0;   // 处理非法字符  
                }
            }
        }

        /// <summary>
        /// 浏览目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择共享目录：";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "共享目录不能为空！", "提示");
                    return;
                }
                ServerDirPath = dialog.SelectedPath;
                label9.Text = ServerDirPath;

                fileList = Directory.GetFiles(ServerDirPath, "*", SearchOption.AllDirectories);
                fileCounter = fileList.Length;

                serverFileList = "#connect#file#";
                for (int i = 0; i < fileCounter; i++)
                {
                    OutputLog("[读入文件] " + fileList[i]);
                    serverFileList = serverFileList + fileList[i] + "#"; 
                    // 即使是最后一行都要加#！要不然等着字符串末尾0的恐惧吧！！！！！！！！CNM C#！！！！
                }

                // Debug
                // OutputLog("[读入目录] " + serverFileList);

                AcceptButton = btnStartServer;
            }
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            textBoxLog.Text = "";
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormServerMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseServer();
            Dispose(true);
            Environment.Exit(0);
            Application.Exit();
            Application.ExitThread();
        }

        private void listBoxClientList_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("计算机网络课程设计\n\r代号巧克力牛奶\n\r服务器端于 May 20th, '18 编译\n\rBy PichuTheNeko");
        }
    }
}
