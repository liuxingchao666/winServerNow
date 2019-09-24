using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsService;
using System.Windows;
using System.Windows.Forms;
using System.IO;

namespace ConsoleApp1
{
    class Program
    {
      static  scoketClicent scoketClicent;
        [STAThread]
        static void Main(string[] args)
        {
             scoketClicent = new scoketClicent();
            a();
            Console.ReadKey();
        }
        [STAThread]
        public static bool a()
        {
            string str = Console.ReadLine();
            if (str.Equals("disconnect"))
            {
                scoketClicent.close();
                a();
            }
            else if (str.Equals("reset"))
            {
                scoketClicent = new scoketClicent();
                a();
            }
            else if (str.Equals("upload"))
            {
                scoketClicent.upload();
                a();
            }
            else if (str.Equals("download"))
            {
                Console.WriteLine("请输入您要下载的文件名称：");
                string fileName = Console.ReadLine();
                scoketClicent.download(fileName);
                a();
            } else
            {
                scoketClicent.sendMessage(str);
                a();
            }
            return false;
        }
    }
    
    public class scoketClicent
    {
        static Socket scoket;
        
        static Thread thread;
        public Stack<string> list = new Stack<string>();//返回信息堆
        string fileName;
        /// <summary>
        /// 建立同讯
        /// </summary>
      
        public scoketClicent()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(ip, 8081);
            scoket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            scoket.Connect(ipe);
            thread = new Thread(new ThreadStart(() => {
                while (true)
                {
                    try
                    {
                        byte[] arrRecvmsg = new byte[1024 * 1024];
                        int length = scoket.Receive(arrRecvmsg);
                        if (arrRecvmsg[0] == 2)
                        {
                            Console.WriteLine("请选择您的文件保存路径");
                            string path = getPath();
                            ////测试
                            if (!string.IsNullOrEmpty(path))
                            {
                                Console.WriteLine("正在下载,请稍等....");
                                path += "/" + list.Pop();
                               
                                using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
                                {
                                    stream.Write(arrRecvmsg, 1, length - 1);
                                }
                                Console.WriteLine("下载完成,保存路径:"+path);
                            }
                            else
                            {
                                list.Pop();
                            }
                        }
                        else
                        {
                            string strRevMsg = Encoding.UTF8.GetString(arrRecvmsg, 0, length);
                            if (string.IsNullOrEmpty(strRevMsg))
                            {
                                Console.WriteLine("服务器主动断开连接");
                                scoket.Disconnect(true);
                                scoket.Close();
                                scoket.Dispose();
                                thread.Abort();
                                break;
                            }
                            if (strRevMsg.Contains("文件名"))
                            {
                                list.Push(strRevMsg.Split(':')[1]);
                            }
                            else
                            {
                                Console.WriteLine(strRevMsg);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        thread.Abort();
                        break;
                    }
                }
            }));
            thread.IsBackground = true;
            thread.TrySetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="message"></param>
        public void sendMessage(string message)
        {
            try
            {
                byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(message);
                scoket.Send(arrClientSendMsg);
            }
            catch { }
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void close()
        {
            try
            {
                scoket.Send(Encoding.UTF8.GetBytes("disconnect"));
                scoket.Disconnect(true);
                scoket.Close();
                thread.Abort();
                scoket.Dispose();
            }
            catch { }
        }
        /// <summary>
        /// 文件上传
        /// </summary>
        public void upload()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                List<string> paths = openFileDialog.FileNames.ToList();
                List<string> fileNames = openFileDialog.SafeFileNames.ToList();
                int i = 0;
                foreach (string path in paths)
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        scoket.Send(Encoding.UTF8.GetBytes("fileName:"+fileNames[i]));
                        byte[] arrFile = new byte[1024 * 1024 * 2];
                        int length = stream.Read(arrFile, 0, arrFile.Length);
                        byte[] arrFileSend = new byte[length + 1];
                        arrFileSend[0] = 1;
                        Buffer.BlockCopy(arrFile, 0, arrFileSend, 1, length);
                        scoket.Send(arrFileSend);
                    }
                    i++;
                }
            }
        }
        /// <summary>
        /// 文件下载（模糊查询）
        /// </summary>
        /// <param name="fileName"></param>
        public void download(string fileName)
        {
            this.fileName = fileName;
            scoket.Send(Encoding.UTF8.GetBytes("下载文件:"+fileName));
        }
        [STAThread]
        public string getPath()
        {
            FolderBrowserDialog saveFileDialog = new FolderBrowserDialog();
            saveFileDialog.Description = "请选择您的文件保存路径";
            saveFileDialog.ShowDialog();
            return saveFileDialog.SelectedPath;
        }
    }
}
