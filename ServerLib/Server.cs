using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using TechLib;
using DebuffDispell;
using System.Windows.Forms;

namespace ServerLib
{
    public class Server
    {
        VolatileBool working = new VolatileBool();
        public static Dictionary<string, WowForPartyMemberHealerCD> party_data = new Dictionary<string, WowForPartyMemberHealerCD>();
        StreamWriter sw = new StreamWriter("ServerLog.txt");

        PartyDataRetrivalServer clnt_srv;
        AssaultBotServer asbot_srv;
        int delay;

        VolatileBool priest_assist=new VolatileBool();
        VolatileBool abolish_disease = new VolatileBool();
        VolatileBool dispell_magic = new VolatileBool();
        VolatileBool dispell_curse = new VolatileBool();

        public bool PriestAssist
        {
            set
            {
                priest_assist.value = value;
            }
            private get { return false; }
        }

        public bool ToDispellMagic
        {
            set
            {
                dispell_magic.value = value;
            }
            private get { return false; }
        }

        public bool ToDispellCurse
        {
            set
            {
                dispell_curse.value = value;
            }
            private get { return false; }
        }

        public bool ToAbolishDisease
        {
            set
            {
                abolish_disease.value = value;
            }
            private get { return false; }
        }

        public int GetBadPacketsNum()
        {
            return clnt_srv.GetBadPacketsNum();
        }

        public int GetConnectedClientsNum()
        {
            return clnt_srv.GetConnectedClientsNum();
        }

        public int GetConnectedAssaultBotsNum()
        {
            return asbot_srv.GetConnectedAssaultBotsNum();
        }

        public Server(int delay)
        {
            this.delay = delay;
            WowForPartyMemberHealerCD.tick=delay;
            WowForPartyMemberHealerCD.ping=delay;
            clnt_srv = new PartyDataRetrivalServer(working, party_data);
            asbot_srv = new AssaultBotServer(working, delay,  party_data,priest_assist,abolish_disease,dispell_magic,dispell_curse);

            sw.WriteLine("Server Started!");
        }

        public void Close()
        {
            if (working.value != false)
            {
                working.value = false;
                clnt_srv.Close();
                asbot_srv.Close();
                party_data.Clear();
            }
            else throw new Exception("Not started Exceprion!");

        }

        public void Start()
        {
            if (working.value != true)
            {
                working.value = true;
                clnt_srv.Start();
                asbot_srv.Start();
            }
            else throw new Exception("Already working Exception");
        }
    }

    class PartyDataRetrivalServerVariables
    {
        protected static VolatileBool working;
        protected static int bad_packets;
        protected readonly int client_packet_size = 400;
        protected static Dictionary<string, WowForPartyMemberHealerCD> party_data;
    }

    class PartyDataRetrivalServer : PartyDataRetrivalServerVariables
    {
        static VolatileInt[] thread_flags;
        static int max_connections;

        Socket Socket_listener;
        Thread listen_thread;

        public PartyDataRetrivalServer(VolatileBool working, Dictionary<string, WowForPartyMemberHealerCD> party_data)
        {
            PartyDataRetrivalServerVariables.working = working;
            PartyDataRetrivalServerVariables.party_data = party_data;
        }

        int ConnectedNum
        {
            get
            {
                int count = 0;

                for (int i = 0; i < thread_flags.Length; i++)
                    if (thread_flags[i].perem != 0)
                        count++;
                return count;
            }
        }

        public int GetBadPacketsNum()
        {
            return bad_packets;
        }

        public int GetConnectedClientsNum()
        {
            return ConnectedNum;
        }

        void Init()
        {
            StreamReader sr = new StreamReader("ip_srv_config.txt");
            string ip = sr.ReadLine();
            int port = Convert.ToInt32(sr.ReadLine());
            max_connections = Convert.ToInt32(sr.ReadLine());
            sr.Close();

            thread_flags = new VolatileInt[max_connections];

            IPAddress ip_adr = IPAddress.Parse(ip);
            IPEndPoint ip_endpoint = new IPEndPoint(ip_adr, port);
            Socket_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket_listener.Bind(ip_endpoint);
        }

