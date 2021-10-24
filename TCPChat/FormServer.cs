using System;
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
