using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using DebuffDispell;

namespace WoWBotServer
{
    public partial class Form2 : Form
    {
        private DebuffPurify.XMLDebuffContainer file;
        XmlSerializer xml_serialize = new XmlSerializer(typeof(DebuffPurify.XMLDebuffContainer));
        private DebuffPurify.Debuff[] debuff_arr;
        public Form2()
        {
            InitializeComponent();
            
            using (StreamReader mr = new StreamReader("Debuffs.xml"))
            {
                file = (DebuffPurify.XMLDebuffContainer)xml_serialize.Deserialize(mr);
            }

            debuff_arr = file.debuff_arr.ToArray();
            dataGridView1.DataSource = debuff_arr;

        }

        public void DebuffDel(string name)
        {
            debuff_arr = debuff_arr.Where(p => p.name != name).ToArray();
            dataGridView1.DataSource = debuff_arr;

            file.debuff_arr = debuff_arr.ToList();

            using (StreamWriter sw = new StreamWriter("Debuffs.xml"))
            {
                xml_serialize.Serialize(sw, file);
                sw.Flush();
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