        void Listen()
        {
            Socket_listener.Listen(max_connections);

            while (working.value)
            {
                Socket handle = null;
                try
                {
                    handle = Socket_listener.Accept();
                }
                catch { }

                if (working.value)
                {
                    int thread_num = 0;

                    for (int i = 0; i < thread_flags.Length; i++)
                        if (thread_flags[i].perem == 0)
                            lock (thread_flags)
                            {
                                if (thread_flags[i].perem == 0)
                                {
                                    thread_flags[i].perem = 1;
                                    thread_num = i;
                                    break;
                                }
                            }

                    ThreadReceivingPlayerStats my_thread = new ThreadReceivingPlayerStats(thread_num, thread_flags, handle);
                }
            }
        }

        public void Close()
        {
            Socket_listener.Close();
            listen_thread.Join();
            PartyDataRetrivalServer.bad_packets = 0;
        }

        public void Start()
        {
            Init();
            listen_thread = new Thread(Listen);
            listen_thread.Start();
        }
    }

    class ThreadReceivingPlayerStats : PartyDataRetrivalServerVariables
    {
        Thread thread;
        string PlayerName;
        Socket handle;
        int thread_num;
        VolatileInt[] thread_flags;

        bool CopyData(WoWLivingObj inp, WowForPartyMemberHealerCD outpt)
        {
            if (inp == null || inp.name == null)
                return false;

            outpt.name = inp.name;
            outpt.buff_arr = inp.buff_arr;
            outpt.mp = inp.mp;
            outpt.max_mp = inp.max_mp;
            outpt.hp = inp.hp;
            outpt.max_hp = inp.max_hp;
            outpt.under_attack = inp.under_attack;
            outpt.type = inp.type;
            outpt.GUID = inp.GUID;
            return true;
        }

        public ThreadReceivingPlayerStats(int num_thread, VolatileInt[] thread_flags, Socket handle)
        {
            this.thread_num = num_thread;
            this.thread_flags = thread_flags;
            this.handle = handle;

            thread = new Thread(ConnectionHandler);
            thread.IsBackground = true;
            thread.Start();
        }

        void ConnectionHandler()
        {
            WowForPartyMemberHealerCD obj = new WowForPartyMemberHealerCD();
            bool end_of_transmition = false;


            while (!CopyData(Converter.RecieveAndDecode<WoWLivingObj>(handle, ref end_of_transmition, ref bad_packets, client_packet_size), obj)) ;

            lock (party_data)
            {
                party_data.Add(obj.name, obj);
            }
            PlayerName = obj.name;

            while (working.value)
            {
                CopyData(Converter.RecieveAndDecode<WoWLivingObj>(handle, ref end_of_transmition, ref bad_packets, client_packet_size), obj);

                if (end_of_transmition)
                {
                    lock (party_data)
                    {
                        party_data.Remove(PlayerName);
                    }
                    break;
                }
            }

            handle.Close();
            thread_flags[thread_num].perem = 0;
        }
    }

    class AssaultBotServerVariables
    {
        protected static VolatileBool working;
        protected readonly int client_packet_size = 300;
        protected static Dictionary<string, WowForPartyMemberHealerCD> party_data;
        protected static int delay;
    }

    class AssaultBotServer:AssaultBotServerVariables
    {
        Thread listen_for_subs_thread;
        ThreadSubscribersForThreat[] subs_arr;
        Socket Socket_listener_publisher;
        Thread publisher_thread;

        VolatileBool PriestHealAssist;
        VolatileBool AbolishDisease;
        VolatileBool DispellMagic;
        VolatileBool DispellCurse;

        int max_connections;

        DDPriestAssist priest;
        DDMageAssist mage;

