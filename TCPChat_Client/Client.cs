using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPChat;

namespace TCPChat_Client
{
    /// <summary>
    /// Клиент сервера
    /// </summary>
    public class Client
    {
        private TcpClient client = null;
        private NetworkStream stream = null;
        private Thread listenThread = null;
        private bool isConnected = false;
        private bool isAuthorized = false;
        private string iPAddress = null;
        private FormClient formClient = null;
        private int port = 2021;
        /// <summary> Идентификатор </summary>
        public string ID { get; }
        /// <summary> Название </summary>
        public string Name { get; set; }
        /// <summary> Подключен </summary>
        public bool IsConnected { get { return isConnected; } }
        /// <summary> Авторизирован </summary>
        public bool IsAuthorized { get { return isAuthorized; } }
        

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="iPAddress">IP адрес сервера</param>
        /// <param name="port">Порт сервера</param>
        /// <param name="name">Наименование клиента</param>
        /// <param name="password">Пароль для подключения к серверу</param>
        public Client(string iPAddress, int port, FormClient formClient, string name = "Client")
        {
            this.ID = Guid.NewGuid().ToString();
            this.iPAddress = iPAddress;
            this.port = port;
            this.Name = name;
            this.formClient = formClient;
        }

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        public bool Connect()
        {
            bool result = false;
            try
            {
                this.client = new TcpClient(iPAddress, port);
                AuthorizationData authorizationData = new AuthorizationData();
                authorizationData.ID = ID;
                authorizationData.Name = Name;
                stream = client.GetStream();
                listenThread = new Thread(Listen);
                Server.Send(stream, authorizationData);
                listenThread.Start();
                result = true;
            }
            catch (Exception ex)
            {

            }

            return isConnected = result;
        }

        /// <summary>
        /// Отключиться от сервера
        /// </summary>
        public void DisConnect()
        {
            if (isConnected)
            {
                stream.Close();
                client.Close();
                isConnected = false;
            }
        }

        /// <summary>
        /// Отправить серверу
        /// </summary>
        /// <param name="obl"></param>
        private void Send(object obl)
        {
            if (IsConnected)
            {
                Server.Send(stream, obl);
            }
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        /// <param name="recipient">Получатель</param>
        public void SendMessage(string text, string recipient = null)
        {
            Message message = new Message();
            message.NameSender = this.Name;
            message.Sender = this.ID;
            message.Recipient = recipient;
            message.Text = text;
            Send(message);
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public void SendMessage(Message message)
        {
            Send(message);
        }

        /// <summary>
        /// Прослушивать поток
        /// </summary>
        private void Listen()
        {
            Byte[] data = new Byte[1024];

            try
            {
                while (true)
                {
                    int sizeReadBytes = stream.Read(data, 0, data.Length);
                    if (sizeReadBytes == 0)
                        break;


                    var obj = Server.Deserialize(data);

                    // АВТОРИЗАЦИЯ
                    if (obj is AuthorizationData)
                    {
                        AuthorizationData authorizationData = obj as AuthorizationData;
                        if (authorizationData.ID == this.ID)
                            isAuthorized = authorizationData.Allowed;
                    }

                    // СООБЩЕНИЕ
                    if (obj is Message)
                        formClient.AddHistory($"{((Message)obj).NameSender}: {((Message)obj).Text}");

                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                DisConnect();
            }
        }
    }
}
