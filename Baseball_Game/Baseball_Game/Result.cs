using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Baseball_Game
{
    public partial class Result : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;

        string rand_num;
        string my_nickname;
        int enter_count;
        string my_client_num;
        MainGame maingame;

        public Result(string nick, string num, int cnt, MainGame game)
        {
            InitializeComponent();
            rand_num = num;
            my_nickname = nick;
            enter_count = cnt;
            label1.Text = num;
            maingame = game;
            if (cnt != 0)
                label2.Text = cnt.ToString();
            else
                label2.Text = "-";
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _textAppender = new AppendTextDelegate(AppendText);
        }

        void AppendText(Control ctrl, string s)
        {
            if (ctrl.InvokeRequired) ctrl.Invoke(_textAppender, ctrl, s);
            else
            {
                string source = ctrl.Text;
                ctrl.Text = source + Environment.NewLine + s;
            }
        }

        public class AsyncObject
        {
            public byte[] Buffer;
            public Socket WorkingSocket;
            public readonly int BufferSize;
            public AsyncObject(int bufferSize)
            {
                BufferSize = bufferSize;
                Buffer = new byte[BufferSize];
            }

            public void ClearBuffer()
            {
                Array.Clear(Buffer, 0, BufferSize);
            }
        }

        private void Result_Load(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }

            if (defaultHostAddress == null)
                defaultHostAddress = IPAddress.Loopback;

            try { mainSock.Connect(defaultHostAddress.ToString(), 9000); }
            catch
            {
                MessageBox.Show("연결에 실패했습니다!");
                return;
            }
            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = mainSock;
            mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }

        void DataReceived(IAsyncResult ar)
        {
            try
            {
                AsyncObject obj = (AsyncObject)ar.AsyncState;

                int received = obj.WorkingSocket.EndReceive(ar);

                if (received <= 0)
                {
                    obj.WorkingSocket.Close();
                    return;
                }

                string text = Encoding.UTF8.GetString(obj.Buffer);

                string[] tokens = text.Split('\x01');
                string ip = tokens[0];
                string msg = tokens[1];

                if (ip == "client")
                {
                    my_client_num = tokens[1];
                    byte[] bDts = Encoding.UTF8.GetBytes("result" + '\x01' + my_client_num + '\x01');
                    mainSock.Send(bDts);
                }
                if (ip == "result")
                {
                    int k = (tokens.Length - 2);
                    int j = 0;
                    int m = 0;
                    for (int i = 0; i < (tokens.Length - 2) / 2; i++)
                    {
                        ListViewItem lvt = new ListViewItem();
                        if ((m != int.Parse(tokens[k - 1])) && (tokens[k - 1] != "-"))
                        {
                            lvt.Text = ((i + 1).ToString());
                            m = int.Parse(tokens[k - 1]);
                            j = i + 1;
                        }
                        else
                        {
                            lvt.Text = (j.ToString());
                        }
                        lvt.SubItems.Add(tokens[k]);
                        k--;
                        lvt.SubItems.Add(tokens[k]);
                        k--;
                        listView1.Items.Add(lvt);
                    }
                }

                obj.ClearBuffer();
                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
            }
            catch
            {
                return;
            }
        }

        private void Result_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainSock.Close();
        }

        private void button1_Click(object sender, EventArgs e)//다시하기
        {
            this.Close();
            maingame.Replay();
        }

        private void button2_Click(object sender, EventArgs e)//종료
        {
            this.Close();
            maingame.Close();
        }
    }
}
