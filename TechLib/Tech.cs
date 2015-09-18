using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

namespace TechLib
{
    public class PartyToHeal
    {
        static PartyToHeal()
        {
            Players = new string[] { "Йогсоттот", "ShoggotPet", "Lichinkin", "Format", "Shoggot" };
        }
        public static string[] Players;
    }

    public class Party
    {
        static Party()
        {
            Players = new string[] { "Йогсоттот", "ShoggotPet", "Lichinkin", "Format", "Shoggot","Zorn" };
        }
        public static string[] Players;
    }

    [Serializable]
    public class WoWLivingObj
    {
        public string name;
        public volatile uint hp;
        public volatile uint max_hp;
        public volatile uint mp;
        public volatile uint max_mp;
        public uint type;
        public volatile bool under_attack;
        public ulong GUID;
        public volatile int[] buff_arr = new int[24];
    }

    [Serializable]
    public class AssaultBotNameMsg
    {
        public string name;
    }  // 200 байт

    public enum Request { DoNothing, do1, do2, do3, do4, do5, do6, do7, do8, do9 }

    [Serializable]
    public class toAssaultBotMsg
    {
        public bool under_attack;
        public int player_id;
        public Request to_do_code = Request.DoNothing;
    } //234 байта

    public class WowForPartyMemberHealerCD : WoWLivingObj
    {
        public string Name { get { return name; } }
        public uint Hp { get { return hp; } }
        public uint Max_hp { get { return max_hp; } }
        public uint Mp { get { return mp; } }
        public uint Max_mp { get { return max_mp; } }
        [Browsable(false)]
        public uint Type { get { return type; } }
        public bool Under_attack { get { return under_attack; } }

        public static int ping = 400;
        public static int tick = 200;

        static int g = 0;
        protected int gcd = 1500;

        protected int[] arr = new int[16];

        [Browsable(false)]
        public static int G
        {
            get
            {
                return g;
            }
            set
            {
                g = value;
            }
        }
        [Browsable(false)]
        public int HoT
        {
            get
            {
                return arr[0];
            }
            set
            {
                arr[0] = value;
            }
        }
        [Browsable(false)]
        public int Regrowth
        {
            get
            {
                return arr[4];
            }
            set
            {
                arr[4] = value;
            }
        }
        [Browsable(false)]
        public int Shield
        {
            get
            {
                return arr[1];
            }
            set
            {
                arr[1] = value;
            }
        }
        [Browsable(false)]
        public int AbolishDisease
        {
            get
            {
                return arr[10];
            }
            set
            {
                arr[10] = value;
            }
        }
        [Browsable(false)]
        public int AbolishPoison
        {
            get
            {
                return arr[14];
            }
            set
            {
                arr[14] = value;
            }
        }
        [Browsable(false)]
        public int Lifebloom
        {
            get
            {
                return arr[15];
            }
            set
            {
                arr[15] = value;
            }
        }

        public void CDdec()
        {
            int tmp_time;
            for (int i = 0; i < arr.Length; i++)
            {
                if(i!=10 && i!=1) // 10 1 work for assault assist server
                {
                tmp_time = arr[i] - tick;
                if (tmp_time > 0)
                    arr[i] -= tick;
                else
                    arr[i] = 0;
                }
            }
        }
        public static void GCDdec()
        {
            int tmp = G - tick;
            if (tmp < 0)
                G = 0;
            else
                G = tmp;
        }
        /////////////////////////////////////////////////
        public void SetRegrowth()
        {
            Regrowth = 21000 + ping;
            //G = gcd + ping;
        }
        public void SetHoT()
        {
            HoT = 15000 + ping;
            //G = gcd + ping;
        }
        public void SetShield()
        {
            Shield = 15000 + ping;
        }
        public void SetAbolishDisease()
        {
            AbolishDisease = 12000 + ping;
        }
        public void SetAbolishPoison()
        {
            AbolishPoison = 12000 + ping;
        }
        public void SetLifebloom()
        {
            Lifebloom = 8000 + ping;
        }

        public void SetG_ping()
        {
            G = ping;
        }
    }

    public class WowHealerCD : WowForPartyMemberHealerCD
    {
        public int Innervate
        {
            get
            {
                return arr[2];
            }
            set
            {
                arr[2] = value;
            }
        }
        public int Tranquility
        {
            get
            {
                return arr[3];
            }
            set
            {
                arr[3] = value;
            }
        }
        public int BarkSkin
        {
            get
            {
                return arr[6];
            }
            set
            {
                arr[6] = value;
            }
        }

