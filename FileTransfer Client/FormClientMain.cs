﻿using System;
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
        // 服务器共享文件
        public String[] fileList = null;
        // 文件数量
        private int fileLength;
        // 文件夹仪表盘标识
        private int flagDirPathDisp;

        // SB多线程
        private string tmpFileRes = "";

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

                // 打学号和姓名
                Server.Send(Encoding.Unicode.GetBytes("#connect#userdata#" + stdNo + "#" + stdName));

                // 仪表盘状态
                label5.Text = ServerIP;
                label6.Text = ServerPort;
                label7.Text = "[联机]";
                label15.Text = stdNo;
                label16.Text = stdName;

                button2.Enabled = false;

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
            label5.Text = "[离线]";
            label6.Text = "[离线]";
            label7.Text = "[离线]";
            label8.Text = "[未启用]";
            label15.Text = "[离线]";
            label16.Text = "[离线]";

            listBoxServerFileList.Items.Clear();
            button2.Enabled = true;
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
            label5.Text = "[离线]";
            label6.Text = "[离线]";
            label7.Text = "[离线]";
            label8.Text = "[未启用]";
            label15.Text = "[离线]";
            label16.Text = "[离线]";

            listBoxServerFileList.Items.Clear();
            button2.Enabled = true;
        }

        /// <summary>
        /// 接收服务器信息
        /// </summary>
        public void ReceiveMessage()
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
                    listBoxServerFileList.Items.Clear();

                    for (int i = 3; i < fileLength - 1; i++)
                    {
                        string[] tmpDirPath = fileList[i].Split('\\');
                        int tmpDirLength = tmpDirPath.Length;

                        // 在仪表盘里显示目录
                        if(flagDirPathDisp == 0)
                        {
                            label8.Text = "";
                            for(int j = 0; j<tmpDirLength - 2; j++)
                            {
                                label8.Text += tmpDirPath[j] + '\\';
                            }
                            label8.Text += tmpDirPath[tmpDirLength - 2];
                            flagDirPathDisp = 1;
                        }

                        OutputLog("[读入文件] " + tmpDirPath[tmpDirLength - 1]);
                        listBoxServerFileList.Items.Add(tmpDirPath[tmpDirLength - 1]);
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
        public string ReceiveMessageStr()
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

        public void ReceiveFileThread(int index)
        {
            ReceiveFile(index);
        }

        public void ReceiveFile(int index)
        {
            string serverCommand;
            string[] fileInfo;
            string filePath;
            string fileName;
            int fileSpace;
            int pkgNum;

            OutputLog("[向服务器请求文件] " + listBoxServerFileList.Items[index]);
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

            // 现在该接收文件了！

            List<byte[]> TempFileByteList = new List<byte[]>();

            // 结束当前线程
            OutputLog("[接收文件完成]");
            Thread.CurrentThread.Suspend();
            Thread.CurrentThread.Abort();

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
        public void LoopCheckReceiveMsgStr(string checkStr)
        {
            // 循环检测接收到的消息是否包含指定字符串
            while (!Regex.IsMatch(ReceiveMsgStr, checkStr))
            {

            }
        }
    }
}
