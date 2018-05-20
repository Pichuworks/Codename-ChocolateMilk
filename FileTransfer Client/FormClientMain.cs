using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        static Socket Server;
        // 服务器IP
        static string ServerIP;
        // 服务器端口
        static string ServerPort;
        // 是否处于连接状态
        static bool isConnectStart;
        // 接收到的消息字符串
        static string ReceiveMsgStr = "";
        // 学号
        static string stdNo = "";
        // 姓名
        static string stdName = "";
        // 服务器共享文件
        static String[] fileList = null;
        // 文件数量
        static int fileLength;
        // 文件夹仪表盘标识
        static int flagDirPathDisp;

        // SB多线程
        static string tmpFileRes = "";

        public FormClientMain()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            label5.Text = "[离线]";
            label6.Text = "[离线]";
            label7.Text = "[离线]";

            label8.Text = "[未启用]";

            label15.Text = "[未启用]";
            label16.Text = "[未启用]";

            // Debug
            stdNo = "PichuTheNeko";
            stdName = "橘猫昊昊";
            label15.Text = "[离线]";
            label16.Text = "[离线]";
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

            if(stdNo == "" || stdName == "")
            {
                MessageBox.Show("学号/姓名为空！");
                return;
            }

            new Thread(ConnectServerThread).Start();

            OutputLog("[客户端正在上线]");
        }

        /// <summary>
        /// 服务器连接线程
        /// </summary>
        static void ConnectServerThread()
        {
            ServerIP = Program.main.textBox1.Text;

            ServerPort = Program.main.textBox2.Text;

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
        static bool ConnectServer(string serverIP, string serverPort)
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
        static bool CheckConnectResult()
        {
            // 接收消息字符串
            string receiveResult = ReceiveMessageStr();

            // 判断接收到的消息是否包含 #connect#ok# 是则表示连接成功
            if (Regex.IsMatch(receiveResult, "#connect#ok#"))
            {
                // 输出日志
                OutputLog("[客户端上线] 服务器IP: " + ServerIP + " Port: " + ServerPort);

                // 设置按钮文本及窗口标题
                Program.main.button1.Text = "断开";
                Program.main.Text = "[在线] 客户端仪表盘 - 服务器IP: " + ServerIP + " Port: " + ServerPort;

                // 打学号和姓名
                Server.Send(Encoding.Unicode.GetBytes("#connect#userdata#" + stdNo + "#" + stdName));

                // 仪表盘状态
                Program.main.label5.Text = ServerIP;
                Program.main.label6.Text = ServerPort;
                Program.main.label7.Text = "[联机]";
                Program.main.label15.Text = stdNo;
                Program.main.label16.Text = stdName;

                Program.main.button2.Enabled = false;

                // 设置连接状态
                isConnectStart = true;

                return true;
            }
            else // 否则连接失败
            {
                OutputLog("[客户端无法上线] 服务器IP: " + ServerIP + " Port: " + ServerPort);
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        static void DisconConnect()
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
            Program.main.button1.Text = "连接";
            Program.main.Text = "[离线] 客户端仪表盘";
            Program.main.label5.Text = "[离线]";
            Program.main.label6.Text = "[离线]";
            Program.main.label7.Text = "[离线]";
            Program.main.label8.Text = "[未启用]";
            Program.main.label15.Text = "[离线]";
            Program.main.label16.Text = "[离线]";

            Program.main.listBoxServerFileList.Items.Clear();
            Program.main.button2.Enabled = true;
        }

        /// <summary>
        /// 释放服务器连接资源
        /// </summary>
        static void DisconServerSocket()
        {
            // 重置服务器对象
            Server = null;
            // 重置连接状态
            isConnectStart = false;

            OutputLog("[客户端下线]");
            Program.main.button1.Text = "连接";
            Program.main.Text = "[离线] 客户端仪表盘";
            Program.main.label5.Text = "[离线]";
            Program.main.label6.Text = "[离线]";
            Program.main.label7.Text = "[离线]";
            Program.main.label8.Text = "[未启用]";
            Program.main.label15.Text = "[离线]";
            Program.main.label16.Text = "[离线]";

            Program.main.listBoxServerFileList.Items.Clear();
            Program.main.button2.Enabled = true;
        }

        /// <summary>
        /// 接收服务器信息
        /// </summary>
        static void ReceiveMessage()
        {
            try
            {
                // 定义Byte数组
                byte[] result = new byte[65535];

                // 接收
                Server.Receive(result);

                // 转换成字符串
                string receiveResult = Encoding.Unicode.GetString(result);

                // 在这里也要保存消息字符串 用于异步访问
                ReceiveMsgStr = receiveResult;

                // 消息 - 连接关闭
                if (Regex.IsMatch(receiveResult, "#connect#close#"))
                {
                    OutputLog("[服务器下线]");
                    DisconConnect();

                    return;
                }

                // 消息 - 目录文件
                if (Regex.IsMatch(receiveResult, "#connect#file#"))
                {
                    // 标签初始化
                    flagDirPathDisp = 0;

                    // 分隔字符串
                    fileList = receiveResult.Split('#');
                    fileLength = fileList.Length;

                    // 清空原列表
                    Program.main.listBoxServerFileList.Items.Clear();

                    for (int i = 3; i < fileLength - 1; i++)
                    {
                        string[] tmpDirPath = fileList[i].Split('\\');
                        int tmpDirLength = tmpDirPath.Length;

                        // 在仪表盘里显示目录
                        if(flagDirPathDisp == 0)
                        {
                            Program.main.label8.Text = "";
                            for(int j = 0; j<tmpDirLength - 2; j++)
                            {
                                Program.main.label8.Text += tmpDirPath[j] + '\\';
                            }
                            Program.main.label8.Text += tmpDirPath[tmpDirLength - 2];
                            flagDirPathDisp = 1;
                        }

                        OutputLog("[读入文件] " + tmpDirPath[tmpDirLength - 1]);
                        Program.main.listBoxServerFileList.Items.Add(tmpDirPath[tmpDirLength - 1]);
                    }
                }

                
                if (Regex.IsMatch(receiveResult, "#filedata#receive#"))
                {
                    OutputLog("[ReceiveMessage 收到服务器响应] - " + ReceiveMsgStr);
                    tmpFileRes = ReceiveMsgStr;
                }
                

                // 保存消息字符串 用于异步访问
                // ReceiveMsgStr = receiveResult;

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
        /// /*
        static string ReceiveMessageStr()
        {
            try
            {
                // 定义字节数组
                byte[] result = new byte[65535];

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

        static string ReceiveMessageStr(Socket client)
        {
            OutputLog("ReceiveMessageStr~");

            byte[] result = new byte[9000];

                int receiveLength = client.Receive(result);

                string receiveResult = Encoding.Unicode.GetString(result);

                OutputLog("ReceiveMessageStr" + receiveResult);

                return receiveResult;
        }

        /// <summary>
        /// 打日志
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="isErrorLog">是否是错误日志</param>
        /// <param name="logFrom">日志来源</param>
        static void OutputLog(string log, bool isFromServer = false, bool isErrorLog = false, string logFrom = "")
        {
            if (isFromServer == true)
            {
                Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][Server] " + log);
                Program.main.textBoxLog.AppendText("\r\n");

                return;
            }

            if (isErrorLog == true)
            {
                Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][错误] " + log);
                Program.main.textBoxLog.AppendText("\r\n");

                return;
            }

            if (logFrom == "")
            {
                Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][消息] " + log);
                Program.main.textBoxLog.AppendText("\r\n");

                return;
            }

            Program.main.textBoxLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + logFrom + "] " + log);
            Program.main.textBoxLog.AppendText("\r\n");
        }

        static void FormClientMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isConnectStart)
            {
                DisconConnect();
            }

            Program.main.Dispose(true);
            Environment.Exit(0);
            Application.Exit();
            Application.ExitThread();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBoxLog.Text = "";
        }

        private void listBoxServerFileList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBoxServerFileList.SelectedIndex;
            if (index != System.Windows.Forms.ListBox.NoMatches)
            {
                Thread thread = new Thread(() => ReceiveFileThread(index));
                thread.Start();
            }
        }

        static void ReceiveFileThread(int index)
        {
            ReceiveFile(index);
        }

        static void ReceiveFile(int index)
        {
            string serverCommand;
            string[] fileInfo;
            string filePath;
            string fileName;
            int fileSpace;
            int pkgNum;

            OutputLog("[向服务器请求文件] " + Program.main.listBoxServerFileList.Items[index]);
            // 末尾不加#就等着报错吧，字符串不自己断\0的恐惧
            Server.Send(Encoding.Unicode.GetBytes("#file#request#" + stdNo + "#" + stdName + " #" + fileList[index + 3] + "#"));
            OutputLog("[等待服务器响应]");

            while (tmpFileRes == "") ;
            serverCommand = tmpFileRes;
            OutputLog("[ReceiveFile 收到服务器响应] - " + serverCommand);
            tmpFileRes = "";

            fileInfo = serverCommand.Split('#');

            filePath = fileInfo[3];

            string[] tmpDirPath = filePath.Split('\\');
            int tmpDirLength = tmpDirPath.Length;

            fileName = tmpDirPath[tmpDirLength - 1];
            fileSpace = Convert.ToInt32(fileInfo[4]);
            pkgNum = Convert.ToInt32(fileInfo[5]);

            OutputLog("[解析] 文件名: " + fileName + " 字节数: " + fileSpace.ToString() + " 分片数: " + pkgNum.ToString());
            string savePath = "D:\\fuckyouSocket\\" + stdNo + "_" + stdName + "_" + fileName;

            // 现在该接收文件了！
            List<byte[]> TempFileByteList = new List<byte[]>();
            OutputLog("Nya~0");
            while (true)
            {
                OutputLog("Nya~1");
                byte[] result = new byte[9000];
                OutputLog("Nya~2");
                string receiveMsg;
                // = ReceiveMessageStr();
                OutputLog("Nya~3");
                // OutputLog("receiveMsg = " + receiveMsg);

                // if (Regex.IsMatch(receiveMsg, "#file#stream#"))
                // {

                    Server.Send(Encoding.Unicode.GetBytes("#file#receivestream#"));

                    byte[] resultFileByte = ReceiveMessageByte(Server);

                //string[] fileinfo = receiveMsg.Split('#');

                //int fileByteLength = int.Parse(fileinfo[4]);

                    int fileByteLength = 256;


                    byte[] temp = resultFileByte;

                    byte[] fileBuffer = new byte[fileByteLength];

                    Array.Copy(temp, fileBuffer, fileByteLength);

                    TempFileByteList.Add(fileBuffer);


                    Server.Send(Encoding.Unicode.GetBytes("#file#next#"));
               // }

                //if (Regex.IsMatch(receiveMsg, "#file#end#"))
                //{

                    OutputLog("[正在写入文件]");

                // string[] fileinfo = receiveMsg.Split('#');

                /* FileStream fileStream = File.Create(savePath);

                 TempFileByteList.ForEach(f =>
                 {
                     byte[] b = f;
                     fileStream.Write(b, 0, b.Length);
                 });
                fileStream.Close();
                */

                // 合并文件流
                int eachReadLength = 256;
                int loadPkg = 0;
                int toLaunched = 0;

                FileStream src = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                FileStream dist = new FileStream(savePath, FileMode.Create, FileAccess.Write);

                
                if (eachReadLength < src.Length)
                {

                    byte[] buffer = new byte[eachReadLength];
                    long launched = 0;

                    while (launched <= src.Length - eachReadLength)
                    {
                        loadPkg++;
                        OutputLog("正在合并第 " + loadPkg + "/" + pkgNum + " 组数据");

                        toLaunched = src.Read(buffer, 0, eachReadLength);

                        src.Flush();
                        dist.Write(buffer, 0, eachReadLength);
                        dist.Flush();

                        /*
                         f = new byte[256]; 
                         byte[] b = f;
                         fileStream.Write(b,toLaunched, b.Length);
                         */

                        dist.Position = src.Position;
                        launched += toLaunched;  
                    }
                    int left = (int)(src.Length - launched);
                    toLaunched = src.Read(buffer, 0, left);

                    src.Flush();
                    dist.Write(buffer, 0, left);

                    dist.Flush();

                }
                else
                {
                    loadPkg++;
                    OutputLog("正在合并第 " + loadPkg + "/" + pkgNum + " 组数据");
                    byte[] buffer = new byte[src.Length];

                    src.Read(buffer, 0, buffer.Length);
                    src.Flush();

                    dist.Write(buffer, 0, buffer.Length);
                    dist.Flush();
                }
                OutputLog("[合并文件完成]");    // 合并文件
                src.Close();
                dist.Close();

              
                
               // }

                // 结束当前线程
                OutputLog("[接收文件完成]");
                Thread.CurrentThread.Suspend();
                Thread.CurrentThread.Abort();
                return;
            }
        }

        static byte[] ReceiveMessageByte(Socket client)
        {
            try
            {
                byte[] result = new byte[9000];

                int receiveLength = client.Receive(result);

                return result;
            }
            catch (Exception ex)
            {
                // 显示错误信息

                OutputLog(ex.Message.ToString(), true);

                return new byte[0];
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox3.Text != "" && textBox4.Text != "")
            {
                stdNo = textBox3.Text;
                stdName = textBox4.Text;
                label15.Text = "[离线]";
                label16.Text = "[离线]";
            }
            else
            {
                MessageBox.Show("学号/姓名为空！");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int index = listBoxServerFileList.SelectedIndex;
            if (index != System.Windows.Forms.ListBox.NoMatches)
            {
                // MessageBox.Show(fileList[index + 3]);
                OutputLog("[向服务器请求文件]" + fileList[index + 3]);
                Server.Send(Encoding.Unicode.GetBytes("#file#request#" + stdNo + "#" + stdName +" #" + fileList[index + 3]));
            }
        }

        /// <summary>
        /// 循环检查返回的消息内容是否包含内容 - 阻塞 用于异步线程等待响应消息
        /// </summary>
        /// <param name="checkStr"></param>
        static void LoopCheckReceiveMsgStr(string checkStr)
        {
            // 循环检测接收到的消息是否包含指定字符串
            while (!Regex.IsMatch(ReceiveMsgStr, checkStr))
            {

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("计算机网络课程设计\n\r代号巧克力牛奶\n\r客户端于 May 20th, '18 编译\n\rBy PichuTheNeko");
        }
    }
}
