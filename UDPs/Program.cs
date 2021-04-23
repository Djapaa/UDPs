using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDPs
{
    class Program
    {

        const string helpstring = "UDPs<mode> [file] [adress]\n"+
            "\tmode:\n"+
            "\t\t s -sender mode\n" +
            "\t\t r -reciver mode\n"+
            "\tfile - file to send\n"+
            "\taddress - host to send";
        static UdpClient udpSender;
        static UdpClient udpReciver;

        const int sendPort = 9000;
        const int recivePort  = 9050;
        const int packetSize = 8192;

        static void Sender(string path,string address) {

            IPAddress ipAddr;
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(address);
                ipAddr = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                /*метод берет из addresslist вытаскиваем самый 1 или 
               по умолчанию. параметр с Linq берет адресс IPV4 */
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
            IPEndPoint sendEndpoint = new IPEndPoint(ipAddr, sendPort);// отправить данные по адруссу и порту
            IPEndPoint reciveEndpint = null;//от кого пришло 
            try
            {
                udpReciver = new UdpClient(recivePort);

            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
            using (FileStream fsSource = new FileStream(path,FileMode.Open, FileAccess.Read))
            {
                int numBytesToRead = (int)fsSource.Length; // кол во байтов сколько предстоит посчитать
                int nubBytesToReaded = 0;// кол во байтов сколько уже прочитанно из файлов

                string name = Path.GetFileName(path);//имя файла в файловой системе
                byte[] packetSend;
                byte[] packetRecieve;

                packetSend = Encoding.Unicode.GetBytes(name);// кодируем символы в последовательность байт. Сформировали пакет
                udpReciver.Client.ReceiveTimeout = 5000;
                udpSender.Send(packetSend, packetSend.Length, sendEndpoint);// отправляем пакет. пакет размер место
                packetRecieve = udpReciver.Receive(ref reciveEndpint);// ждем ответ

                int parts = (int)fsSource.Length / packetSize;
                if ((int)fsSource.Length % packetSize != 0) parts++;//кол во частей которые нужно передать 
                packetSend = BitConverter.GetBytes(parts);// переводим кол во частей в байт
                udpSender.Send(packetSend, packetSend.Length, sendEndpoint);// отправляем пакет. пакет размер место
               packetRecieve = udpReciver.Receive(ref reciveEndpint);// ждем ответ

                int n = 0;
                packetSend = new byte[packetSize];

                for(int i=0; i<parts - 1; i++)
                {
                    n = fsSource.Read(packetSend, 0, packetSize);//n возвращает сколько данных мы вычли
                    if (n == 0) break;
                    nubBytesToReaded += n;
                    numBytesToRead = -n;
                    udpSender.Send(packetSend, packetSend.Length, sendEndpoint);// отправляем пакет. пакет размер место
                    packetRecieve = udpReciver.Receive(ref reciveEndpint);// ждем ответ



                }
                packetSend = new byte[numBytesToRead];// оставшиеся байты
                n = fsSource.Read(packetSend, 0, numBytesToRead);
                udpSender.Send(packetSend, packetSend.Length, sendEndpoint);// отправляем пакет. пакет размер место
                packetRecieve = udpReciver.Receive(ref reciveEndpint);// ждем ответ


            }


            Console.WriteLine("File sent successfully");
        }
        static void Reciver() {


            IPEndPoint reciveEndPoint = null;
            try
            {
                udpReciver = new UdpClient(sendPort);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
            byte[] packetSend = new byte[1];// пакет потверждения 
            byte[] packetRecieve;
            packetSend[0] = 1;
            packetRecieve = udpReciver.Receive(ref reciveEndPoint);// ждем подключения 

            IPEndPoint sendEndpoint = new IPEndPoint(reciveEndPoint.Address, recivePort);// из пакета который пришел достаем адресс и порт 

            string name = Encoding.Unicode.GetString(packetRecieve);

            udpSender.Send(packetSend, packetSend.Length, sendEndpoint);

            udpReciver.Client.ReceiveTimeout = 5000;
            packetRecieve = udpReciver.Receive(ref reciveEndPoint);// принимаем пакет

            int parts = BitConverter.ToInt32(packetRecieve,0);
            udpSender.Send(packetSend, packetSend.Length, sendEndpoint);
            using(FileStream fsDest = new FileStream (name,FileMode.Create, FileAccess.Write)){
                for (int i = 0; i < parts; i++) ;
                packetRecieve = udpReciver.Receive(ref reciveEndPoint);
                fsDest.Write(packetRecieve, 0, packetRecieve.Length);
                udpSender.Send(packetSend, packetSend.Length, sendEndpoint);

            }




            Console.WriteLine("File Recieved");
        }
        static void Main(string[] args)
        {

            udpSender = new UdpClient();
            udpReciver = new UdpClient();



            if (args.Length < 1)
            {
                Console.WriteLine(helpstring);
            }
            else if(args[0] == "s")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Not enoufh parameters");
                    Console.WriteLine(helpstring);
                }
                else Sender(args[1],args[2]);
            }
            else if (args[0] == "r")
            {
                Reciver();
            }
            else Console.WriteLine(helpstring);




            Console.ReadLine();
            udpReciver.Close();
            udpSender.Close();
        }
    }
}