        public AssaultBotServer(VolatileBool working, int delay, Dictionary<string, WowForPartyMemberHealerCD> party_data, VolatileBool PriestHealAssist, VolatileBool AbolishDisease, VolatileBool DispellMagic, VolatileBool DispellCurse)
        {
            AssaultBotServer.party_data = party_data;
            AssaultBotServerVariables.working = working;
            this.PriestHealAssist = PriestHealAssist;
            this.AbolishDisease = AbolishDisease;
            this.DispellMagic = DispellMagic;
            this.DispellCurse = DispellCurse;
            AssaultBotServerVariables.delay = delay;

            priest = new DDPriestAssist(delay,party_data);
            mage = new DDMageAssist(delay,party_data);
        }

        void Init()
        {
            StreamReader sr = new StreamReader("ip_srv_config.txt");
            string ip = sr.ReadLine();
            int port = Convert.ToInt32(sr.ReadLine());
            max_connections = Convert.ToInt32(sr.ReadLine());
            sr.Close();

            subs_arr = new ThreadSubscribersForThreat[max_connections];//не айс ибо не обязательно max_connections;

            IPAddress ip_adr = IPAddress.Parse(ip);

            IPEndPoint ip_endpoint_publisher = new IPEndPoint(ip_adr, port + 50);
            Socket_listener_publisher = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket_listener_publisher.Bind(ip_endpoint_publisher);
        }

        void Listen_for_subscrbrs()
        {
            Socket_listener_publisher.Listen(max_connections);

            while (working.value)
            {
                Socket handle = null;
                SubscrbrsCleaner clnr = new SubscrbrsCleaner(subs_arr);

                try
                {
                    clnr.Start();
                    handle = Socket_listener_publisher.Accept();
                    clnr.Stop();
                }
                catch { }

                if (working.value)
                {
                    SubscrbrsCleaner.CleanSubsArray(subs_arr);

                    for (int i = 0; i < subs_arr.Length; i++)
                        if (subs_arr[i] == null)
                        {
                            subs_arr[i] = new ThreadSubscribersForThreat(handle);
                            break;
                        }
                }
            }
        }

        int SubscribersNum
        {
            get
            {
                int count = 0;

                for (int i = 0; i < subs_arr.Length; i++)
                    if (subs_arr[i] != null)
                        count++;
                return count;
            }
        }

        public int GetConnectedAssaultBotsNum()
        {
            return SubscribersNum;
        }

        void Publish()
        {
            toAssaultBotMsg default_msg = new toAssaultBotMsg();

            while (working.value)
            {
                default_msg.under_attack = false;

                foreach (WoWLivingObj item in party_data.Values)
                {
                    if (item.under_attack)
                    {
                        default_msg.under_attack = true;
                        break;
                    }
                }

                for (int i = 0; i < subs_arr.Length; i++)
                {
                    if (subs_arr[i] != null && subs_arr[i].IsAlive())
                    {
                        if (subs_arr[i].name == "Lichinkin")
                            priest.Do(subs_arr[i], default_msg.under_attack, DispellMagic.value, AbolishDisease.value, PriestHealAssist.value);
                        else
                            if (subs_arr[i].name == "Format")
                                mage.Do(subs_arr[i], default_msg.under_attack, DispellCurse.value);
                            else
                                subs_arr[i].Send(default_msg);
                    }
                    
                        
                }

                Thread.Sleep(delay);
            }
        }    

        public void Close()
        {
            priest.Close();
            mage.Close();
            Socket_listener_publisher.Close();
            listen_for_subs_thread.Join();
            publisher_thread.Join();
        }

        public void Start()
        {
            Init();

            publisher_thread = new Thread(Publish);
            publisher_thread.Start();

            listen_for_subs_thread = new Thread(Listen_for_subscrbrs);
            listen_for_subs_thread.Start();
        }

        class SubscrbrsCleaner
        {
            Thread thrd;
            ThreadSubscribersForThreat[] arr;
            bool working = false;

            public SubscrbrsCleaner(ThreadSubscribersForThreat[] arr)
            {
                this.arr = arr;
            }

            void PeriodicCleanSubsArray()
            {
                while (working)
                {
                    CleanSubsArray(arr);
                    Thread.Sleep(delay);
                }
            }

            public static void CleanSubsArray(ThreadSubscribersForThreat[] subs_arr)
            {
                for (int i = 0; i < subs_arr.Length; i++)
                    if (subs_arr[i] != null && subs_arr[i].IsAlive() == false)
                        subs_arr[i] = null;
            }

