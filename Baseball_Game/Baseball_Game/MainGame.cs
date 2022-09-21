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
    public partial class MainGame : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;

        string my_nickname = "";
        string my_client_num;
        string[] random_num = new string[4];
        int enter_count = 0;

        public MainGame(string nickname)
        {
            InitializeComponent();
            my_nickname = nickname;
            label1.Text = nickname;
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

        public void MainGame_Load(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            // 처음으로 발견되는 ipv4 주소를 사용한다.
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
                    byte[] bDts = Encoding.UTF8.GetBytes("rand" + '\x01' + my_client_num + '\x01');
                    mainSock.Send(bDts);
                }
                if (ip == "rand")//랜덤 미지의 4자리 숫자 받기
                {
                    random_num = tokens[1].Split(';');
                    //MessageBox.Show("본 게임은 미지의 4자리 숫자를 맞추는 게임입니다");
                    //MessageBox.Show("4자리 숫자는 자리마다 겹치는 숫자가 존재하지 않습니다.");
                    //MessageBox.Show("같은 자리 같은 숫자라면 Strike(S), 자리는 다르지만 미지의 4자리 숫자에 존재하는 숫자라면 Ball(B), S와 B이 모두 없으면 Out(O) 입니다");
                    //MessageBox.Show("게임의 순위는 미지의 숫자를 맞추기 위해 숫자 확인을 시도한 횟수가 적으면 높아집니다");
                    //MessageBox.Show("그럼 게임 시작!");
                }
                obj.ClearBuffer();
                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
            }
            catch
            {
                return;
            }
        }

        private void MainGame_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainSock.Close();
        }

        private void button1_Click(object sender, EventArgs e)//확인
        {
            int strike = 0;
            int ball = 0;
            string result = "";
            string rand = textBox1.Text + textBox2.Text + textBox3.Text + textBox4.Text;

            if (textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "" || textBox4.Text == "")
            {
                MessageBox.Show("4자리 모두 입력해 주세요(0~9)");
            }
            else
            {
                if (textBox1.Text == random_num[0])//각 자리 숫자 확인해서 Strike인지 BAll인지 확인하기
                    strike++;
                else if (textBox1.Text == random_num[1])
                    ball++;
                else if (textBox1.Text == random_num[2])
                    ball++;
                else if (textBox1.Text == random_num[3])
                    ball++;

                if (textBox2.Text == random_num[0])
                    ball++;
                else if (textBox2.Text == random_num[1])
                    strike++;
                else if (textBox2.Text == random_num[2])
                    ball++;
                else if (textBox2.Text == random_num[3])
                    ball++;

                if (textBox3.Text == random_num[0])
                    ball++;
                else if (textBox3.Text == random_num[1])
                    ball++;
                else if (textBox3.Text == random_num[2])
                    strike++;
                else if (textBox3.Text == random_num[3])
                    ball++;

                if (textBox4.Text == random_num[0])
                    ball++;
                else if (textBox4.Text == random_num[1])
                    ball++;
                else if (textBox4.Text == random_num[2])
                    ball++;
                else if (textBox4.Text == random_num[3])
                    strike++;

                if (strike == 0 && ball == 0)
                    result = "O";
                else if (strike == 0 && ball != 0)
                    result = ball.ToString() + "B";
                else if (strike != 0 && ball == 0)
                    result = strike.ToString() + "S";
                else
                    result = strike.ToString() + "S" + ball.ToString() + "B";

                enter_count++;
                ListViewItem lvt = new ListViewItem();
                lvt.Text = (enter_count.ToString());
                lvt.SubItems.Add(rand);
                lvt.SubItems.Add(result);
                listView1.Items.Add(lvt);

                if (result == "4S")
                {
                    byte[] bDts = Encoding.UTF8.GetBytes("clear" + '\x01' + enter_count.ToString() + '\x01' + my_nickname + '\x01');
                    if (enter_count < 10)
                        bDts = Encoding.UTF8.GetBytes("clear" + '\x01' + "0" + enter_count.ToString() + '\x01' + my_nickname + '\x01');
                    OnSendData(bDts, "clear");//클리어 했으면 클리어했다고 서버에 보내기
                    MessageBox.Show("축하합니다!");
                    new Result(my_nickname, rand, enter_count, this).Show();
                }
                textBox1.Clear();
                textBox2.Clear();
                textBox3.Clear();
                textBox4.Clear();
            }
        }

        public void Send(IAsyncResult result)
        {
            mainSock.EndSend(result);
        }

        public void OnSendData(byte[] bDts, string str)
        {
            mainSock.BeginSend(bDts, 0, bDts.Length, 0, new AsyncCallback(Send), mainSock);//여기
        }

        private void button2_Click(object sender, EventArgs e)//포기
        {
            string rand = random_num[0] + random_num[1] + random_num[2] + random_num[3];
            byte[] bDts = Encoding.UTF8.GetBytes("clear" + '\x01' + "-" + '\x01' + my_nickname + '\x01');
            OnSendData(bDts, "clear");//포기했다고 서버에 보내기
            MessageBox.Show("정답은 " + random_num[0] + random_num[1] + random_num[2] + random_num[3] + "입니다.");
            new Result(my_nickname, rand, 0, this).Show();
        }

        public void Replay()//Result에서 다시하기 버튼을 누르면 동작(초기화)
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            textBox4.Clear();
            listView1.Clear();
            listView1.Columns.Add("횟수", 40);
            listView1.Columns.Add("입력숫자", 90);
            listView1.Columns.Add("결과", 80);
            enter_count = 0;
            byte[] bDts = Encoding.UTF8.GetBytes("rand" + '\x01' + my_client_num + '\x01');
            OnSendData(bDts, "rand");
        }
    }
}
