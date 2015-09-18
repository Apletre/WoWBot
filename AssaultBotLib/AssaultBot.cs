using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using TechLib;
using System.Windows.Forms;

namespace AssaultBotLib
{
    public class AssaultClient
    {
        Socket Socket_reciever;
        Thread client;
        IPEndPoint server_endpoint;
        bool working = false;
        volatile bool threat;
        int packet_size = 300;

        AssaultBotNameMsg name = new AssaultBotNameMsg();
        public toAssaultBotMsg to_do = new toAssaultBotMsg();
        
        

        public AssaultClient(string name)
        {
            this.name.name = name;
        }

        void Init()
        {
            StreamReader sr = new StreamReader("ip_client_config.txt");

            string ip = sr.ReadLine();
            int port = Convert.ToInt32(sr.ReadLine());

            sr.Close();

            Socket_reciever = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress server_ip_adr = IPAddress.Parse(ip);
            server_endpoint = new IPEndPoint(server_ip_adr, port + 50);
        }

        public bool Threat
        {
            get { return threat; }
            private set { }
        }

        void Recieve()
        {
            bool conn=false;
            int bad_packets=0;
            toAssaultBotMsg msg = new toAssaultBotMsg();

            while (working)
            {
                try
                {
                    msg = Converter.RecieveAndDecode<toAssaultBotMsg>(Socket_reciever,ref conn,ref bad_packets,packet_size);
                    threat = msg.under_attack;

                    if (msg.to_do_code != Request.DoNothing)
                    {
                        to_do.player_id = msg.player_id;
                        to_do.to_do_code = msg.to_do_code;
                    }
                }
                catch { }
            }
        }

        public void Start()
        {
            if (working != true)
            {
                Init();

                working = true;

                while (!Socket_reciever.Connected)
                {
                    try
                    {
                        Socket_reciever.Connect(server_endpoint);
                    }
                    catch { }
                }

                bool conn=false;

                Converter.CodeAndSend<AssaultBotNameMsg>(Socket_reciever, name, ref conn, 200);

                client = new Thread(Recieve);
                client.IsBackground = true;
                client.Start();
            }
            else throw new Exception("Already working Exception");
        }

        public void Close()
        {
            if (working != false)
            {
                working = false;
                byte[] arr = new byte[1];
                Socket_reciever.Send(arr);
                Socket_reciever.Close();
                client.Join();
            }
            else throw new Exception("Not started Exceprion!");
        }
    }

    public abstract class AssaultBot
    {
        Thread bot;
        protected bool working;
        protected  AssaultClient clnt;
        public WoWMemReader wmr;

        public WoWLivingObj target = new Target();
        public SelfCD self;
        public KeyToWindowSender snd_keys;
        public ulong curr_target_guid;
        public bool smart_win_recog = false;
        public bool to_drink = false;
        public bool auto_follow = false;
        public int[] target_debuff_arr;
        protected int delay;
        protected StreamWriter sw;

        public bool after_spell_follow = false;

        public abstract class SelfCD : WoWLivingObj
        {
            protected static int ping = 200;
            protected static int tick = 200;

            static int g = 0;
            protected int gcd = 1500;

            public int[] arr = new int[12];
            protected int[] spells_cd = new int[12];
            protected int[] spells_G_cd = new int[12];

            public SelfCD()
            {
                CDInit();
            }

            public void Set0SpellCD()
            {
                arr[0] = spells_cd[0] + ping;
                G = spells_G_cd[0] + ping;
            }

            public void Set1SpellCD()
            {
                arr[1] = spells_cd[1] + ping;
                G = spells_G_cd[1] + ping;
            }

            public void Set2SpellCD()
            {
                arr[2] = spells_cd[2] + ping;
                G = spells_G_cd[2] + ping;
            }

            public void Set3SpellCD()
            {
                arr[3] = spells_cd[3] + ping;
                G = spells_G_cd[3] + ping;
            }

            public void Set4SpellCD()
            {
                arr[4] = spells_cd[4] + ping;
                G = spells_G_cd[4] + ping;
            }

            public void Set5SpellCD()
            {
                arr[5] = spells_cd[5] + ping;
                G = spells_G_cd[5] + ping;
            }

            public void Set6SpellCD()
            {
                arr[6] = spells_cd[6] + ping;
                G = spells_G_cd[6] + ping;
            }

            public void Set7SpellCD()
            {
                arr[7] = spells_cd[7] + ping;
                G = spells_G_cd[7] + ping;
            }

