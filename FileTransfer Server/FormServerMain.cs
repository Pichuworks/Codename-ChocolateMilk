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
        public Socket Server;

        // 服务器端口
        int startPort;

        // 客户端容器
        List<Socket> ClientList = new List<Socket>();

        // 当前选中的客户端
        public Socket SelectedClient;

        public Socket TempListenClient;

        public int ListenCount;

        public string AddressIP;

        public string ReceiveMsgStr = "";

        public Byte[] ReceiveMsgByte;

        public string ServerDirPath;

        public String[] fileList = null;

        public string serverFileList = "#connect#file#";

        public string[] ClientInfo = null;

        int fileCounter;

        bool isServerStart;

        public FormServerMain()
        {
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
        private void btnStartServer_Click(object sender, EventArgs e)
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

            if (textBox1.Text != "")
            {
                startPort = int.Parse(textBox1.Text);
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
        bool ServerInit(int port)
        {
            try
            {
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                AddressIP = GetAddressIP();
                Server.Bind(new IPEndPoint(IPAddress.Parse(AddressIP), port));
                Server.Listen(10);
                OutputLog("[服务器上线] IP: " + AddressIP + " Port:" + port + "");
                Text = "[在线] 服务器仪表盘 - IP: " + AddressIP + " Port: " + port;
                label6.Text = AddressIP;
                label7.Text = port.ToString();
                label8.Text = "[在线]";
                btnStartServer.Text = "下线";
                btnBrowseDir.Enabled = false;
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
        public void CloseServer()
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

            label6.Text = "[离线]";
            label7.Text = "[离线]";
            label8.Text = "[离线]";
            OutputLog("[服务器下线]");
            isServerStart = false;
            Text = "[离线] 服务器仪表盘";
            btnStartServer.Text = "上线";
            btnBrowseDir.Enabled = true;
        }

        /// <summary>
        /// 侦听连接
        /// </summary>
        public void ListenConnect()
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
        public void ReceiveMessage()
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
                        string[] fileRequest = receiveResult.Split('#');
                        int frLength = fileRequest.Length;
                        OutputLog("---收到文件请求---");
                        OutputLog("来自: " + tmp_client.RemoteEndPoint.ToString());
                        OutputLog("学号: " + fileRequest[3] + " 姓名: " + fileRequest[4]);
                        OutputLog("文件: " + fileRequest[5]);
                        OutputLog("------------------");
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

        /// <summary>
        /// 更新列表
        /// </summary>
        public void UpdateListBoxData()
        {
            List<Socket> tempList = new List<Socket>();

            tempList.AddRange(ClientList);

            listBoxClientList.DataSource = tempList;
            listBoxClientList.DisplayMember = "RemoteEndPoint";
            listBoxClientList.ValueMember = "Handle";
        }

        /// <summary>
        /// 向客户端广播报文
        /// </summary>
        /// <param name="Message">报文信息</param>
        public void BroadcastMessage(string Message)
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
        string GetAddressIP()
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
        public void OutputLog(string log, bool isErrorLog = false, string logFrom = "")
        {

            if (isErrorLog == true)
            {
                textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][错误] " + log);
                textBoxLog.AppendText("\r\n");
            }
            else if (logFrom == "")
            {
                textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][消息] " + log);
                textBoxLog.AppendText("\r\n");
                
            }
            else
            {
                textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + logFrom + "] " + log);
                textBoxLog.AppendText("\r\n");
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
                for (int i = 0; i < fileCounter - 1; i++)
                {
                    OutputLog("[读入文件] " + fileList[i]);
                    serverFileList = serverFileList + fileList[i] + "#"; 
                }
                serverFileList = serverFileList + fileList[fileCounter - 1];

                OutputLog("[读入文件] " + fileList[fileCounter - 1]);

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
    }
}
