using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        // 服务器是否处于启动状态
        bool isServerStart;

        string[] ClientInfo;

        public FormServerMain()
        {
            InitializeComponent();
            label6.Text = "[离线]";
            label7.Text = "[离线]";
            label8.Text = "[离线]";
            label9.Text = "[未选择]";
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (isServerStart)
            {
                CloseServer();
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
            }
            else
            {
                MessageBox.Show("端口号为空！");
                return;
            }
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="port">指定端口</param>
        /// <returns>启动结果</returns>
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
                return true;
            }
            catch (Exception ex)
            {
                OutputLog("[服务器无法上线] " + ex.Message.ToString(), true);
                return false;
            }
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public void CloseServer()
        {
            if (!isServerStart)
            {
                return;
            }

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
    }
}
