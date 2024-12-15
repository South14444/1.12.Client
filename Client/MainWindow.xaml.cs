using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Timers;

namespace Client
{
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private System.Timers.Timer _autoMessageTimer;
        private Random _random;
        public MainWindow()
        {
            InitializeComponent();
            _random = new Random();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text;
            int port = int.Parse(PortTextBox.Text);

            try
            {
                _client = new TcpClient(ipAddress, port);
                _stream = _client.GetStream();

                StatusLabel.Content = "Подключено";

                _receiveThread = new Thread(ReceiveMessages) { IsBackground = true };
                _receiveThread.Start();

                int mode = ModeComboBox.SelectedIndex;
                SetupMode(mode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void SetupMode(int mode)
        {
            switch (mode)
            {
                case 0: // Человек — человек
                    StopAutoMessaging();
                    break;

                case 1: // Человек — компьютер
                    StopAutoMessaging();
                    break;

                case 2: // Компьютер — человек
                    StartAutoMessaging();
                    break;

                case 3: // Компьютер — компьютер
                    StartAutoMessaging();
                    break;

                default:
                    StopAutoMessaging();
                    break;
            }
        }

        private void StartAutoMessaging()
        {
            if (_autoMessageTimer == null)
            {
                _autoMessageTimer = new System.Timers.Timer(2000);
                _autoMessageTimer.Elapsed += AutoSendMessage;
            }
            _autoMessageTimer.Start();
        }

        private void StopAutoMessaging()
        {
            _autoMessageTimer?.Stop();
        }

        private void AutoSendMessage(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_stream == null || !_stream.CanWrite) return;

                string[] phrases = { "Привет!", "Как дела?", "Что нового?", "До свидания!", "Bye" };
                string randomMessage = phrases[_random.Next(phrases.Length)];

                SendMessage(randomMessage);

                if (randomMessage == "Bye")
                {
                    StopAutoMessaging();
                }
            });
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            string message = SendMessageTextBox.Text;
            SendMessage(message);
        }

        private void SendMessage(string message)
        {
            try
            {
                if (_stream != null && _stream.CanWrite)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    _stream.Write(data, 0, data.Length);
                    MessagesTextBox.AppendText($"Вы: {message}\n");
                    SendMessageTextBox.Clear();

                    if (message == "Bye")
                    {
                        StopAutoMessaging();
                        Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    if (_stream == null || !_stream.CanRead) break;

                    byte[] buffer = new byte[1024];
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Dispatcher.Invoke(() => MessagesTextBox.AppendText($"Сервер: {message}\n"));

                        if (message == "Bye")
                        {
                            Dispatcher.Invoke(Disconnect);
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                Dispatcher.Invoke(() => StatusLabel.Content = "Отключено");
            }
        }

        private void Disconnect()
        {
            StopAutoMessaging();

            _stream?.Close();
            _client?.Close();

            StatusLabel.Content = "Отключено";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
        }
    }
}