        public int NaturesSwiftness
        {
            get
            {
                return arr[11];
            }
            set
            {
                arr[11] = value;
            }
        }
        public int LifeBlood
        {
            get
            {
                return arr[8];
            }
            set
            {
                arr[8] = value;
            }
        }
        public int Heal
        {
            get
            {
                return arr[9];
            }
            set
            {
                arr[9] = value;
            }
        }
        public int SwiftMend
        {
            get
            {
                return arr[12];
            }
            set
            {
                arr[12] = value;
            }
        }
        public int WildGrowth
        {
            get
            {
                return arr[13];
            }
            set
            {
                arr[13] = value;
            }
        }

        public void SetWildGrowth()
        {
            WildGrowth = 6000 + ping;
            G = 1500 + ping;
        }
        public void SetTranquility()
        {
            Tranquility = 32 * 6000 + ping;
            G = 8000 + ping;
        }
        public void SetHeal()
        {
            Heal = 0 + ping;
            G = gcd + ping;
        }
        public void SetSwiftMend()
        {
            SwiftMend = 15000 + ping;
            G = 1500 + ping;
        }
        public void SetBarkSkin()
        {
            BarkSkin = 60000 + ping;
            G = gcd + ping;
        }
        public void SetInnervate()
        {
            Innervate = 180000 + ping;
            G = ping;
        }
        public void SetLifeBlood()
        {
            LifeBlood = 180000 + ping;
            G = ping;
        }
        public void SetNaturesSwiftness()
        {
            NaturesSwiftness = 180000 + ping;
            G = ping;
        }
   /*     public new void CDdec()
        {
            int tmp_time;
            for (int i = 0; i < arr.Length; i++)
            {
                tmp_time = arr[i] - tick;
                if (tmp_time > 0)
                    arr[i] -= tick;
                else
                    arr[i] = 0;
            }
        }*/
        public void SetGCD(int m_secs, bool has_ping)
        {
            if (has_ping)
                G = m_secs + ping;
            else
                G = m_secs;
        }
    }

    public struct VolatileInt
    {
        public volatile int perem;
    }

    public class VolatileBool
    {
        public volatile bool value;
    }

    public class WoWMemReader
    {
        public WoWMemReader(uint ProcID)
        {
            this.ProcID = ProcID;
            ProcOpen();
        }

        ~WoWMemReader()
        {
            ProcClose();
        }

        #region API

        class ProcAccess
        {
            public static uint PROCESS_TERMINATE = (0x0001),
            PROCESS_CREATE_THREAD = (0x0002),
            PROCESS_SET_SESSIONID = (0x0004),
            PROCESS_VM_OPERATION = (0x0008),
            PROCESS_VM_READ = (0x0010),
            PROCESS_VM_WRITE = (0x0020),
            PROCESS_DUP_HANDLE = (0x0040),
            PROCESS_CREATE_PROCESS = (0x0080),
            PROCESS_SET_QUOTA = (0x0100),
            PROCESS_SET_INFORMATION = (0x0200),
            PROCESS_QUERY_INFORMATION = (0x0400);
        }

        // function declarations are found in the MSDN and in <winbase.h> 

        //		HANDLE OpenProcess(
        //			DWORD dwDesiredAccess,  // access flag
        //			BOOL bInheritHandle,    // handle inheritance option
        //			DWORD dwProcessId       // process identifier
        //			);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwProcessId);

        //		BOOL CloseHandle(
        //			HANDLE hObject   // handle to object
        //			);
        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hObject);

