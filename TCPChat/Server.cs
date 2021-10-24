using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

namespace TCPChat
{
    /// <summary>
    /// Сервер чата
    /// </summary>
    public class Server
    {
        private bool active = false;
        private Thread listenThread = null;
        private byte[] bytes = new byte[1024];
        private TcpListener listener = null;
        private List<AuthorizedСlient> clients = new List<AuthorizedСlient>();
        private byte[] Data { get; set; }
       
        /// <summary>
        /// Сервер запущен
        /// </summary>
        public bool IsStart { get { return active; } }

        /// <summary>
        /// Форма сервера
        /// </summary>
        public static FormServer FormServer;

        public Server(FormServer formServer)
        {
            FormServer = formServer;
        }

        /// <summary>
        /// Запустить сервер
        /// </summary>
        /// <param name="port">Порт для прослушивания</param>
        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listenThread = new Thread(Listen);
            listenThread.Start();
        }

        /// <summary>
        /// Остановить сервер
        /// </summary>
        public void Stop()
        {
            active = false;
            if (listener != null)
                listener.Stop();
            DeleteClients();
        }

        /// <summary>
        /// Прослушивать поток
        /// Сервер прослуживает только новые подключения
        /// Все обмены данных выполняются в отдельном потоке AuthorizedСlient
        /// </summary>
        private void Listen()
        {
            active = true;
            while (active)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    stream.Read(bytes, 0, bytes.Length);
                    var obj = Deserialize(bytes);

                    if (obj is AuthorizationData)
                    {
                        AuthorizationData authorizationData = obj as AuthorizationData;
                        authorizationData.Allowed = true;
                        var authorizedСlient = new AuthorizedСlient(authorizationData, client, this);
                        clients.Add(authorizedСlient);
                        FormServer.AddHistory($"Подключился - {authorizationData.Name}");
                        Send(stream, authorizationData);
                        Message msg = new Message();
                        msg.NameSender = authorizationData.Name;
                        msg.Text = "Подключился";
                        SendBroadcastMessage(msg);
                        FormServer.Invoke((MethodInvoker)delegate
                        {
                            FormServer.dataGridView1.Rows.Add(authorizationData.ID, authorizationData.Name);
                        });
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                }
                bytes = new byte[1024];
            }
        }

        /// <summary>
        /// Удалить клиента
        /// </summary>
        /// <param name="authorizationData"></param>
        public void DeleteClient(AuthorizedСlient authorizationData)
        {
            authorizationData.TcpClient.Close();
            if (clients.Remove(authorizationData))
            {
                FormServer.AddHistory($"Отключился - {authorizationData.Name}");
                Message msg = new Message();
                msg.NameSender = authorizationData.Name;
                msg.Text = "Отключился";
                SendBroadcastMessage(msg);

                FormServer.Invoke((MethodInvoker)delegate
                {
                    DataGridViewRow delRow = null;
                    for (int i = 0; i < FormServer.dataGridView1.Rows.Count; i++)
                    {
                        if(FormServer.dataGridView1.Rows[i].Cells["ColumnId"].Value.ToString() == authorizationData.ID)
                        {
                            delRow = FormServer.dataGridView1.Rows[i];
                            break;
                        }
                    }
                    FormServer.dataGridView1.Rows.Remove(delRow);
                });
            }
        }

        /// <summary>
        /// Удалить всех клиентов
        /// </summary>
        /// <param name="authorizationData"></param>
        public void DeleteClients()
        {
            try
            {
                AuthorizedСlient[] delClient = new AuthorizedСlient[clients.Count];
                clients.CopyTo(delClient);
                foreach (var client in delClient)
                {
                    DeleteClient(client);
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Отправка сообщениям всем клиентам
        /// </summary>
        /// <param name="message"></param>
        public void SendBroadcastMessage(Message message)
        {
            foreach (var client in clients)
            {
                Send(client.TcpClient.GetStream(), message);
            }
        }

        /// <summary>
        /// Отправить сообщение клиенту (если получатель не указан то всем клиентам)
        /// </summary>
        /// <param name="message">С</param>
        public void SendMessage(Message message)
        {
            // Отправка сообщения всем если получатель не указан
            if (string.IsNullOrEmpty(message.Recipient))
            {
                SendBroadcastMessage(message);
                return;
            }

            AuthorizedСlient client = clients.Where(c => c.ID == message.Recipient).FirstOrDefault();
            if (client != null)
                Send(client.TcpClient.GetStream(), message);
            else
                FormServer.AddHistory($"Клиент {message.Recipient} не найден!");
        }

        public static byte[] Serialize(object anySerializableObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
                return memoryStream.ToArray();
            }
        }

        public static object Deserialize(byte[] data)
        {
            object obj = null;
            try
            {
                using (var memoryStream = new MemoryStream(data))
                    obj = (new BinaryFormatter()).Deserialize(memoryStream);
            }
            catch (SerializationException ex)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return obj;
        }

        public object Deserialize()
        {
            return Deserialize(this.Data);
        }

        public static void Send(NetworkStream networkStream, object obj)
        {
            var data = Serialize(obj);
            networkStream.Write(data, 0, data.Length);
        }

        public static void AddHisory(string text)
        {
            if(FormServer != null)
                FormServer.AddHistory(text);
        }
    }

    /// <summary>
    /// Авторизованный клиент
    /// </summary>
    public class AuthorizedСlient : AuthorizationData
    {
        private byte[] bytes = new byte[1024];
        private Server server = null;
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="authorizationData">Данные авторизации</param>
        /// <param name="client">TcpClient клиент</param>
        /// <param name="server">Сервер</param>
        public AuthorizedСlient(AuthorizationData authorizationData, TcpClient client, Server server)
        {
            this.ID = authorizationData.ID;
            this.Name = authorizationData.Name;
            this.Allowed = authorizationData.Allowed;
            this.TcpClient = client;
            this.server = server;
            new Thread(Listen).Start();
        }

        /// <summary>
        /// Прослушивать поток
        /// </summary>
        public void Listen()
        {
            NetworkStream stream = TcpClient.GetStream();
            try
            {
                while (true)
                {
                    int sizeReadBytes = stream.Read(bytes, 0, bytes.Length);
                    if (sizeReadBytes == 0)
                        break;

                    var obj = Server.Deserialize(bytes);

                    // СООБЩЕНИЕ
                    if (obj is Message)
                    {
                        Message message = obj as Message;
                        Server.AddHisory($"{message.NameSender}:{message.Text}");
                        if (string.IsNullOrEmpty(message.Recipient))
                            server.SendBroadcastMessage(message);
                        else
                            server.SendMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            server.DeleteClient(this);
        }
    }

    /// <summary>
    /// Данные авторизации
    /// </summary>
    [Serializable]
    public class AuthorizationData
    {
        /// <summary>Идентификатор клиента</summary>
        public string ID { get; set; }
        /// <summary>Наименование клиента</summary>
        public string Name { get; set; }
        /// <summary>Доступ разрешён</summary>
        public bool Allowed { get; set; }
    }

    /// <summary>
    /// Сообщение
    /// </summary>
    [Serializable]
    public class Message
    {
        /// <summary> Отправитель </summary>
        public string Sender { get; set; }

        /// <summary> Название отправителя </summary>
        public string NameSender { get; set; }

        /// <summary> Получатель </summary>
        public string Recipient { get; set; }

        /// <summary> Сообщение </summary>
        public string Text { get; set; }
    }
}
