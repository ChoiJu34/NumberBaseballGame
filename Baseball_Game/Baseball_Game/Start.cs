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
    public partial class Start : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;

        public string my_nickname = "";
        string my_client_num;

        public Start()
        {
            InitializeComponent();
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

        private void Start_Load(object sender, EventArgs e)
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

            try { mainSock.Connect(defaultHostAddress.ToString(), 9000); }//서버 연결
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

                if (ip == "client")//내 클라이언트 번호값을 받는다
                {//클라이언트 번호값을 받는 이유는 이걸 다시 서버에게 보내서 서버가 내 클라이언트를 찾아 내게만 값을 보내기 위해
                    my_client_num = tokens[1];
                    byte[] bDts = Encoding.UTF8.GetBytes("result" + '\x01' + my_client_num + '\x01');
                    mainSock.Send(bDts);
                }
                if (ip == "result")//서버에 저장된 listView1(닉네임, 최고기록)을 받는다
                {
                    int k = (tokens.Length - 2);
                    int j = 0;
                    int m = 0;
                    int l = 1;
                    //MessageBox.Show(k.ToString());
                    for (int i = 0; i < (tokens.Length - 2) / 2; i++)
                    {
                        if (tokens[k - 1] != "-")
                        {
                            ListViewItem lvt = new ListViewItem();
                            if (m != int.Parse(tokens[k - 1]))
                            {
                                lvt.Text = (l.ToString());
                                m = int.Parse(tokens[k - 1]);
                                l += 1;
                                j = l-1;
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
                        else
                        {
                            k -= 2;
                        }
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

        private void button1_Click(object sender, EventArgs e)//게임시작
        {
            if (textBox1.Text == "")
                MessageBox.Show("닉네임을 입력하세요!");
            else
            {
                my_nickname = textBox1.Text;
                DialogResult = DialogResult.OK;
                this.Close();//MainGame폼을 여는게 아니라 이 폼을 닫는 이유는 Program.cs파일 확인
            }
        }

        private void Start_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainSock.Close();
        }
    }
}