            public void Start()
            {
                working = true;
                thrd = new Thread(PeriodicCleanSubsArray);
                thrd.Start();
            }

            public void Stop()
            {
                working = false;
                thrd.Abort();
                thrd.Join();
            }
        }
    }

    class ThreadSubscribersForThreat:AssaultBotServerVariables
    {
        Thread init_thrd;
        Socket handle;
        public string name;
        byte[] arr = new byte[1];

        public ThreadSubscribersForThreat(Socket handle)
        {
            this.handle = handle;

            init_thrd = new Thread(ConnectionHandler);
            init_thrd.IsBackground = true;
            init_thrd.Start();
        }

        void ConnectionHandler()
        {
            byte[] buffer = new byte[200];

            bool end_of_transmittion= true;
            int bad_packets=0;

            name = (Converter.RecieveAndDecode<AssaultBotNameMsg>(handle,ref end_of_transmittion,ref bad_packets, 200)).name;

            try
            {
                handle.Receive(new byte[1]);
            }
            catch { }
            
            handle.Close();
        }

        public void Send(toAssaultBotMsg msg)
        {
            bool conn=false;
            Converter.CodeAndSend<toAssaultBotMsg>(handle, msg,ref conn, client_packet_size);
        }

        public bool IsAlive()
        {
            return handle.Connected;
        }
    }

    class DDPriestAssist:DebuffPurify
    {
        public DDPriestAssist(int delay, Dictionary<string, WowForPartyMemberHealerCD>  party_data)
        {
            this.party_data = party_data;
            this.delay = delay;
        }

        Dictionary<string, WowForPartyMemberHealerCD> party_data;

        int GCD;
        int delay;
        int ShieldCD;
        int AbolishDiseaseCD;
        int DispellCD;
        bool to_heal;
        bool tmp_to_heal;
        WowForPartyMemberHealerCD member;
        private StreamWriter sw = new StreamWriter("PriestLog.txt");

        public void Do(ThreadSubscribersForThreat sbscr, bool under_attack, bool to_dispell, bool to_abolish_disease, bool assist)  // do1 - shield do2 - dispell do3 - abolish do4 - flash heal do5 - shadowform
        {
            toAssaultBotMsg msg = new toAssaultBotMsg();

            if (GCD == 0)
            {
                for (int i = 0; i < Party.Players.Length; i++)
                {
                    if (party_data.TryGetValue(Party.Players[i], out member))
                    {
                        if (to_dispell && toPurify(MagicToDispellArray, member.buff_arr))
                        {
                            msg.to_do_code = Request.do3;
                            msg.player_id = i;
                            SetGCD();

                            sw.WriteLine("Dispell "+Convert.ToString(i));

                            break;
                        }

                        if (to_abolish_disease && toPurify(DiseaseToDispellArray, member.buff_arr) && member.AbolishDisease == 0)
                        {
                            msg.to_do_code = Request.do2;
                            msg.player_id = i;
                            member.SetAbolishDisease();
                            SetGCD();

                            sw.WriteLine("AbolishDisease " + Convert.ToString(i));

                            break;
                        }


                        if (under_attack && assist)
                        {
                            if (i == 0 && member.hp > member.max_hp * 0.66)
                                to_heal = false;

                            if (i == 0 && member.hp < member.max_hp * 0.33)
                            {
                                to_heal = true;
                                tmp_to_heal = true;
                            }

                            if (to_heal == false && tmp_to_heal == true)
                            {
                                msg.to_do_code = Request.do5;
                                tmp_to_heal = false;
                                SetGCD();
                                break;
                            }

                            if (member.hp < member.max_hp * 0.40 && ShieldCD == 0 && member.Shield == 0 && member.hp > 0)
                            {
                                msg.to_do_code = Request.do1;
                                msg.player_id = i;
                                member.SetShield();
                                SetGCD();
                                ShieldCD = 4000 + delay;
                                break;
                            }

                            if (to_heal)
                            {
                                msg.to_do_code = Request.do4;
                                msg.player_id = 0;
                                SetGCD();
                                break;
                            }
                        }
                    }
                }
            }

            foreach (string item in Party.Players)
            {
                WowForPartyMemberHealerCD player_data;
                if(party_data.TryGetValue(item,out player_data))
                {
                    if (player_data.Shield < 0)
                        player_data.Shield = 0;
                    else
                        player_data.Shield -= delay;

                    if (player_data.AbolishDisease < 0)
                        player_data.AbolishDisease = 0;
                    else
                        player_data.AbolishDisease -= delay;
                }
            }

            CDdec();
            msg.under_attack = under_attack;
            sbscr.Send(msg);
        }

