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
using System.Runtime.InteropServices;
using System.Threading;
using TechLib;
using AssaultBotLib;

namespace Warlock
{
    public partial class Form1 : Form
    {
        int delay = 200;
        public Form1()
        {
            InitializeComponent();
            timer1.Interval = delay;
        }
        WarlockBot wld;

        private void button1_Click(object sender, EventArgs e)
        {
            wld = new WarlockBot(delay);
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
            wld.DrainSoul = checkBox1.Checked;
            wld.smart_win_recog = checkBox2.Checked;
            wld.to_drink = checkBox3.Checked;
            wld.auto_follow = checkBox5.Checked;
            wld.drain_mana = checkBox4.Checked;
            List<AssaultBot.Target> lst = new List<AssaultBot.Target>();
            lst.Add((AssaultBot.Target)wld.target);
            dataGridView1.DataSource = lst;
        }
    }

    public class WarlockBot : AssaultBot
    {
        public WarlockBot(int delay)
            : base("Satanoboris",delay)
        {
            self = new WarlockSpellsCD(delay);
            n_target_state = new NoTargetState(this);
            target_alive_state = new TargetAliveState(this);
            main_state_variable = n_target_state;
        }

        public bool DrainSoul = false;
        public bool drain_mana = false;

        class WarlockSpellsCD : SelfCD
        {
            public WarlockSpellsCD(int delay)
            {
                SelfCD.tick = delay;
                SelfCD.ping = delay;
            }
            override public void CDInit()
            {
                spells_cd[0] = 0;
                spells_G_cd[0] = 3000; //шб

                spells_cd[1] = gcd + 24000;
                spells_G_cd[1] = gcd+500;//имм

                spells_cd[2] = 0;
                spells_G_cd[2] = 3600;//соул фаер

                spells_cd[3] = 18000;
                spells_G_cd[3] = gcd;//корапт

                spells_cd[4] = 24000;
                spells_G_cd[4] = gcd;//керс

                spells_cd[5] = 0;
                spells_G_cd[5] = 15000;//дрэйн соул

                spells_cd[6] = 0;//мана дрэйн
                spells_G_cd[6] = 5000;

                spells_cd[7] = 0;//лайф тап
                spells_G_cd[7] = gcd;

                spells_cd[8] = 0;//камень хп
                spells_G_cd[8] = 0;

                spells_cd[9] = 0;//вода
                spells_G_cd[9] = 24000;

                spells_cd[10] = 40000;//фир
                spells_G_cd[10] = gcd;
            }
            override public void Reset()
            {
                for (int i = 0; i < arr.Length; i++)
                    if (i!=10)
                        arr[i] = 0;
            }
        }

        public class WarlockBuffs
        {
            public static int Molten_Core = 71165,
                Shadow_Trance = 17941,
                Decimation = 63167,
                Drink = 22734;
        }

        public class WarlockDeBuffs
        {
            public static int
                Corruption = 11672,
                Curse_of_elem = 11721,
                Mana_drain = 5138,
                DrainSoul = 11675,
                Immolate = 11667;
        }

        public NoTargetState n_target_state;
        public TargetAliveState target_alive_state;
        public State main_state_variable;

        override public void Do()
        {
            while (working)
            {
                wmr.GetPlayer(self);
                wmr.GetLocalPlayerTarget(target);
                target_debuff_arr = wmr.GetLocalPlayerTargetBuffs();

                if (WarlockSpellsCD.G == 0 && !(TargetHasDebuff(WarlockBot.WarlockDeBuffs.DrainSoul)||TargetHasDebuff(WarlockBot.WarlockDeBuffs.Mana_drain)||HasBuff(WarlockBuffs.Drink)))
                {
                    if (after_spell_follow)
                        AfterSpellFollow();

                    if (self.hp > 0 && !WindowFocused(smart_win_recog) && clnt.Threat)
                    {
                        main_state_variable.Do();
                    }
                    else
                    {
                        if (self.hp > 0 && self.mp < self.max_mp * 0.66 && !clnt.Threat)
                        {
                            target_alive_state.used_heal_stone = false;
                            if (to_drink && !WindowFocused(smart_win_recog))
                            {
                                snd_keys.SendKey(Keys.Oemplus);
                            }
                        }
                    }
                }

                self.CDdec();
                WarlockSpellsCD.GCDdec();
                Thread.Sleep(delay);
            }
        }

