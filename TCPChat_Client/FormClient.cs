using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TCPChat;

namespace TCPChat_Client
{
    public partial class FormClient : Form
    {
        Client client;
        public FormClient()
        {
            InitializeComponent();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (buttonConnect.Text == "Подключиться")
            {
                client = new Client(textBoxIP.Text, Convert.ToInt32(textBoxPort.Text), this, textBoxName.Text);
                client.Connect();
                buttonConnect.Text = "Отключиться";
            }
            else
            {
                client.DisConnect();
                buttonConnect.Text = "Подключиться";
            }
        }

        public void AddHistory(string text)
        {
            this.Invoke((MethodInvoker)delegate
            {
                richTextBoxHistory.AppendText($"{text}\n");
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            client.SendMessage(richTextBoxMessage.Text);
            richTextBoxMessage.Clear();
        }
    }
}
