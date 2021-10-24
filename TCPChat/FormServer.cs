using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TCPChat
{
    public partial class FormServer : Form
    {
        Server server;

        public FormServer()
        {
            InitializeComponent();
            server = new Server(this);
        }

        private void FormServer_Load(object sender, EventArgs e)
        {

        }

        public void AddHistory(string text)
        {
            this.Invoke((MethodInvoker)delegate 
            { 
                richTextBoxHistory.AppendText($"{text}\n");
            });
        }

        private void button_ServerStart(object sender, EventArgs e)
        {
            if (buttonServerStart.Text == "Запустить сервер")
            {
                server.Start(Convert.ToInt32(textBoxPort.Text));
                buttonServerStart.Text = "Остановить сервер";
            }
            else
            {
                server.Stop();
                buttonServerStart.Text = "Запустить сервер";
            }
        }
    }
}