        void CDdec()
        {
            GCD -= delay;
            ShieldCD -= delay;
            DispellCD-=delay;
            AbolishDiseaseCD-=delay;

            if (GCD < 0) GCD = 0;
            if (ShieldCD < 0) ShieldCD = 0;
            if (DispellCD < 0) DispellCD = 0;
            if (AbolishDiseaseCD < 0) AbolishDiseaseCD = 0;
        }
        void SetGCD()
        {
            GCD = 1500 + delay;
        }

        public void Close()
        {
            sw.Close();
        }
    }

    class DDMageAssist : DebuffPurify
    {
        public DDMageAssist(int delay, Dictionary<string, WowForPartyMemberHealerCD> party_data)
        {
            this.party_data = party_data;
            this.delay = delay;
        }

        Dictionary<string, WowForPartyMemberHealerCD> party_data;

        int GCD;
        int delay;
        int DispellCurseCD;
        StreamWriter sw = new StreamWriter("MageLog.txt");

        public void Do(ThreadSubscribersForThreat sbscr, bool under_attack, bool to_dispell_curse)  // do1 - dispell curse
        {
            WowForPartyMemberHealerCD member;
            toAssaultBotMsg msg = new toAssaultBotMsg();

            if (GCD == 0)
            {
                for (int i = 0; i < Party.Players.Length; i++)
                {
                    if (party_data.TryGetValue(Party.Players[i], out member))
                    {
                        if (to_dispell_curse && toPurify(CurseToDispellArray, member.buff_arr))
                        {
                            msg.to_do_code = Request.do1;
                            msg.player_id = i;
                            SetGCD();

                            sw.WriteLine("Curse " + Convert.ToString(i));

                            break;
                        }
                    }
                }
                
            }
            CDdec();
            msg.under_attack = under_attack;
            sbscr.Send(msg);
        }

        void CDdec()
        {
            GCD -= delay;
            DispellCurseCD -= delay;

            if (GCD < 0) GCD = 0;
            if (DispellCurseCD < 0) DispellCurseCD = 0;
        }

        void SetGCD()
        {
            GCD = 1500 + delay;
        }

        public void Close()
        {
            sw.Close();
        }
    }

    public class HealBot : DebuffPurify
    {
        bool threat = false;
        bool potion_used = false;
        bool working = false;
        Thread bot;

        public bool use_potion = false;
        public bool toAbolishPoison = false;
        public bool toDrink = false;
        public bool toAOEheal = false;
        public bool tank_heal_priority = true;
        int delay;

        int DispellDebuff;
        private StreamWriter sw;

        Dictionary<string, WowForPartyMemberHealerCD> party_data;

        public HealBot(Dictionary<string, WowForPartyMemberHealerCD> party_data, int delay)
        {
            this.party_data = party_data;
            this.delay = delay;
        }

        class HealerBuffs
        {
            public static int
                Drink = 34291;
        }

        WoWMemReader wmr;
        KeyToWindowSender sk;
        WowHealerCD self = new WowHealerCD();

        public void Start()
        {
            if (working != true)
            {
                working = true;
                sw= new StreamWriter("HealLog.txt");
                sw.WriteLine("HealBotStarted");
                Init();
                party_data.Add("Zorn", new WowForPartyMemberHealerCD());
                bot = new Thread(Do);
                bot.IsBackground = true;
                bot.Start();
            }
            else throw new Exception("Already working Exception!");
        }

