using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using TechLib;
using AssaultBotLib;

namespace Mage
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            timer1.Interval = delay;
        }
        MageBot wld;
        int delay = 200;

        private void button1_Click(object sender, EventArgs e)
        {
            
            wld = new MageBot(delay);
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            wld.smart_win_recog = checkBox2.Checked;
            wld.to_drink = checkBox3.Checked;
            wld.auto_follow = checkBox1.Checked;
            List<AssaultBot.Target> lst = new List<AssaultBot.Target>();
            lst.Add((AssaultBot.Target)wld.target);
            dataGridView1.DataSource = lst;
        }
    }

    public class MageBot : AssaultBot
    {
        public MageBot(int delay)
            : base("Format", delay)
        {
            self = new MageSpellsCD(delay);
            n_target_state = new NoTargetState(this);
            target_alive_state = new TargetAliveState(this);
            main_state_variable = n_target_state;
        }

        private class MageSpellsCD : SelfCD
        {
            public MageSpellsCD(int delay)
            {
                SelfCD.tick = delay;
                SelfCD.ping = delay;
            }

            public override void CDInit()
            {
                //  spells_cd[0] = 20000;
                spells_cd[0] = 0;
                spells_G_cd[0] = 1500; //Скорч

                spells_cd[1] = 6000;
                spells_G_cd[1] = gcd; //фаербласт

                spells_cd[2] = 0;
                spells_G_cd[2] = 5000; //пиробласт


                spells_cd[7] = 240000; //эвок
                spells_G_cd[7] = 8000;

                spells_cd[8] = 0; //камень мп
                spells_G_cd[8] = 0;

                spells_cd[9] = 0; //вода
                spells_G_cd[9] = 24000;

                spells_cd[10] = 300000; //айсблок
                spells_G_cd[10] = 10000;
            }

            public override void Reset()
            {
                arr[0] = 0;
            }
        }

        public class MageBuffs
        {
            public static int
                Drink = 34291,
                HotStreak = 48108,
                MoltenArmor=30482,
                ManaShield=10193;
        }

        public class MageDeBuffs
        {
            public static int
                Scorch = 22959,
                LivingBomb = 44457;
        }

        public NoTargetState n_target_state;
        public TargetAliveState target_alive_state;
        public State main_state_variable;

        private bool Assist()
        {
            if (clnt.to_do.to_do_code == Request.do1)
            {
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                snd_keys.SendKey(KeyChoice(clnt.to_do.player_id + 1));
                snd_keys.SendKey(Keys.D7);
                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                self.SetGCD(1500, true);
                clnt.to_do.to_do_code = Request.DoNothing;
                sw.WriteLine("Curse " + Convert.ToString(clnt.to_do.player_id + 1));
                return true;
            }

            return false;
        }

        public override void Do()
        {
            while (working)
            {
                wmr.GetPlayer(self);
                wmr.GetLocalPlayerTarget(target);
                target_debuff_arr = wmr.GetLocalPlayerTargetBuffs();

                if (MageSpellsCD.G == 0 && !HasBuff(MageBuffs.Drink))
                {
                    if (self.hp > 0 && !WindowFocused(smart_win_recog) && clnt.Threat)
                    {
                        if (!Assist())
                            main_state_variable.Do();
                    }
                    else
                    {
                        if (self.hp > 0 && self.mp < self.max_mp*0.66 && !clnt.Threat)
                        {
                            target_alive_state.used_mana_stone = false;

                            if (!HasBuff(MageBuffs.MoltenArmor))
                            {
                                snd_keys.SendTwoKeys(Keys.LShiftKey,Keys.D3);
                                snd_keys.SendKey(Keys.D9);
                                snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                                self.SetGCD(1500,true);
                                continue;
                            }


                            if (to_drink && !Assist() && !WindowFocused(smart_win_recog))
                            {
                                snd_keys.SendKey(Keys.Oemplus);
                            }
                        }
                    }
                }

                self.CDdec();
                MageSpellsCD.GCDdec();
                Thread.Sleep(delay);
            }
        }

        public override void AfterSpellFollow()
        {
            snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D4);
            snd_keys.SendKey(Keys.D2);
            snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D2);

            after_spell_follow = false;
        }

        ~MageBot()

        {
            sw.Close();
        }
    }

    public class NoTargetState : State
    {
        public NoTargetState(MageBot bot)
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
                MageBot bot_tmp = (MageBot)bot;
                
                bot_tmp.main_state_variable = bot_tmp.target_alive_state;
            }
        }
    }
    public class TargetAliveState : State
    {
        public TargetAliveState(MageBot bot)
            : base(bot)
        { }

        public bool used_mana_stone = false;

        override public void Do()
        {
            bot.snd_keys.SendKey(Keys.D1);
            bot.wmr.GetLocalPlayerTarget(bot.target);

            if (bot.target.hp > 0 && bot.curr_target_guid == bot.target.GUID)
            {
                if (!bot.HasBuff(MageBot.MageBuffs.ManaShield))
                {
                    bot.snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D3);
                    bot.snd_keys.SendKey(Keys.D8);
                    bot.snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                    bot.self.SetGCD(1500, true);
                    return;
                }

                if (bot.self.hp < bot.self.max_hp * 0.25)//айсблок
                {
                    bot.snd_keys.SendKey(Keys.D9);
                    bot.self.Set10SpellCD();
                    return;
                }
        
                if (bot.self.mp < bot.self.max_mp * 0.25 && used_mana_stone == false)//камень мп
                {
                    bot.snd_keys.SendKey(Keys.OemMinus);
                    bot.self.Set8SpellCD();
                    used_mana_stone = true;
                    return;
                }
                if (bot.self.mp < bot.self.max_mp * 0.33 && bot.self.arr[7] == 0)//эвок
                {
                    bot.snd_keys.SendKey(Keys.D0);
                    bot.self.Set7SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(MageBot.MageDeBuffs.Scorch)) //Скорч
                {
                    bot.snd_keys.SendKey(Keys.D2);
                    bot.self.Set0SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(MageBot.MageDeBuffs.LivingBomb)) //Скорч
                {
                    bot.snd_keys.SendKey(Keys.D5);
                    bot.self.Set0SpellCD();
                    return;
                }

                if (bot.self.arr[1] == 0)//фаербласт
                {
                    bot.snd_keys.SendKey(Keys.D3);
                    bot.self.Set1SpellCD();
                    return;
                }

                if (true)//пиробласт
                {
                    bot.snd_keys.SendKey(Keys.D4);

                    if (bot.HasBuff(MageBot.MageBuffs.HotStreak))
                        bot.self.SetGCD(1500,true);
                    else
                        bot.self.Set2SpellCD();
                    return;
                }
            }
            else
            {
                MageBot bot_tmp = (MageBot)bot;
                bot_tmp.main_state_variable = bot_tmp.n_target_state;

                if (bot_tmp.auto_follow)
                    bot_tmp.AfterSpellFollow();
            }
        }
    }
}
