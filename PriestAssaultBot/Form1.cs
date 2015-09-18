using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using TechLib;
using AssaultBotLib;

namespace PriestAssaultBot
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            timer1.Interval = delay;
        }
        PriestBot wld=null;
        int delay = 200;

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            wld.smart_win_recog = checkBox2.Checked;
            wld.to_drink = checkBox3.Checked;
            wld.auto_follow = checkBox1.Checked;
            wld.to_manapot = checkBox4.Checked;
            wld.to_mindflay = checkBox5.Checked;
            List<AssaultBot.Target> lst = new List<AssaultBot.Target>();
            lst.Add((AssaultBot.Target)wld.target);
            dataGridView1.DataSource = lst;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            wld = new PriestBot(delay);
            wld.Start();
            timer1.Start();
            button1.Enabled = false;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            wld.Close();
            timer1.Stop();
            button1.Enabled = true;
            button2.Enabled = false;
        }
    }

    public class PriestBot : AssaultBot
    {
        public PriestBot(int delay)
            : base("Lichinkin",delay)
        {
            self = new PriestSpellsCD(delay);
            n_target_state = new NoTargetState(this);
            target_alive_state = new TargetAliveState(this);
            main_state_variable = n_target_state;
        }

        class PriestSpellsCD : SelfCD
        {
            public PriestSpellsCD(int delay)
            {
                SelfCD.tick = delay;
                SelfCD.ping = delay;
            }
            override public void CDInit()
            {
                spells_cd[0] = 30*60000;
                spells_G_cd[0] = 1500; //ВампЭмбрейс

                spells_cd[1] = 7000;
                spells_G_cd[1] = gcd;//МайндБласт

                spells_cd[2] = 2*60000;
                spells_G_cd[2] = 0;//арканторрент

                spells_cd[3] = 0;
                spells_G_cd[3] = 1500;//вампирик

                spells_cd[4] = 0;
                spells_G_cd[4] = 1500;//шадоу ворд пэйн/ девоуринг плаг/ майнд флай

                spells_cd[5] = 2*60000;
                spells_G_cd[5] = 6000;

                spells_cd[7] = 5 * 60000;//шадоу фиенд
                spells_G_cd[7] = 1500;

                spells_cd[8] = 0;//пот мп
                spells_G_cd[8] = 0;

                spells_cd[9] = 0;//вода
                spells_G_cd[9] = 24000;

            }
            override public void Reset()
            {
               // arr[0] = 0;
            }
        }

        class PriestBuffs
        {
            public static int
                Drink = 34291;
        }

        public class PriestDeBuffs
        {
            public static int
                MindFlay = 18807,
                VampiricTouch = 34916,
                ShadowWordPain = 25367,
                DevouringPlague = 19280;
        }

        public NoTargetState n_target_state;
        public TargetAliveState target_alive_state;
        public State main_state_variable;
        public bool to_manapot = false;
        public bool to_mindflay = false;

        bool Assist()
        {
            if (clnt.to_do.to_do_code == Request.do1)
            {
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                snd_keys.SendKey(KeyChoice(clnt.to_do.player_id + 1));
                snd_keys.SendKey(Keys.D7);
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D1);
                self.SetGCD(1500, true);
                clnt.to_do.to_do_code = Request.DoNothing;
                sw.WriteLine("Do Shield "+Convert.ToString(clnt.to_do.player_id + 1));
                return true;
            }

            if (clnt.to_do.to_do_code == Request.do4)
            {
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                snd_keys.SendKey(Keys.D1);
                snd_keys.SendKey(Keys.OemMinus);
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D1);
                self.SetGCD(1500, true);
                clnt.to_do.to_do_code = Request.DoNothing;
                sw.WriteLine("Do  f_heal" + Convert.ToString(clnt.to_do.player_id + 1));
                return true;
            }

            if (clnt.to_do.to_do_code == Request.do5)
            {
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                snd_keys.SendKey(Keys.D0);
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D1);
                self.SetGCD(1500, true);
                clnt.to_do.to_do_code = Request.DoNothing;
                sw.WriteLine("Do  ShadForm " + Convert.ToString(clnt.to_do.player_id + 1));
                return true;
            }

            if (clnt.to_do.to_do_code == Request.do2)
            {
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                snd_keys.SendKey(KeyChoice(clnt.to_do.player_id + 1));
                snd_keys.SendKey(Keys.D8);
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D1);
                self.SetGCD(1500, true);
                clnt.to_do.to_do_code = Request.DoNothing;
                sw.WriteLine("Do  AbolishDisease " + Convert.ToString(clnt.to_do.player_id + 1));
                return true;
            }

            if (clnt.to_do.to_do_code == Request.do3)
            {
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                snd_keys.SendKey(KeyChoice(clnt.to_do.player_id + 1));
                snd_keys.SendKey(Keys.D9);
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D1);
                self.SetGCD(1500, true);
                clnt.to_do.to_do_code = Request.DoNothing;
                sw.WriteLine("Do Dispell " + Convert.ToString(clnt.to_do.player_id + 1));
                return true;
            }

            return false;
        }

        override public void Do()
        {
            while (working)
            {
                wmr.GetPlayer(self);
                wmr.GetLocalPlayerTarget(target);
                target_debuff_arr = wmr.GetLocalPlayerTargetBuffs();

                if (PriestSpellsCD.G == 0 && !HasBuff(PriestBuffs.Drink))
                {
                    if (self.hp > 0 && !WindowFocused(smart_win_recog) && clnt.Threat)
                    {
                        if(!TargetHasDebuff(PriestDeBuffs.MindFlay) && !Assist())
                            main_state_variable.Do();
                    }
                    else
                    {
                        if (self.hp > 0 && self.mp < self.max_mp * 0.66 && !clnt.Threat)
                        {
                            target_alive_state.used_mana_pot = false;
                            
                            if (to_drink && !Assist() && !WindowFocused(smart_win_recog))
                            {
                                snd_keys.SendKey(Keys.Oemplus);
                            }
                        }
                    }
                }

                self.CDdec();
                PriestSpellsCD.GCDdec();
                Thread.Sleep(delay);
            }
        }
        override public void AfterSpellFollow()
        {
            snd_keys.SendKey(Keys.D8);
            after_spell_follow = false;
        }

        ~PriestBot()
        {
            sw.Close();
        }
    }
    public class NoTargetState : State
    {
        public NoTargetState(PriestBot bot)
            : base(bot)
        { }

        override public void Do()
        {
            Thread.Sleep(500);
            bot.snd_keys.SendKey(Keys.D1);
            bot.wmr.GetLocalPlayerTarget(bot.target);

            if (bot.target.hp > 0 && bot.target.type != WoWMemReader.ObjType.Player)
            {
                bot.curr_target_guid = bot.target.GUID;

                Thread.Sleep(500); //before combat delay
                bot.self.Reset();
                PriestBot bot_tmp = (PriestBot)bot;

                bot_tmp.main_state_variable = bot_tmp.target_alive_state;
            }
        }
    }
    public class TargetAliveState : State
    {
        public TargetAliveState(PriestBot bot)
            : base(bot)
        { }

        public bool used_mana_pot = false;     

        override public void Do()
        {
            bot.snd_keys.SendKey(Keys.D1);
            bot.wmr.GetLocalPlayerTarget(bot.target);

            if (bot.target.hp > 0 && bot.curr_target_guid == bot.target.GUID)
            {
                bool tmp_mindflay =bot.TargetHasDebuff(PriestBot.PriestDeBuffs.MindFlay);

                if (bot.self.mp < bot.self.max_mp * 0.25 && bot.self.arr[7] == 0 && !tmp_mindflay)
                {
                    bot.snd_keys.SendTwoKeys(Keys.LShiftKey,Keys.D3);
                    bot.snd_keys.SendKey(Keys.Oemplus);
                    bot.snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D1);
                    bot.self.Set7SpellCD();
                    return;
                }

                if (bot.self.mp < bot.self.max_mp * 0.25 && bot.self.arr[2] == 0 && !tmp_mindflay)//аркан торрент
                {
                    bot.snd_keys.SendKey(Keys.D0);
                    bot.self.Set2SpellCD();
                    return;
                }

                if (bot.self.mp < bot.self.max_mp * 0.25 && used_mana_pot == false && ((PriestBot)bot).to_manapot && !tmp_mindflay)//пот мп
                {
                    bot.snd_keys.SendKey(Keys.OemMinus);
                    used_mana_pot = true;
                    return;
                }
      

                if ((bot.self.mp < bot.self.max_mp * 0.50 || bot.self.hp < bot.self.max_hp*0.2) && bot.self.arr[5] == 0 && !tmp_mindflay)//дисперсия
                {
                    bot.snd_keys.SendKey(Keys.D9);
                    bot.self.Set5SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(PriestBot.PriestDeBuffs.VampiricTouch) && !tmp_mindflay)
                {
                    bot.snd_keys.SendKey(Keys.D6);
                    bot.self.Set3SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(PriestBot.PriestDeBuffs.ShadowWordPain) && !tmp_mindflay)
                {
                    bot.snd_keys.SendKey(Keys.D5);
                    bot.self.Set4SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(PriestBot.PriestDeBuffs.DevouringPlague) && !tmp_mindflay)
                {
                    bot.snd_keys.SendKey(Keys.D4);
                    bot.self.Set4SpellCD();
                    return;
                }

                if (bot.self.arr[1] == 0 && !tmp_mindflay)
                {
                    bot.snd_keys.SendKey(Keys.D3);
                    bot.self.Set1SpellCD();
                    return;
                }

                if (!tmp_mindflay&&((PriestBot)bot).to_mindflay) 
                {
                    bot.snd_keys.SendKey(Keys.D2);
                    bot.self.Set4SpellCD();
                    return;
                }
            }
            else
            {
                PriestBot bot_tmp = (PriestBot)bot;
                bot_tmp.main_state_variable = bot_tmp.n_target_state;

                if (bot_tmp.auto_follow)
                    bot_tmp.AfterSpellFollow();
            }
        }
    }
}