        //		BOOL ReadProcessMemory(
        //			HANDLE hProcess,              // handle to the process
        //			LPCVOID lpBaseAddress,        // base of memory area
        //			LPVOID lpBuffer,              // data buffer
        //			SIZE_T nSize,                 // number of bytes to read
        //			SIZE_T * lpNumberOfBytesRead  // number of bytes read
        //			);
        [DllImport("kernel32.dll")]
        public static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);

        //		BOOL WriteProcessMemory(
        //			HANDLE hProcess,                // handle to process
        //			LPVOID lpBaseAddress,           // base of memory area
        //			LPCVOID lpBuffer,               // data buffer
        //			SIZE_T nSize,                   // count of bytes to write
        //			SIZE_T * lpNumberOfBytesWritten // count of bytes written
        //			);
        [DllImport("kernel32.dll")]
        public static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesWritten);

        #endregion

        #region Offsets
        public class ObjType
        {
            public static uint Item = 1,
            Contains = 2,
            NPC = 3,
            Player = 4,
            GameObjects = 5,  // nodes etc
            DynamicObjects = 6, //spells etc
            Corpses = 7;
        }

        class ClientOffsets
        {
            public static uint WoWbase = 0x0400000,
            StaticClientConnection = 0x00C79CE0,
            ObjectManagerOffset = 0x2ED0,
            FirstObjectOffset = 0xAC,
            LocalPlayerGuidOffset = 0xC0,
            NextObjectOffset = 0x3C,
            LocalPlayerGUID = 0xBD07A8,
            LocalTargetGUID = 0x00BD07B0,
            CurrentContinent = 0x00ACCF04,

            LocalPlayerName = 0x00C79D18,//string 12
            CurrentLocalPlayerTargetGUID = 0x00BD07B0,
            LocalPlayerPreviousTargetGUID = 0x00BD07B8;
        }

        class ObjectOffsets
        {
            public static uint Type = 0x14,
            Pos_X = 0x79C,
            Pos_Y = 0x798,
            Pos_Z = 0x7A0,
            Rot = 0x7A8,
            Guid = 0x30,
            UnitFields = 0x8,
            Node_Pos_X = 0xEC,
            Node_Pos_Y = 0xE8,
            Node_Pos_Z = 0xF0;
        }

        class UnitOffsets
        {
            public static uint Level = 0x36 * 4,
            Health = 0x18 * 4,
            Energy = 0x19 * 4,
            MaxHealth = 0x20 * 4,
            SummonedBy = 0xE * 4,
            MaxEnergy = 0x21 * 4,
            Threat = 0xec;
        }

        class MineNodes
        {
            public static uint Copper = 310,
            Tin = 315,
            Incendicite = 384,
            Silver = 314,
            Iron = 312,
            Indurium = 384,
            Gold = 311,
            LesserBloodstone = 48,
            Mithril = 313,
            Truesilver = 314,
            DarkIron = 2571,
            SmallThorium = 3951,
            RichThorium = 3952,
            ObsidianChunk = 6650,
            FelIron = 6799,
            Adamantite = 6798,
            Cobalt = 7881,
            Nethercite = 6650,
            Khorium = 6800,
            Saronite = 7804,
            Titanium = 6798;
        }

        class HerbNodes
        {
            public static uint Peacebloom = 269,
            Silverleaf = 270,
            Earthroot = 414,
            Mageroyal = 268,
            Briarthorn = 271,
            Stranglekelp = 700,
            Bruiseweed = 358,
            WildSteelbloom = 371,
            GraveMoss = 357,
            Kingsblood = 320,
            Liferoot = 677,
            Fadeleaf = 697,
            Goldthorn = 698,
            KhadgarsWhisker = 701,
            Wintersbite = 699,
            Firebloom = 2312,
            PurpleLotus = 2314,
            ArthasTears = 2310,
            Sungrass = 2315,
            Blindweed = 2311,
            GhostMushroom = 389,
            Gromsblood = 2313,
            GoldenSansam = 4652,
            Dreamfoil = 4635,
            MountainSilversage = 4633,
            Plaguebloom = 4632,
            Icecap = 4634,
            BlackLotus = 4636,
            Felweed = 6968,
            DreamingGlory = 6948,
            Terocone = 6969,
            Ragveil = 6949,
            FlameCap = 6966,
            AncientLichen = 6967,
            Netherbloom = 6947,
            NightmareVine = 6946,
            ManaThistle = 6945,
            TalandrasRose = 7865,
            Goldclover = 7844,
            AddersTongue = 8084;
        }

        class NameOffsets
        {
            public static ulong nameStore = 0x00C5D938 + 0x8,
            nameMask = 0x24,
            nameBase = 0x1C,
            nameString = 0x20;
        }

        class BuffsOffsets
        {
            public static uint aura_spell_guid = 0x0,
            aura_spell_duration = 0x10,
            aura_spell_endtime = 0x14,
            aura_spell_id = 0x8,
            aura_spell_size = 0x18,

            CGUnit_aura = 0x556E10,
            aura_count_1 = 0xDD0,
            aura_count_2 = 0xC54,
            aura_table_1 = 0xC50,
            aura_table_2 = 0xC58;
        }

        class WoWGameObjectType
        {
            public static uint Door = 0,
            Button = 1,
            QuestGiver = 2,
            Chest = 3,
            Binder = 4,
            Generic = 5,
            Trap = 6,
            Chair = 7,
            SpellFocus = 8,
            Text = 9,
            Goober = 0xa,
            Transport = 0xb,
            AreaDamage = 0xc,
            Camera = 0xd,
            WorldObj = 0xe,
            MapObjTransport = 0xf,
            DuelArbiter = 0x10,
            FishingNode = 0x11,
            Ritual = 0x12,
            Mailbox = 0x13,
            AuctionHouse = 0x14,
            SpellCaster = 0x16,
            MeetingStone = 0x17,
            Unkown18 = 0x18,
            FishingPool = 0x19,
            FORCEDWORD = 0xFFFFFFFF;
        }
        #endregion

        #region Work_with_process

        void ProcOpen()
        {
            ProcPointer = OpenProcess(ProcAccess.PROCESS_VM_READ, 1, ProcID);
            ProcOpened = true;// пока заглушка без проверки
        }

        void ProcClose()
        {
            int Ret;
            Ret = CloseHandle(ProcPointer);
            if (Ret == 0)
            {
                //ошибка закрытия 
            }
            else
                ProcOpened = false;
        }

        byte[] ReadProcessMemory(IntPtr MemoryAddress, uint bytesToRead)
        {
            IntPtr a;
            byte[] buffer = new byte[bytesToRead];
            ReadProcessMemory(ProcPointer, MemoryAddress, buffer, bytesToRead, out a);
            return buffer;
        }

        uint ReadMemUint(uint adress)
        {
            return BitConverter.ToUInt32(ReadProcessMemory(new IntPtr(adress), 4), 0);
        }

        int ReadMemInt(uint adress)
        {
            return BitConverter.ToInt32(ReadProcessMemory(new IntPtr(adress), 4), 0);
        }

        ulong ReadMemUlong(uint adress)
        {
            return BitConverter.ToUInt64(ReadProcessMemory(new IntPtr(adress), 8), 0);
        }

        string ReadMemText(uint adress, uint length)
        {
            byte[] arr = ReadProcessMemory(new IntPtr(adress), length);
            int i = 0;
            string s = "";

            for (int j = 0; j < arr.Length; j++)
                if (arr[j] == '\0')
                    if (j <= 11)
                    {
                        while (arr[i] != '\0')
                        {
                            s += Convert.ToChar(arr[i]);
                            i++;
                        }
                    }
                    else
                    {
                        s = Encoding.UTF8.GetString(arr, 0, arr.Length);
                        s = s.Split('\0')[0];
                    }
            return s;

        }

        #endregion

        uint ProcID;
        IntPtr ProcPointer;
        bool ProcOpened = false;

        uint FirstObjPointer
        {
            get
            {
                uint first_pointer = ReadMemUint(ClientOffsets.StaticClientConnection);
                uint second_pointer = ReadMemUint(first_pointer + ClientOffsets.ObjectManagerOffset);
                return ReadMemUint(second_pointer + ClientOffsets.FirstObjectOffset);
            }
        }

        uint ObjManager
        {
            get
            {
                uint first_pointer = ReadMemUint(ClientOffsets.StaticClientConnection);
                return ReadMemUint(first_pointer + ClientOffsets.ObjectManagerOffset);
            }
        }

        ulong LocalPlayerTargetGuid
        {
            get
            {
                if (ProcOpened)
                    return ReadMemUlong(ClientOffsets.CurrentLocalPlayerTargetGUID);
                else
                    return 0;
            }
        }

        ulong LocalPlayerPreviousTargetGuid
        {
            get
            {
                if (ProcOpened)
                    return ReadMemUlong(ClientOffsets.LocalPlayerPreviousTargetGUID);
                else
                    return 0;
            }
        }

        public ulong LocalPlayerGuid
        {
            get
            {
                if (ProcOpened)
                    return ReadMemUlong(ClientOffsets.LocalPlayerGuidOffset + ObjManager);
                else
                    return 0;
            }
        }

        public string LocalPlayerName
        {
            get
            {
                if (ProcOpened)
                {
                    return ReadMemText(ClientOffsets.LocalPlayerName, 24);
                }
                else
                    return "";
            }
        }

        uint FindObjBaseAdrByGUID(ulong GUID)
        {
            uint adress = FirstObjPointer;

            while (adress != 0 && adress % 2 == 0)
            {
                if (GUID == ReadMemUlong(adress + ObjectOffsets.Guid))
                    return adress;

                adress = NextObjAdr(adress);
            }

            return 0;
        } // 0 - нет объекта

        public uint GetPlayerBaseAdress()
        {
            return FindObjBaseAdrByGUID(LocalPlayerGuid);
        }

        public uint GetPlayerBaseBuffAdressTable1()
        {
            return FindObjBaseAdrByGUID(LocalPlayerGuid) + BuffsOffsets.aura_count_1 + BuffsOffsets.aura_spell_id;
        }

        public uint GetPlayerBaseBuffAdressTable2()
        {
            return FindObjBaseAdrByGUID(LocalPlayerGuid) + BuffsOffsets.aura_count_2 + BuffsOffsets.aura_spell_id;
        }

        uint FindObjSummonedNonPlayerByGUID(ulong GUID)
        {
            uint adress = FirstObjPointer;
            uint unit_fields_adr;

            while (adress != 0 && adress % 2 == 0)
            {
                unit_fields_adr = ReadMemUint(adress + ObjectOffsets.UnitFields);

                if (GUID == ReadMemUlong(unit_fields_adr + UnitOffsets.SummonedBy))
                    return adress;

                adress = NextObjAdr(adress);
            }
            return 0;
        }

        string GetNonPlayerName(uint adress)
        {
            uint adr1 = ReadMemUint(adress + 0x964);
            return ReadMemText(adr1 + 0x05C, 12);
        }

        uint NextObjAdr(uint adress)
        {
            return ReadMemUint(adress + ClientOffsets.NextObjectOffset);
        }

        bool UnderThreat(uint adress)
        {
            uint adr = ReadMemUint(adress + UnitOffsets.Threat);
            return 0x08 != adr;
        }

        void GetObjMainParams(uint adress, WoWLivingObj obj)
        {
            uint type = ReadMemUint(adress + ObjectOffsets.Type);

            uint unit_fields_adr = ReadMemUint(adress + ObjectOffsets.UnitFields);

            if (type != ObjType.Player)//не человек
            {
                //obj.name = GetNonPlayerName(adress);
                obj.name = LocalPlayerName + "Pet";
            }
            else
            {
                obj.name = LocalPlayerName;
            }

            obj.mp = ReadMemUint(unit_fields_adr + UnitOffsets.Energy);
            obj.max_mp = ReadMemUint(unit_fields_adr + UnitOffsets.MaxEnergy);

            obj.hp = ReadMemUint(unit_fields_adr + UnitOffsets.Health);
            obj.max_hp = ReadMemUint(unit_fields_adr + UnitOffsets.MaxHealth);
            obj.type = type;
            obj.under_attack = UnderThreat(unit_fields_adr);

            GetBuffs(adress, obj.buff_arr);
        }

        public void GetLocalPlayerTarget(WoWLivingObj obj)
        {
            uint adr = FindObjBaseAdrByGUID(LocalPlayerTargetGuid);
            GetObjMainParams(adr, obj);
            obj.GUID = LocalPlayerTargetGuid;
        }

        public int[] GetLocalPlayerTargetBuffs()
        {
            int[] arr = new int[24];
            GetBuffs(FindObjBaseAdrByGUID(LocalPlayerTargetGuid), arr);
            return arr;
        }

        public void GetPlayer(WoWLivingObj obj)
        {
            uint adr = FindObjBaseAdrByGUID(LocalPlayerGuid);
            GetObjMainParams(adr, obj);
        }

        public void GetPet(WoWLivingObj obj)
        {
            uint adr = FindObjSummonedNonPlayerByGUID(LocalPlayerGuid);
            GetObjMainParams(adr, obj);
        }

        public bool WoWProcessHasLocalPlayerName(string name)
        {
            if (name == LocalPlayerName) return true;
            return false;
        }

        void GetBuffs(uint adress, int[] buff_arr)
        {
            int BuffCount = ReadMemInt(adress + BuffsOffsets.aura_count_1);
            uint table = adress + BuffsOffsets.aura_table_1;
            int[] tmp_arr;

            if (BuffCount == -1)
            {
            //    BuffCount = ReadMemInt(adress + BuffsOffsets.aura_count_2);
                table = adress + BuffsOffsets.aura_table_2;
                BuffCount = 24;
            }

            tmp_arr = new int[BuffCount];

            for (uint i = 0; i < BuffCount; i++)
            {
                if(BuffCount!=24)
                    tmp_arr[i] = ReadMemInt(table + BuffsOffsets.aura_spell_size * i + BuffsOffsets.aura_spell_id);
                else
                {
                    tmp_arr[i] = ReadMemInt((uint)ReadMemInt(table) + BuffsOffsets.aura_spell_size * i + BuffsOffsets.aura_spell_id);
                }
            }

            tmp_arr = tmp_arr.Where(p => p != 0).ToArray();

            for (int i = 0; i < buff_arr.Length; i++)
                buff_arr[i] = 0;

            for (int i = 0; i < tmp_arr.Length; i++)
            {
                buff_arr[i] = tmp_arr[i];
            }
        }

        void GetDebuffs(uint adress, int[] buff_arr)
        {
            uint max_debuffs = 16;
            uint debuff_offset = 0x160;
            uint debuff_size = 0x40;
            int tmp;

            for (int i = 0; i < buff_arr.Length; i++)
                buff_arr[i] = 0;

            for (uint i = 0; i < max_debuffs; i++)
            {
                if ((tmp = ReadMemInt(adress + debuff_offset + debuff_size * i)) != 0)
                    buff_arr[i] = tmp;
                //  else
                //  break;
            }
        }
    }

    public class Converter
    {
        public static void CodeAndSend<T>(Socket sender, T obj, ref bool connection_lost, int buffer_size)
        {
            byte[] buffer = new byte[buffer_size];
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, obj);
            byte[] tm_arr = ms.ToArray();
            ms.Close();

            for (int i = 0; i < tm_arr.Length; i++)
            {
                buffer[i] = tm_arr[i];
            }

            try
            {
                sender.Send(buffer);
            }
            catch
            {
                connection_lost = true;
            }
        }

        public static dynamic RecieveAndDecode<T>(Socket reciever, ref bool end_of_transmition, ref int bad_packets_num, int buffer_size)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            T obj = default(T);

            byte[] buffer = new byte[buffer_size];
            end_of_transmition = true;

            if (reciever.Receive(buffer) > 0)
            {
                ms.Write(buffer, 0, buffer_size);
                ms.Seek(0, SeekOrigin.Begin);

                end_of_transmition = false;

                try
                {
                    obj = (T)bf.Deserialize(ms);
                }
                catch
                {
                    bad_packets_num++;
                }
            }
            ms.Close();

            return obj;
        }
    }

    public class KeyToWindowSender
    {
        int WM_SYSKEYDOWN = 0x0104;
        int WM_SYSKEYUP = 0x0105;
        //int WM_CHAR = 0x0102;

        [DllImport("User32.dll")]
        private static extern IntPtr PostMessage(int hWnd, int Msg, int wParam, int lParam);
        [DllImport("User32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public int WowWindowHndl;

        public KeyToWindowSender(Process procHNDL)
        {
            WowWindowHndl = (int)procHNDL.MainWindowHandle;
        }

        public static int GetForegroundWindowHandle()
        {
            return (int)GetForegroundWindow();
        }

        public void SendKey(Keys key)
        {
            PostMessage(WowWindowHndl, WM_SYSKEYDOWN, (int)key, 1);
            Thread.Sleep(10);
            PostMessage(WowWindowHndl, WM_SYSKEYUP, (int)key, 1);
            Thread.Sleep(10);

        }

        public void SendTwoKeys(Keys key1, Keys key2)
        {
            PostMessage(WowWindowHndl, WM_SYSKEYDOWN, (int)key1, 1);
            PostMessage(WowWindowHndl, WM_SYSKEYDOWN, (int)key2, 1);
            Thread.Sleep(10);
            PostMessage(WowWindowHndl, WM_SYSKEYUP, (int)key1, 1);
            PostMessage(WowWindowHndl, WM_SYSKEYUP, (int)key2, 1);
            Thread.Sleep(10);
        }
    }
}
