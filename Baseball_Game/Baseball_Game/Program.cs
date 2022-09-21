using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Baseball_Game
{
    static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Start start = new Start();
            if (start.ShowDialog() == DialogResult.OK)//시작 폼 제대로 닫치면
            {
                Application.Run(new MainGame(start.my_nickname));//메임 폼 열기
            }
        }
    }
}
