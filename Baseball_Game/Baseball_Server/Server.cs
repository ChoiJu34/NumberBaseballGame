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

namespace Baseball_Server
{
    public partial class Server : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;
        IPAddress thisAddress;

        public Server()
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

        private void button1_Click(object sender, EventArgs e)//서버가동
        {
            try
            {
                IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

                // 처음으로 발견되는 ipv4 주소를 사용한다.
                foreach (IPAddress addr in he.AddressList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        thisAddress = addr;
                        break;
                    }
                }

                // 주소가 없다면..
                if (thisAddress == null)
                    // 로컬호스트 주소를 사용한다.
                    thisAddress = IPAddress.Loopback;

                //바인딩 되어있는지 확인
                if (mainSock.IsBound)
                {
                    MessageBox.Show("이미 서버에 연결되어 있습니다!");
                    return;
                }

                // 서버에서 클라이언트의 연결 요청을 대기하기 위해
                // 소켓을 열어둔다.

                IPEndPoint serverEP = new IPEndPoint(thisAddress, 9000);
                mainSock.Bind(serverEP);
                mainSock.Listen(10);

                // 비동기적으로 클라이언트의 연결 요청을 받는다.
                mainSock.BeginAccept(AcceptCallback, null);
                MessageBox.Show("서버가구동 되었습니다!");
                button1.Text = "서버 가동 중";
            }
            catch
            {
                MessageBox.Show("서버 시작시 오류가 발생하였습니다.");
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

        List<Socket> connectedClients = new List<Socket>();
        void AcceptCallback(IAsyncResult ar)
        {

            try
            {
                // 클라이언트의 연결 요청을 수락한다.
                Socket client = mainSock.EndAccept(ar);

                // 또 다른 클라이언트의 연결을 대기한다.
                mainSock.BeginAccept(AcceptCallback, null);

                AsyncObject obj = new AsyncObject(4096);
                obj.WorkingSocket = client;

                // 연결된 클라이언트 리스트에 추가해준다.
                connectedClients.Add(client);

                // 텍스트박스에 클라이언트가 연결되었다고 써준다.
                AppendText(listBox1, string.Format(client.RemoteEndPoint.ToString()));

                Socket socket = client;
                byte[] bDts = Encoding.UTF8.GetBytes("client" + '\x01' + client.RemoteEndPoint.ToString() + '\x01');
                socket.Send(bDts);

                // 클라이언트의 데이터를 받는다.
                client.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
            }
            catch
            {
                return;
            }
        }

        void DataReceived(IAsyncResult ar)
        {
            try
            {
                // BeginReceive에서 추가적으로 넘어온 데이터를 AsyncObject 형식으로 변환한다.
                AsyncObject obj = (AsyncObject)ar.AsyncState;

                // 데이터 수신을 끝낸다.
                int received = obj.WorkingSocket.EndReceive(ar);

                // 받은 데이터가 없으면(연결끊어짐) 끝낸다.
                if (received <= 0)
                {
                    obj.WorkingSocket.Close();
                    return;
                }

                // 텍스트로 변환한다.
                string text = Encoding.UTF8.GetString(obj.Buffer);

                string[] tokens = text.Split('\x01');
                string ip = tokens[0];
                string msg = tokens[1];
                listBox1.Items.Add(text);

                if (ip == "result")//listView1의 값을 클라이언ㅌ로 보내준다
                {
                    int k = 0;
                    for (k = connectedClients.Count - 1; k >= 0; k--)//값을 보내야할 클라이언트를 찾는다
                    {
                        if (connectedClients[k].RemoteEndPoint.ToString().Trim() == msg.Trim())
                        {
                            listBox1.Items.Add(connectedClients[k].RemoteEndPoint.ToString());
                            break;
                        }
                    }
                    int j = 0;
                    string str = "result" + '\x01';
                    foreach (ListViewItem item in listView1.Items)//listView1의 값을 보내기 위해 string에 저장
                    {
                        string[] itemtext = new string[100];
                        itemtext[j] = item.SubItems[0].Text;
                        j++;
                        itemtext[j] = item.SubItems[1].Text;
                        j++;
                        str += item.SubItems[0].Text + '\x01' + item.SubItems[1].Text + '\x01';
                    }
                    byte[] bDts = Encoding.UTF8.GetBytes(str);//string에 저장한 값을 소켓을 통해 보내주기 위해 데이터 변환
                    Socket socket = connectedClients[k];
                    socket.Send(bDts);
                }
                if (ip == "rand")//미지의 4자리 숫자를 클라이언트로 보낸다
                {
                    int k = 0;
                    for (k = connectedClients.Count - 1; k >= 0; k--)//값을 보내야할 클라이언트를 찾는다
                    {
                        if (connectedClients[k].RemoteEndPoint.ToString().Trim() == msg.Trim())
                        {
                            listBox1.Items.Add(connectedClients[k].RemoteEndPoint.ToString());
                            break;
                        }
                    }
                    Random rand = new Random();
                    string[] random_num = new string[4];
                    for(int i=0; i<4; i++)//4자리 숫자가 겹치지 않게 미지의 4자리 랜덤으로 정하기
                    {
                        random_num[i] = rand.Next(0, 10).ToString();
                        for (int r = i - 1; r >= 0; r--)
                            while (random_num[r] == random_num[i])
                                random_num[i] = rand.Next(0, 10).ToString();
                    }
                    listBox1.Items.Add(random_num[0] + random_num[1] + random_num[2] + random_num[3]);
                    byte[] bDts = Encoding.UTF8.GetBytes("rand" + '\x01' + random_num[0] + ";" + random_num[1] + ";" + random_num[2] + ";" + random_num[3] + '\x01');
                    Socket socket = connectedClients[k];
                    socket.Send(bDts);
                }
                else if (ip == "clear")//포기, 성공 관계 없이 게임이 종료됨
                {//listView1에 값을 저장하자
                    int overlap = 0;
                    foreach (ListViewItem item in listView1.Items)
                        if (item.SubItems[1].Text == tokens[2])//같은 닉네임이라면
                        {
                            item.SubItems[2].Text = (int.Parse(item.SubItems[2].Text) + 1).ToString();//게임 시도 횟수 1증가
                            overlap = 1;
                            if ((int.Parse(item.SubItems[0].Text) > int.Parse(tokens[1])) && (tokens[1] != "-"))//기존 최고기록 보다 좋은 기록이면 기존값 삭제하고 새로운 값 넣기
                            {
                                ListViewItem lvt = new ListViewItem();
                                lvt.Text = (tokens[1]);
                                lvt.SubItems.Add(item.SubItems[1].Text);
                                lvt.SubItems.Add(item.SubItems[2].Text);
                                listView1.Items.Remove(item);
                                listView1.Items.Add(lvt);
                            }
                            break;
                        }
                    if (overlap == 0)//닉네임이 겹치지 않는다면 listView1에 새로운 값 넣기
                    {
                        ListViewItem lvt = new ListViewItem();
                        lvt.Text = (tokens[1]);
                        lvt.SubItems.Add(tokens[2]);
                        lvt.SubItems.Add("1");
                        listView1.Items.Add(lvt);
                    }
                }
                else
                {
                    // for을 통해 "역순"으로 클라이언트에게 데이터를 보낸다.
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        Socket socket = connectedClients[i];
                        if (socket != obj.WorkingSocket)
                        {
                            try { socket.Send(obj.Buffer); }
                            catch
                            {
                                // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                                try { socket.Dispose(); } catch { }
                                connectedClients.RemoveAt(i);
                            }
                        }
                    }
                }

                // 데이터를 받은 후엔 다시 버퍼를 비워주고 같은 방법으로 수신을 대기한다.
                obj.ClearBuffer();

                // 수신 대기
                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
            }
            catch
            {
                return;
            }
        }

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainSock.Close();
        }
    }
}