            public void Set8SpellCD()
            {
                arr[8] = spells_cd[8] + ping;
                G = spells_G_cd[8] + ping;
            }

            public void Set9SpellCD()
            {
                arr[9] = spells_cd[9] + ping;
                G = spells_G_cd[9] + ping;
            }

            public void Set10SpellCD()
            {
                arr[10] = spells_cd[10] + ping;
                G = spells_G_cd[10] + ping;
            }

            public void Set11SpellCD()
            {
                arr[11] = spells_cd[11] + ping;
                G = spells_G_cd[11] + ping;
            }

            public static int G
            {
                get
                {
                    return g;
                }
                set
                {
                    if (value <= 0)
                        g = 0;
                    else
                        g = value;
                }
            }

            public void SetGCD(int m_secs, bool has_ping)
            {
                if (has_ping)
                    G = m_secs + ping;
                else
                    G = m_secs;
            }

            public void CDdec()
            {
                int tmp_time;
                for (int i = 0; i < arr.Length; i++)
                {
                    tmp_time = arr[i] - tick;
                    if (tmp_time > 0)
                        arr[i] = tmp_time;
                    else
                        arr[i] = 0;
                }
            }
            public static void GCDdec()
            {
                int tmp_time;
                tmp_time = G - tick;
                if (tmp_time > 0)
                    G = tmp_time;
                else
                    G = 0;
            }
            public abstract void CDInit();
            public abstract void Reset();

        }
        public class Target : WoWLivingObj
        {
            public int Hp
            {
                get
                {
                    return (int)hp;
                }
                private set
                { }
            }
            public int Max_hp
            {
                get
                {
                    return (int)max_hp;
                }
                private set { }
            }
            public int Mp
            {
                get
                {
                    return (int)mp;
                }
                private set { }
            }
            public int Max_mp
            {
                get
                {
                    return (int)max_mp;
                }
                private set { }
            }
        }

        public AssaultBot(string name,int delay)
        {
            Process WowProc = GetCharWowProc(name);
            wmr = new WoWMemReader((uint)WowProc.Id);
            snd_keys = new KeyToWindowSender(WowProc);
            this.delay = delay;
        }

        public abstract void Do();

        public abstract void AfterSpellFollow();

        public Keys KeyChoice(int num)
        {
            switch (num)
            {
                case 1:
                    return Keys.D1;
                case 2:
                    return Keys.D2;
                case 3:
                    return Keys.D3;
                case 4:
                    return Keys.D4;
                case 5:
                    return Keys.D5;
                case 6:
                    return Keys.D6;
                case 7:
                    return Keys.D7;
                case 8:
                    return Keys.D8;
                case 9:
                    return Keys.D9;
                case 0:
                    return Keys.D0;
            }
            return Keys.N;///
        }

        public bool WindowFocused(bool todo)
        {
            if (todo)
            {
                if (KeyToWindowSender.GetForegroundWindowHandle() == snd_keys.WowWindowHndl)
                    return true;
            }
            return false;
        }

        public Process GetCharWowProc(string name)
        {
            Process[] arr = Process.GetProcessesByName("WoW");

            foreach (Process item in arr)
            {
                WoWMemReader wmRRRR = new WoWMemReader((uint)item.Id);
                if (wmRRRR.WoWProcessHasLocalPlayerName(name))
                {
                    return item;
                }
            }
            return null;
        }

        public bool HasBuff(int buff_id)
        {
            for (int i = 0; i < self.buff_arr.Length; i++)
            {
                if (self.buff_arr[i] == buff_id) return true;
            }

            return false;
        }

        public bool TargetHasDebuff(int debuff_id)
        {
            for (int i = 0; i < target_debuff_arr.Length; i++)
            {
                if (target_debuff_arr[i] == debuff_id) return true;
            }

            return false;
        }

        public void Start()
        {
            clnt = new AssaultClient(wmr.LocalPlayerName);
            clnt.Start();
            sw = new StreamWriter("log.txt", false);
            working = true;
            bot = new Thread(Do);
            bot.IsBackground = true;
            bot.Start();
        }

        public void Close()
        {
            sw.Close();
            working = false;
            bot.Join();
            clnt.Close();
        }
    }

    public abstract class State
    {
        public AssaultBot bot;

        public State(AssaultBot bot)
        {
            this.bot = bot;
        }
        public abstract void Do();
    }
}
