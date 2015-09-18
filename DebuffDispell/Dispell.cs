
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DebuffDispell
{
    public class DebuffPurify
    {
        static XmlSerializer xml_serialize = new XmlSerializer(typeof(XMLDebuffContainer));
        static XMLDebuffContainer file=new XMLDebuffContainer();

        public enum DebuffTypes{ Curse, Magic, Poison, Disease }

        public class XMLDebuffContainer
        {
            [XmlArray("Debuff_List")]
            [XmlArrayItem("debuff_info")]
            public List<Debuff>  debuff_arr= new List<Debuff>();
        }

        public class Debuff
        {
            public int id { get; set; }
            public string name { get; set; }
            public DebuffTypes type { get; set; }
            public string instance_name { get; set; }
        }

        public static void ReloadDebuffs()
        {
            using (StreamReader mr = new StreamReader("Debuffs.xml"))
            {
                file = (XMLDebuffContainer)xml_serialize.Deserialize(mr);
            }

            if (file.debuff_arr.Count > 0)
            {
                FillArray(file,ref CurseToDispellArray,DebuffTypes.Curse);
                FillArray(file,ref MagicToDispellArray, DebuffTypes.Magic);
                FillArray(file,ref PoisonToDispellArray, DebuffTypes.Poison);
                FillArray(file,ref DiseaseToDispellArray, DebuffTypes.Disease);
            }
        }     

        static void FillArray(XMLDebuffContainer file,ref int[] debuff_arr, DebuffTypes debuff_type)
        {
            List<int> tmp_lst = new List<int>();

            foreach (Debuff item in file.debuff_arr)
            {
                if (item.type == debuff_type)
                    tmp_lst.Add(item.id);
            }

            if(tmp_lst.Count>0)
                debuff_arr = tmp_lst.ToArray();
        }

        protected static int[] CurseToDispellArray = new int[0];
        protected static int[] PoisonToDispellArray = new int[0];
        protected static int[] DiseaseToDispellArray = new int[0];
        protected static int[] MagicToDispellArray = new int[0];

        protected bool toPurify(int[] DB_arr, int[] buff_arr)
        {
            for (int i = 0; i < buff_arr.Length; i++)
            {
                if (BinarySearch(DB_arr, buff_arr[i]))
                    return true;
            }
            return false;
        }

        /*       bool BinarySearch(int[] DB_arr, int id)
               {
                   int a = 0;
                   int b = DB_arr.Length - 1;
                   int c=0;
                   int index;

                   if (DB_arr.Length == 0)
                       return false;

                   if(DB_arr[a]==id)
                       return true;

                   if(DB_arr[b]==id)
                       return true;

                   if (DB_arr.Length > 2)
                   {
                       do
                       {
                           c = (b - a) / 2;
                           index = a + c;

                           if (DB_arr[index] == id)
                               return true;

                           if (DB_arr[index] < id)
                               a = index;

                           if (DB_arr[index] > id)
                               b = index;
                       }
                       while (c > 1);
                   }

                   return false;
               }*/

        bool BinarySearch(int[] DB_arr, int id)
        {
            for (int i = 0; i < DB_arr.Length; i++)
            {
                if (DB_arr[i] == id)
                    return true;
            }
            return false;
        }
    }
}
