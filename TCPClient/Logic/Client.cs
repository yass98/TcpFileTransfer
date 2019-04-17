﻿using System;
using System.IO;
using System.Windows;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TCPClient.Logic
{
    using Models;
    using Logic.Utils;

    public class Client
    {
        static ClientModel vm = MainModel.Instance.Client;

        static DispatcherTimer timer;

        static bool IsCancelling = false;

        public static void Send()
        {            
            IsCancelling = false;
            var ip = vm.DestinationAddress.Ip;
            var port = vm.DestinationAddress.Port;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var file = new FileStream(vm.FileToSend.FullName, FileMode.Open, FileAccess.Read))
                    using (var client = new TcpClient(ip, port))
                    using (var stream = client.GetStream())
                    {
                        var fileHeader = new FileModel(vm.FileToSend);
                        Common.WriteFileHeader(stream, fileHeader);
                        var serverHeader = Common.ReadFileHeader(stream);
                        var start = serverHeader.Offset;
                        // Передаём байты файла
                        vm.SendingTime = new DateTime();
                        for (var i = start; i < file.Length;)
                        {
                            var arr = new byte[1024];
                            var count = file.Read(arr, 0, arr.Length);
                            stream.Write(arr, 0, count);

                            i += count;
                            vm.SentBytes = i;

                            if (IsCancelling)
                            {
                                client.Client.Shutdown(SocketShutdown.Both);
                                timer?.Stop();
                                break;
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    timer?.Stop();
                    MessageBox.Show(ex.Message, "При передаче файла возникла ошибка");
                    vm.SendingTime = new DateTime();
                }
                finally
                {
                    if (vm.SentBytes < vm.FileToSend.Length)
                    {

                    }
                }
            });

            timer = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 0, 100),
                DispatcherPriority.Normal, 
                new EventHandler((o, e) =>
                {
                    if (vm.SentBytes < vm.FileToSend.Length)
                        vm.SendingTime = vm.SendingTime.AddMilliseconds(100);
                }),
                Dispatcher.CurrentDispatcher
            );

        }

        public static void Cancel()
        {
            IsCancelling = true;
        }
    }
}