        override public void AfterSpellFollow()
        {
            snd_keys.SendTwoKeys(Keys.LShiftKey,Keys.D4);
            snd_keys.SendKey(Keys.D2);
            snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D2);

            after_spell_follow = false;
        } 
    }

    public class NoTargetState : State
    {
        public NoTargetState(WarlockBot bot)
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
                WarlockBot bot_tmp = (WarlockBot)bot;
                
                bot_tmp.main_state_variable = bot_tmp.target_alive_state;
            }
        }
    }
    public class TargetAliveState : State
    {
        public TargetAliveState(WarlockBot bot)
            : base(bot)
        { }

        public bool used_heal_stone = false;
        
        override public void Do()
        {
            bot.snd_keys.SendKey(Keys.D1);
            bot.wmr.GetLocalPlayerTarget(bot.target);

            if (bot.target.hp > 0 && bot.curr_target_guid == bot.target.GUID)
            {
                if (bot.self.mp < bot.self.max_mp * 0.1)
                {
                    bot.snd_keys.SendTwoKeys(Keys.LShiftKey,Keys.D4);
                    bot.snd_keys.SendKey(Keys.D1);
                    bot.snd_keys.SendTwoKeys(Keys.LShiftKey, Keys.D2);
                }

                if (bot.self.mp < bot.self.max_mp * 0.66 && bot.target.mp > bot.self.max_mp * 0.05 && !bot.TargetHasDebuff(WarlockBot.WarlockDeBuffs.Mana_drain) && ((WarlockBot)bot).drain_mana)
                {
                    bot.snd_keys.SendKey(Keys.D8);
                   // bot.self.Set6SpellCD();
                    bot.after_spell_follow = true;
                    return;  
                }
                if ((bot.self.mp < bot.self.max_mp * 0.25) && (bot.self.hp > bot.self.max_hp*0.5))
                {
                    bot.snd_keys.SendKey(Keys.D9);
                    bot.self.Set7SpellCD();
                    return;
                }
                if (bot.self.hp < bot.self.max_hp * 0.30 && used_heal_stone == false)
                {
                    bot.snd_keys.SendKey(Keys.OemMinus);
                    bot.self.Set8SpellCD();
                    used_heal_stone = true;
                    return;
                }
                if (bot.self.hp < bot.self.max_hp * 0.25 && bot.self.arr[9]==0)
                {
                    bot.snd_keys.SendKey(Keys.D0);
                    bot.self.Set10SpellCD();
                    return;
                }
                if (bot.target.hp < bot.target.max_hp * 0.25 && !bot.TargetHasDebuff(WarlockBot.WarlockDeBuffs.DrainSoul)&&((WarlockBot)bot).DrainSoul)
                {
                    bot.snd_keys.SendKey(Keys.D7);
                  //  bot.self.Set5SpellCD();
                    bot.after_spell_follow = true;
                    return;  
                }
                if (!bot.TargetHasDebuff(WarlockBot.WarlockDeBuffs.Immolate))
                {
                    bot.snd_keys.SendKey(Keys.D3);
                    bot.self.Set1SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(WarlockBot.WarlockDeBuffs.Curse_of_elem))
                {
                    bot.snd_keys.SendKey(Keys.D6);
                    bot.self.Set4SpellCD();
                    return;
                }

                if (!bot.TargetHasDebuff(WarlockBot.WarlockDeBuffs.Corruption))
                {
                    bot.snd_keys.SendKey(Keys.D5);
                    bot.self.Set3SpellCD();
                    return;
                }


                if (bot.self.arr[2] == 0 && bot.target.hp < bot.target.max_hp * 0.35 && bot.HasBuff(WarlockBot.WarlockBuffs.Decimation))
                {
                    bot.snd_keys.SendKey(Keys.D4);
                    bot.self.Set2SpellCD();
                    return;
                }

                if (true)
                {
                    bot.snd_keys.SendKey(Keys.D2);

                    if (!bot.HasBuff(WarlockBot.WarlockBuffs.Shadow_Trance))
                        bot.self.Set0SpellCD();
                    else
                        bot.self.SetGCD(1500,true);
                    return;
                } 
            }
            else
            {
                WarlockBot bot_tmp = (WarlockBot)bot;
                bot_tmp.main_state_variable = bot_tmp.n_target_state;

                if (bot_tmp.auto_follow)
                {
                    bot_tmp.AfterSpellFollow();
                }
            }
        }
    }
}
