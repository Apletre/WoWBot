using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using TechLib;
using PartyDataRetrivalLib;

namespace WoWClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        bool to_find_debuff = false;

        GetPlayerStat[] player=new GetPlayerStat[0];

        private void button1_Click(object sender, EventArgs e)
        {
            Process[] parr = Process.GetProcessesByName("WoW");
            
            player=new GetPlayerStat[parr.Length];

            for (int i = 0; i < parr.Length; i++)
            {
                player[i] = new GetPlayerStat(new WoWMemReader((uint)parr[i].Id));
            }

            dataGridView1.DataSource = player;

            int delay = 200;
            timer1.Interval = delay;
            timer1.Start();

            button2.Enabled = true;
            button1.Enabled = false;
        }
      
        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Stop();

            for (int i = 0; i < player.Length; i++)
            {
                player[i].Close();
            }     

            button2.Enabled = false;
            button1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < player.Length; i++)
            {
                player[i].Do();
            }

            dataGridView2.RowCount = player.Length;
            dataGridView2.ColumnCount = 24;

            for (int j = 0; j < dataGridView2.RowCount; j++)
            {
                int[] array = player[j].GetPlayerBuffs();

                if (to_find_debuff)
                    find_debuff_msg(array);

                for (int i = 0; i < dataGridView2.ColumnCount; i++)
                {
                    dataGridView2[i, j].Value = array[i];
                }
            }
        }

        void find_debuff_msg(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == Convert.ToInt32(textBox1.Text))
                {
                    new Thread(ShowMsg).Start();
                    to_find_debuff = false;
                    button3.Enabled = true;
                    textBox1.Enabled = true;
                    button4.Enabled = false;
                }
            }
        }

        void ShowMsg()
        {
            MessageBox.Show("Дебаф найден!");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
            for (int i = 0; i < player.Length; i++)
            {
                player[i].Close();
            }
        }

        class GetPlayerStat
        {
            public string name { get; set; }
            public bool pet { get; set; }
            public string mem_adress { get; set; }
            public string buff_table1_adress { get; set; }
            public string buff_table2_adress { get; set; }
            WoWMemReader wmr;
            PartyDataRetrivalClient player_client;
            PartyDataRetrivalClient pet_client;

            WoWLivingObj obj = new WoWLivingObj();
            WoWLivingObj p_obj = new WoWLivingObj();

            public GetPlayerStat(WoWMemReader wmr)
            {
                this.wmr = wmr;
                this.name = wmr.LocalPlayerName;
                this.mem_adress = wmr.GetPlayerBaseAdress().ToString("X");
                this.buff_table1_adress = wmr.GetPlayerBaseBuffAdressTable1().ToString("X");
                this.buff_table2_adress = wmr.GetPlayerBaseBuffAdressTable2().ToString("X");
                this.pet = false;
                if (name != "Zorn")
                   player_client = new PartyDataRetrivalClient();
            }

            public int[] GetPlayerBuffs()
            {
                return p_obj.buff_arr;
            }

            public void Do()
            {
                if (name != "Zorn")
                {
                    if (pet)
                    {
                        if (pet_client == null)
                            pet_client = new PartyDataRetrivalClient();
                        wmr.GetPet(obj);
                        pet_client.Send(obj);
                    }
                    else
                    {
                        if (pet_client != null)
                        {
                            pet_client.Close();
                            pet_client = null;
                        }
                    }
                    wmr.GetPlayer(obj);
                    p_obj = obj;
                    player_client.Send(obj);
                }
            }

            public void Close()
            {
                if (name != "Zorn")
                {
                    if (player_client != null)
                    {
                        player_client.Close();
                        player_client = null;
                    }
                    if (pet_client != null)
                    {
                        pet_client.Close();
                        pet_client = null;
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            textBox1.Enabled = false;
            to_find_debuff = true;
            button4.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            to_find_debuff = false;
            button3.Enabled = true;
            textBox1.Enabled = true;
            button4.Enabled = false;
        }

    }
}