        public void Close()
        {
            if (working != false)
            {
                working = false;
                sw.WriteLine("HealBotStopped");
                sw.Close();
                party_data.Remove("Zorn");
                bot.Join();
            }
            else throw new Exception("Not working Exception!");
        }

        void Init()
        {
            Process[] Parr = Process.GetProcessesByName("Wow");

            uint ID = 0;
            Process proc = null;

            foreach (Process item in Parr)
            {
                WoWMemReader WMRR = new WoWMemReader((uint)item.Id);
                if (WMRR.WoWProcessHasLocalPlayerName("Zorn"))
                {
                    ID = (uint)item.Id;
                    proc = item;
                    break;
                }
            }
            sk = new KeyToWindowSender(proc);
            wmr = new WoWMemReader(ID);
        }

        bool under_threat()
        {
            if (self.under_attack) return true;

            foreach (var item in party_data)
                if (item.Value.under_attack == true)
                    return true;
            return false;
        }

        public void Do()
        {
            WowForPartyMemberHealerCD tmp_player=null;

            while (working)
            {
                wmr.GetPlayer(self);
                CopyData(self,party_data["Zorn"]);

                if (WowForPartyMemberHealerCD.G == 0 && !HealerHasBuff(HealerBuffs.Drink))
                {
                    threat = under_threat();
                    if (self.hp != 0)
                        if (!(toAOEheal && AOE_Heal()))
                            if (!SelfHeal())
                                if(!PartyCleance())
                            {
                                if (tank_heal_priority)
                                {
                                    foreach (string item in PartyToHeal.Players)
                                    {
                                        if (party_data.TryGetValue(item,out tmp_player))
                                            if(PartyHeal(tmp_player))
                                                 break;
                                    }
                                }
                                else
                                {
                                    if(GetMinHpPlayer(out tmp_player))
                                        PartyHeal(tmp_player);            
                                }
                            }
                }

                foreach (var item in party_data)
                    item.Value.CDdec();

                self.CDdec();
                WowForPartyMemberHealerCD.GCDdec();

                DispellDebuff = DispellDebuff - delay;
                if (DispellDebuff < 0) DispellDebuff = 0;

                Thread.Sleep(delay);
            }
        }

        bool GetMinHpPlayer(out WowForPartyMemberHealerCD player_data)
        {
            int min_percent_hp=100;
            WowForPartyMemberHealerCD min_percent_hp_player_data= null;
            WowForPartyMemberHealerCD tmp = null;
            bool has_player = false;
            int tmp_percent_hp=0;

            foreach (string item in PartyToHeal.Players)
            {
                if (party_data.TryGetValue(item, out tmp) && tmp.max_hp!=0)
                {
                    tmp_percent_hp = (int)(tmp.hp * 100 / tmp.max_hp);

                    if (tmp_percent_hp < min_percent_hp)
                    {
                        min_percent_hp = tmp_percent_hp;
                        min_percent_hp_player_data = tmp;
                    }     
                }
                else
                    continue;
            }

            if(min_percent_hp<100)
                has_player = true;

            player_data = min_percent_hp_player_data;

            return has_player;
        }

        public bool HealerHasBuff(int buff_id)
        {
            for (int i = 0; i < self.buff_arr.Length; i++)
            {
                if (self.buff_arr[i] == buff_id) return true;
            }

            return false;
        }

        Keys KeyChoice(int num)
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

        bool PartyCleance()
        {
            WowForPartyMemberHealerCD obj = null;

            if (toAbolishPoison)
            foreach (string item in PartyToHeal.Players)
            {
                if (party_data.TryGetValue(item, out obj) && obj!=null)
                {
                    int ID = GetPlayerObjID(obj);

                    if (toPurify(PoisonToDispellArray, obj.buff_arr) && obj.AbolishPoison == 0 &&
                        DispellDebuff == 0)
                    {
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                        sk.SendKey(KeyChoice(ID + 3));
                        sk.SendKey(Keys.D9);
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);

                        DispellDebuff = 2000;
                        obj.SetAbolishPoison();
                        self.SetGCD(1500, true);

                        sw.WriteLine("AbolishPoison " + Convert.ToString(ID));
                        sw.Flush();

                        return true;
                    }
                }
            }
            return false;
        }

