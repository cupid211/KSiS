using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace trcert
{
    class Program
    {
        static void Main(string[] args)
        {
            string ip;
            Console.Write("tracert "); ;
            ip = Console.ReadLine();
            IPAddress adress;
            if (IPAddress.TryParse(ip, out adress))
            {
                try
                {
                    string domen = System.Net.Dns.GetHostEntry(ip).HostName;
                    Console.WriteLine($"\n Трассировка маршрута к {domen}  [{ip}] \n максимальное число прыжков 30:");
                }
                catch
                {
                    Console.WriteLine($"\n Трассировка маршрута к [{ip}] \n максимальное число прыжков 30:");
                }
            }

            else
            {
                try
                {
                    string domen = System.Net.Dns.GetHostEntry(ip).HostName;
                    if (domen == ip)
                    {
                        ip = System.Net.Dns.GetHostEntry(ip).AddressList[0].ToString();
                    }
                    Console.WriteLine($"\n Трассировка маршрута к {domen}  [{ip}] \n максимальное число прыжков 30:\n");
                }
                catch
                {
                    Console.WriteLine("Не удается разрешить системное имя узла");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            }


            byte[] data = new byte[1024];

            IPAddress adr = IPAddress.Parse(ip);

            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);//1. все адреса, используемые сокетом(протокол IPv4) 2. для передачи icmp пакетов 3. протокол icmp
            IPHostEntry iphe = System.Net.Dns.GetHostEntry(adr);// контейнер для сведений об адресе веб-узлa
            IPEndPoint iep = new IPEndPoint(adr, 0); // конечная точка ip-адрес и номер порта
            EndPoint ep = (EndPoint)iep;//определяет сетевой адрес

            ICMP packet = new ICMP();//формируем ICMP пакет

            packet.Type = 0x08; //тип для эхо-запроса?
            packet.Code = 0x00;
            packet.Checksum = 0;


            int packetsize = packet.MessageSize + 8;//У кати +8 заголовка

            UInt16 chcksum = packet.getChecksum();
            packet.Checksum = chcksum;

            host.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout/*промежуток времени, после которого истечет время тайм-аута*/, 3000);

            int badcount = 0;
            bool stop = false;


            for (int i = 1; i < 30; i++)
            {
                Console.Write(i);
                host.SetSocketOption(SocketOptionLevel.IP/*ip сокеты*/, SocketOptionName.IpTimeToLive/*поле срока жизни заголовка*/, i);

                for (int j = 1; j < 4; j++)
                {
                    DateTime timestart = DateTime.Now;

                    try
                    {
                        host.SendTo(packet.getBytes(), packetsize, SocketFlags.None, iep);
                        data = new byte[1024];
                        int recv = host.ReceiveFrom(data, ref ep);// получает дейтаграмму и конечную точку источника
                        TimeSpan timestop = DateTime.Now - timestart;
                        ICMP response = new ICMP(data, recv);

                        if (response.Type == 11)
                        {
                            Console.Write("{0,10}", (timestop.Milliseconds.ToString()) + "мс");
                        }

                        if (response.Type == 0)
                        {
                            Console.Write("{0,10}", (timestop.Milliseconds.ToString()) + "мс");
                            stop = true;
                        }

                        badcount = 0;
                    }
                    catch (SocketException)
                    {
                        Console.Write("{0,10}", "*");
                        badcount++;

                    }
                }
                if (badcount == 3 && !stop)
                {
                    Console.WriteLine("    Превышен интервал ожидания запроса");
                    badcount = 0;
                }
                else
                {
                    try
                    {
                        string domen = System.Net.Dns.GetHostEntry(IPAddress.Parse(ep.ToString().Split(':')[0])).HostName;
                        Console.WriteLine("    " + domen +"  ["+ep.ToString().Split(':')[0]+"]");
                    }
                    catch
                    {
                        Console.WriteLine("    " + ep.ToString().Split(':')[0]);
                    }
                }
                   
                if (stop)
                {
                    Console.WriteLine("\nТрассировка завершена");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            }
            host.Close();
            Console.ReadKey();
        }
        class ICMP
        {
            public byte Type;
            public byte Code;
            public UInt16 Checksum;
            public ushort id;
            public ushort number;
            public int MessageSize;
            public byte[] Message = new byte[1024];

            public ICMP()
            {
            }

            // Ответ на наш запрос
            public ICMP(byte[] data, int size)
            {
                Type = data[20];
                Code = data[21];
                Checksum = BitConverter.ToUInt16(data, 22);
                id = BitConverter.ToUInt16(data, 24);
                number = BitConverter.ToUInt16(data, 26);
                MessageSize = size - 28;
                Buffer.BlockCopy(data, 28, Message, 0, MessageSize);
            }

            public byte[] getBytes()
            {
                byte[] data = new byte[MessageSize + 9];
                Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, data, 0, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, data, 1, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, data, 2, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(id), 0, data, 4, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(number), 0, data, 6, 2);
                Buffer.BlockCopy(Message, 0, data, 8, MessageSize);
                return data;
            }

            public UInt16 getChecksum()
            {
                UInt32 chcksm = 0;
                byte[] data = getBytes();
                int packetsize = MessageSize + 8;
                int index = 0;

                while (index < packetsize)
                {
                    chcksm += Convert.ToUInt32(BitConverter.ToUInt16(data, index));
                    index += 2;
                }
                chcksm = (chcksm >> 16) + (chcksm & 0xffff);
                chcksm += (chcksm >> 16);
                return (UInt16)(~chcksm);
            }
        }
    }
}