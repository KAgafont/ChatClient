
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
using System.Threading;
using System.IO;
using System.Reflection;

namespace ChatClient
{
    public partial class frmMain : Form
    {
        private TcpClient Client = new TcpClient();
        private IPAddress IP;
        private string Nickname;

        private RichTextBox rtbChat; // Добавляем поле для RichTextBox для чата
        private TextBox tbNickname;  // Добавляем поле для TextBox ввода ника

        public frmMain()
        {
            InitializeComponent();

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IP = hostEntry.AddressList[0];

            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }

            // Добавляем поле для ввода никнейма
            tbNickname = new TextBox();
            tbNickname.Location = new Point(10, 10);
            tbNickname.Size = new Size(150, 20);
            this.Controls.Add(tbNickname);
            tbNickname.Name = "tbNickname";


            // Добавляем RichTextBox для чата
            rtbChat = new RichTextBox();
            rtbChat.Location = new Point(10, 100);  // Разместите где угодно
            rtbChat.Size = new Size(260, 150);      // Задайте размер
            this.Controls.Add(rtbChat);
            rtbChat.ReadOnly = true; // Чтобы пользователь не мог редактировать окно чата
            rtbChat.Name = "rtbChat";


            // Изменяем расположение и размер других элементов, чтобы освободить место
            tbIP.Location = new Point(10, 40);
            btnConnect.Location = new Point(170, 38);
            tbMessage.Location = new Point(10, 70);
            btnSend.Location = new Point(170, 68);


            //Настройка двойной буферизации для RichTextBox
            rtbChat.DoubleBuffered(true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                int Port = 1010;
                IPAddress IP = IPAddress.Parse(tbIP.Text);
                Client.Connect(IP, Port);

                // Отправляем никнейм на сервер

                Nickname = tbNickname.Text;
                byte[] nicknameBytes = Encoding.Unicode.GetBytes(Nickname);
                Stream stm = Client.GetStream();
                stm.Write(nicknameBytes, 0, nicknameBytes.Length);

                // Запускаем поток для приема сообщений
                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                btnConnect.Enabled = false;
                btnSend.Enabled = true;
                tbNickname.Enabled = false; //Запрещаем редактирование ника после подключения
            }
            catch (Exception ex)
            {
                MessageBox.Show("Введен некорректный IP-адрес или ошибка при подключении: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] buff = Encoding.Unicode.GetBytes(tbMessage.Text);
                Stream stm = Client.GetStream();
                stm.Write(buff, 0, buff.Length);
                tbMessage.Clear(); //Очищаем поле ввода после отправки

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке сообщения: " + ex.Message);
            }

        }

        // Метод для приема сообщений
        private void ReceiveMessages()
        {
            try
            {
                Stream stm = Client.GetStream();
                byte[] buff = new byte[1024];

                while (true)
                {
                    int bytesRead = stm.Read(buff, 0, buff.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.Unicode.GetString(buff, 0, bytesRead);
                        // Выводим сообщение в RichTextBox
                        rtbChat.Invoke((MethodInvoker)delegate
                        {
                            rtbChat.AppendText(message);
                            rtbChat.AppendText(Environment.NewLine); // Добавляем перенос строки после каждого сообщения

                        });

                    }
                    else
                    {
                        //Сервер закрыл соединение
                        MessageBox.Show("Сервер закрыл соединение.");
                        break;
                    }
                }
            }
            catch (IOException ex)
            {
                // Обработка ошибок при чтении/закрытии потока
                Console.WriteLine($"Error receiving data: {ex.Message}");
                MessageBox.Show($"Ошибка при получении сообщения от сервера: {ex.Message}");
            }
            finally
            {
                //Закрываем клиент
                Client.Close();

                rtbChat.Invoke((MethodInvoker)delegate
                {
                    rtbChat.AppendText("\nСоединение разорвано.");
                });

            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing client: {ex.Message}");
            }
        }

        private void tbIP_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbMessage_TextChanged(object sender, EventArgs e)
        {

        }
    }

    //Расширение для двойной буферизации RichTextBox
    public static class RichTextBoxExtensions
    {
        public static void DoubleBuffered(this RichTextBox control, bool setting)
        {
            Type richTextBoxType = typeof(RichTextBox);
            PropertyInfo doubleBufferedProperty = richTextBoxType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferedProperty.SetValue(control, setting, null);
        }
    }
}