        bool PartyHeal(WowForPartyMemberHealerCD obj)
        {
            int ID = GetPlayerObjID(obj);

            if (obj.hp > 0)
            {
                if (threat)
                {
                    if (obj.hp <= obj.max_hp * 0.95 && obj.hp > obj.max_hp * 0.33 && obj.HoT == 0)
                    {
                        obj.SetHoT();
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);    // SendKeys.Send("+{3}");
                        sk.SendKey(KeyChoice(ID + 3));              //SendKeys.Send(Convert.ToString(ID + 3));
                        sk.SendKey(Keys.D2);                        //SendKeys.Send("2");
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);    //SendKeys.Send("+{2}");
                        return true;
                    }

                    if (obj.hp <= obj.max_hp * 0.33 && self.NaturesSwiftness == 0)
                    {
                        sk.SendKey(Keys.D4);
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);    // SendKeys.Send("+{3}");
                        sk.SendKey(KeyChoice(ID + 3));
                        sk.SendKey(Keys.D1);
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);    // SendKeys.Send("+{2}");

                        self.SetRegrowth();
                        self.SetNaturesSwiftness();
                        self.SetGCD(1500,true);
                        return true;
                    }

                    if ((obj.hp <= obj.max_hp * 0.85) && obj.Regrowth == 0)
                    {
                        obj.SetRegrowth();
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);    // SendKeys.Send("+{3}");
                        sk.SendKey(KeyChoice(ID + 3));              // SendKeys.Send(Convert.ToString(ID + 3));
                        sk.SendKey(Keys.D1);                        // SendKeys.Send("1");
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);    //SendKeys.Send("+{2}"); 
                        return true;
                    }

                    if (obj.name=="Йогсоттот" && (obj.hp <= obj.max_hp * 0.90) && obj.Lifebloom < 1000)
                    {
                        obj.SetLifebloom();
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);  
                        sk.SendKey(Keys.D3);            
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);    
                        sk.SendKey(Keys.D6);
                        return true;
                    }

                    if ((obj.hp <= obj.max_hp * 0.66) && self.SwiftMend == 0 && (obj.Regrowth > 0 || obj.HoT > 0))
                    {
                        if (obj.HoT > obj.Regrowth)
                            obj.HoT = 0;
                        else
                            obj.Regrowth = 0;

                        self.SetSwiftMend();

                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);    // SendKeys.Send("+{3}");
                        sk.SendKey(KeyChoice(ID + 3));              // SendKeys.Send(Convert.ToString(ID + 3));
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                        sk.SendKey(Keys.D3);
                        return true;
                    }

                    if (obj.hp <= obj.max_hp * 0.83)
                    {
                        self.SetHeal();
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);    //  SendKeys.Send("+{3}");
                        sk.SendKey(KeyChoice(ID + 3));              //SendKeys.Send(Convert.ToString(ID + 3));
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);    // SendKeys.Send("+{2}");
                        sk.SendKey(Keys.D2);                        //SendKeys.Send("2");
                        return true;
                    }
                }
                else
                {
                    if (obj.hp <= obj.max_hp * 0.95 && obj.HoT == 0)
                    {
                        obj.SetHoT();
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);    // SendKeys.Send("+{3}");
                        sk.SendKey(KeyChoice(ID + 3));              //SendKeys.Send(Convert.ToString(ID + 3));
                        sk.SendKey(Keys.D2);                        // SendKeys.Send("2");
                        sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);    //SendKeys.Send("+{2}");
                        return true;
                    }
                }
            }
            return false;
        }

        int GetPlayerObjID(WowForPartyMemberHealerCD obj)
        {
            for (int i = 0; i < PartyToHeal.Players.Length; i++)
                if (PartyToHeal.Players[i] == obj.name)
                    return i;
            return -1;
        }

        void KeyBar3Combo(Keys key1, Keys key2)
        {
            sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);//+{3}
            sk.SendTwoKeys(key1, key2);
            sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);//+{2}
        }

        bool SelfHeal()
        {
            if (self.hp > 0)
            {
                if (self.under_attack == false)
                    potion_used = false;

                if (toAbolishPoison && toPurify(PoisonToDispellArray, self.buff_arr) && self.AbolishPoison == 0 && DispellDebuff == 0)
                {
                    sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                    sk.SendTwoKeys(Keys.LMenu, Keys.D9);
                    sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);

                    DispellDebuff = 2000;
                    self.SetAbolishPoison();
                    self.SetGCD(1500, true);

                    sw.WriteLine("AbolishPoison Self");

                    return true;
                }

                if (threat)
                {
                    if (self.hp <= self.max_hp * 0.5 && self.BarkSkin == 0)
                    {
                        self.SetBarkSkin();
                        sk.SendKey(Keys.D5);
                        return true;
                    }

                    if (self.hp <= self.max_hp * 0.5 && self.LifeBlood == 0)
                    {
                        self.SetLifeBlood();
                        sk.SendKey(Keys.Oemplus);
                        return true;
                    }

                    if (self.hp <= self.max_hp * 0.75 && self.Regrowth == 0)
                    {
                        KeyBar3Combo(Keys.LMenu, Keys.D1);//%{2}
                        self.SetRegrowth();
                        self.SetGCD(2000, true);
                        return true;
                    }

                    if (self.hp <= self.max_hp * 0.95 && self.HoT == 0)
                    {               
                        KeyBar3Combo(Keys.LMenu, Keys.D2);//%{2}
                        self.SetHoT();
                        return true;
                    }

                    if (self.hp <= self.max_hp * 0.5)
                    {
                        sk.SendTwoKeys(Keys.LMenu,Keys.D2);
                        self.SetGCD(1500,true);
                        return true;
                    }

                    if (self.mp <= self.max_mp * 0.25 && self.Innervate == 0)
                    {
                        self.SetInnervate();
                        sk.SendKey(Keys.OemMinus);
                        return true;
                    }

                    if (self.mp <= self.max_mp * 0.2 && use_potion == true && !potion_used)
                    {
                        potion_used = true;
                        self.SetG_ping();
                        sk.SendKey(Keys.D9);
                        return true;
                    }
                }
                else
                {
                    if (self.hp <= self.max_hp * 0.75 && self.HoT == 0)
                    {
                        self.SetHoT();
                        KeyBar3Combo(Keys.LMenu, Keys.D2);//%{2}
                        return true;
                    }

                    if (toDrink && self.mp <= self.max_mp * 0.8 && !HealerHasBuff(HealerBuffs.Drink))
                    {
                        sk.SendKey(Keys.D0);//0
                        return true;
                    }
                }
            }

            return false;
        }

        bool AOE_Heal()
        {
            int party_hp = 0;
            int party_max_hp = 0;

            int top_hp = (int)party_data.Values.Max(p => p.max_hp);

            foreach (WoWLivingObj item in party_data.Values)
            {
                if (item.hp > 0 && item.max_hp != top_hp)
                {
                    party_hp += (int)item.hp;
                    party_max_hp += (int)item.max_hp;
                }
            }

            if (party_hp < party_max_hp * 0.55 && threat)
            {
                sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                sk.SendKey(Keys.D1);
                self.SetTranquility();
                return true;
            }

            if (party_hp < party_max_hp * 0.90 && threat && self.WildGrowth == 0)
            {
                sk.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                sk.SendKey(Keys.D8);
                sk.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                self.SetWildGrowth();
                return true;
            }



            return false;
        }

        void CopyData(WoWLivingObj inp, WowForPartyMemberHealerCD outpt)
        {
            outpt.buff_arr = inp.buff_arr;
            outpt.name = inp.name;
            outpt.mp = inp.mp;
            outpt.max_mp = inp.max_mp;
            outpt.hp = inp.hp;
            outpt.max_hp = inp.max_hp;
            outpt.under_attack = inp.under_attack;
            outpt.type = inp.type;
            outpt.GUID = inp.GUID;
        }
    }
}
