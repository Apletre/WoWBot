using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ServerLib;
using System.IO;
using System.Xml.Serialization;


namespace WoWBotServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 200;

            comboBox1.Items.Add(new ComboboxItem(DebuffTypes.Curse.ToString(), DebuffTypes.Curse));
            comboBox1.Items.Add(new ComboboxItem(DebuffTypes.Magic.ToString(),DebuffTypes.Magic));
            comboBox1.Items.Add(new ComboboxItem(DebuffTypes.Disease.ToString(), DebuffTypes.Disease));
            comboBox1.Items.Add(new ComboboxItem(DebuffTypes.Poison.ToString(), DebuffTypes.Poison));

            if (!File.Exists("Debuffs.xml"))
                using (StreamWriter sw = new StreamWriter("Debuffs.xml"))
                {
                    xml_serialize.Serialize(sw, file);
                    sw.Flush();
                }
        }

        public enum DebuffTypes { Curse, Magic, Poison, Disease }

        public class XMLDebuffContainer
        {
            [XmlArray("Debuff_List")]
            [XmlArrayItem("debuff_info")]
            public List<Debuff> debuff_arr = new List<Debuff>();
        }

        public class Debuff
        {
            public int id;
            public string name;
            public DebuffTypes type;
            public string instance_name;
        }
        
        class ComboboxItem
        {
            public ComboboxItem(string name, DebuffTypes type)
            {
                this.name = name;
                this.debuff_type = type;
            }
            public string name { get; set; }
            public DebuffTypes debuff_type;

            public override string ToString()
            {
                return name;
            }
        }

        Server srv;
        HealBot hb;
        int delay = 200;
        XmlSerializer xml_serialize = new XmlSerializer(typeof(XMLDebuffContainer));
        static XMLDebuffContainer file = new XMLDebuffContainer();   

        private void button1_Click(object sender, EventArgs e)
        {
            srv = new Server(delay);
            hb = new HealBot(Server.party_data,delay);
            srv.Start();
            timer1.Start();

            label3.Text = "Состояние: Запущен.";
            button1.Enabled = false;
            button3.Enabled = true;
            button2.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            srv.Close(); 
            button1.Enabled = true;
            button3.Enabled = false;
            label3.Text = "Состояние: Остановлен.";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            hb = new HealBot(Server.party_data,delay);
            HealBot.ReloadDebuffs();
            hb.Start();
            button2.Enabled = false;
            button4.Enabled = true;
            button5.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            hb.Close();
            button2.Enabled = true;
            button4.Enabled = false;
            button5.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            dataGridView1.DataSource = Server.party_data.Values.ToList();
            
            srv.PriestAssist = checkBox7.Checked;
            srv.ToDispellCurse = checkBox2.Checked;
            srv.ToDispellMagic = checkBox8.Checked;
            srv.ToAbolishDisease = checkBox9.Checked;
            
            hb.use_potion = checkBox1.Checked;
            hb.toAbolishPoison = checkBox3.Checked;
            hb.toDrink = checkBox5.Checked;
            hb.toAOEheal = checkBox6.Checked;
            hb.tank_heal_priority = checkBox4.Checked;

            label1.Text = Convert.ToString(srv.GetConnectedClientsNum());
            label7.Text = Convert.ToString(srv.GetConnectedAssaultBotsNum());
            label9.Text = Convert.ToString(srv.GetBadPacketsNum());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            { srv.Close(); }
            catch { }
        }

        public void AddDebuff(int id, string name, string instance_name, DebuffTypes type)
        {
            using (StreamReader mr = new StreamReader("Debuffs.xml"))
            {
                file = (XMLDebuffContainer)xml_serialize.Deserialize(mr);
            }

            Debuff debuf = new Debuff();
            debuf.id = id;
            debuf.name = name;
            debuf.type = type;
            debuf.instance_name = instance_name;

            file.debuff_arr.Add(debuf);

            using (StreamWriter sw = new StreamWriter("Debuffs.xml"))
            {
                xml_serialize.Serialize(sw, file);
                sw.Flush();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DebuffTypes t = ((ComboboxItem)comboBox1.SelectedItem).debuff_type;
            AddDebuff(Convert.ToInt32(textBox1.Text), textBox2.Text, textBox3.Text, t);
            HealBot.ReloadDebuffs();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.ShowDialog();
        }
    }

}
