﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchBot.CommandTypeForms;

namespace TwitchBot
{
    public partial class MainWindow : Form
    {
        TBot _bot;
        Random r = new Random();
        public TBot Bot
        {
            get { return _bot; }
        }

        public MainWindow()
        {
            using(BotStartForm bsf = new BotStartForm())
            {
                if(bsf.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    Environment.Exit(0);
                    return;
                }
                _bot = bsf.Bot;
            }
            InitializeComponent();
            _bot.OnMessageRead += _bot_OnMessageRead;
            _bot.OnDisconnect += _bot_OnDisconnect;
            _bot.OnConnect += _bot_OnConnect;
            _bot.Start();
        }

        #region " Callbacks "

        void _bot_OnConnect(TBot sender, bool success)
        {
            if (success)
            {
                MessageBox.Show("Connected");
            }
            else
            {
                MessageBox.Show("Failed to connect");
                Environment.Exit(0);
                return;
            }
        }

        void _bot_OnDisconnect(TBot sender, Exception ex)
        {
            MessageBox.Show("Lost connection! \n " + ex.ToString());
            Environment.Exit(0);
            return;
        }

        

        void _bot_OnMessageRead(TBot sender, TBotMessage message, string raw)
        {
            Chatlog.AppendText(string.Format("<{0}> {1}\n", message.Username, message.Text));
            TBotCommand command = null;
            foreach(ListViewItem i in commandList.Items)
            {
                TBotCommand _tCom = (TBotCommand)i.Tag;
                
                string msgCompare = message.Text;
                string msgCompareSplit = message.Text;
                if (msgCompare.Contains(" "))
                    msgCompareSplit = msgCompare.Split(' ')[0];
                if (!_tCom.FlagCaseSensitive)
                    msgCompare = msgCompare.ToLower();
                if (_tCom.FlagIsRegex)
                {
                    if (Regex.Match(msgCompare, _tCom.Flag).Success)
                        command = _tCom;
                }
                else
                {
                    switch(_tCom.Paramiters)
                    {
                        case ParamiterType.HasParamiters:
                            if (i.Text.ToLower() == msgCompare)
                                command = _tCom;
                            break;
                        case ParamiterType.NoParamiters:
                            if (i.Text.ToLower() == msgCompareSplit)
                                command = _tCom;
                            break;
                        default:
                            if (i.Text.ToLower() == msgCompareSplit || i.Text.ToLower() == msgCompare)
                                command = _tCom;
                            break;
                    }
                }
                if (command != null)
                    break;
            }
            if (command != null)
                ExecuteCommand(command, message);
            CheckBlacklist(message);
        }


        #endregion


        

        void ExecuteCommand(TBotCommand command, TBotMessage msg)
        {
            string[] msgBreakdown;
            if (command.RequiresModerator && !IsMod(msg.Username))
                return;
            switch(command.Data.Type)
            {
                case TBotCommandType.SayText:
                    Bot.SayAsync(((string)command.Data.TagData[0]).Replace("{username}", msg.Username));
                    break;

                case TBotCommandType.AddToGiveaway:
                    AddToGiveaway(msg.Username);
                    break;

                case TBotCommandType.BanUser:
                    msgBreakdown = msg.Text.Split(' ');
                    if (msgBreakdown.Length != 2)
                        return;
                    Bot.Ban(msgBreakdown[1].ToLower());
                    break;

                case TBotCommandType.TimeoutUser:
                    msgBreakdown = msg.Text.Split(' ');
                    if (msgBreakdown.Length != 3)
                        return;
                    int time;
                    if (!int.TryParse(msgBreakdown[2], out time))
                        return;
                    Bot.Timeout(msgBreakdown[1], time);
                    break;

                case TBotCommandType.AntiBot:
                    msgBreakdown = msg.Text.Split(' ');
                    if (msgBreakdown.Length != 2) 
                        return;
                    if (msgBreakdown[1].ToLower() != "on" && msgBreakdown[1].ToLower() != "off")
                        return;
                    this.Invoke(msgBreakdown[1].ToLower() == "on" ? (Action)Bot.AntiBotOn : Bot.AntiBotOff);
                    break;

                case TBotCommandType.StartGiveaway:
                    if (acceptGiveawayEntries.Checked)
                        return;
                    acceptGiveawayEntries.Checked = true;
                    msgBreakdown = msg.Text.Split(new char[] { ' ' });
                    if (msgBreakdown.Length == 2)
                    {
                        acceptGiveawayEntries.Checked = true;
                        AddCommand(new TBotCommand(new CommandData(TBotCommandType.AddToGiveaway), msgBreakdown[1]));
                    }
                    Bot.SayBuffer("Giveaway started!");
                    SayGiveawayCommands();
                    break;

                case TBotCommandType.EndGiveaway:
                    if (!acceptGiveawayEntries.Checked)
                        return;
                    acceptGiveawayEntries.Checked = false;
                    msgBreakdown = msg.Text.Split(new char[] { ' ' });
                    acceptGiveawayEntries.Checked = false;
                    if (giveawayEntries.Items.Count < 1)
                    {
                        GiveawayWinner.Text = "Nobody wins.";
                    }
                    else
                    {
                        string winner = (string)giveawayEntries.Items[r.Next(0, giveawayEntries.Items.Count - 1)];
                        GiveawayWinner.Text = winner;
                        _bot.SayAsync("{0} has won the giveaway!", winner);
                    }
                    if (msgBreakdown.Length == 2 && msgBreakdown[1] == "clear")
                        lock (Bot)
                        {
                            foreach(ListViewItem i in commandList.Items)
                                if (((TBotCommand)i.Tag).Data.Type == TBotCommandType.AddToGiveaway)
                                    commandList.Items.Remove(i);
                        }
                    break;

                //case TBotCommandType.WisperText:
                  //  Bot.Whisper(msg.Username, ((string)command.Data.TagData[0]).Replace("{username}", msg.Username));
                    //break;
            }
        }

        #region " Functions "

        private void SayAllCommands()
        {
            List<string> Commands = new List<string>();
            Commands.Add("Commands:");
            foreach(ListViewItem i in commandList.Items)
            {
                TBotCommand cmd = (TBotCommand)i.Tag;
                switch(cmd.Data.Type)
                {
                    case TBotCommandType.SayText:
                        Commands.Add(string.Format("[{0}] {1} - Say: {2}", i.Group.Name, cmd.Flag, (string)cmd.Data.TagData[0]));
                        break;
                    default:
                         Commands.Add(string.Format("[{0}] {1} - {2}", i.Group.Name, cmd.Flag, cmd.Data.Type));
                        break;
                }
            }
            Bot.SayAsync(string.Join("; ", Commands.ToArray()));
        }

        private void SayGiveawayCommands()
        {
            List<string> giveawayCommands = new List<string>();
            foreach (ListViewItem i in commandList.Items)
            {
                TBotCommand cmd = (TBotCommand)i.Tag;
                if (cmd.Data.Type == TBotCommandType.AddToGiveaway)
                    giveawayCommands.Add(i.Text);
            }
            if (giveawayCommands.Count < 1)
            {
                Bot.SayBuffer("No commands set to join the giveaway.");
                Bot.SayFlush();
                return;
            }
            Bot.SayBuffer("Type {0} to join the giveaway!", string.Join(" or ", giveawayCommands.ToArray()));
            Bot.SayFlush();
        }


        bool IsMod(string username)
        {
            return modList.Items.Contains(username.ToLower());
        }

        private bool AddCommand(TBotCommand cmd)
        {
            bool canAdd = true;
            foreach (ListViewItem ci in commandList.Items)
                if (ci.Text.ToLower() == cmd.Flag.ToLower())
                    canAdd = false;
            if (canAdd)
            {
                ListViewItem i = new ListViewItem(cmd.Flag);
                i.Tag = cmd;
                i.SubItems.Add(cmd.Data.Type);
                if (cmd.FlagIsRegex)
                    i.SubItems[1].Text += " [R]";
                if (cmd.FlagCaseSensitive)
                    i.SubItems[1].Text += " [C]";
                if (cmd.RequiresModerator)
                    i.Group = commandList.Groups["mod"];
                else
                    i.Group = commandList.Groups["all"];
                commandList.Items.Add(i);
            }
            return canAdd;
        }

        void CheckBlacklist(TBotMessage msg)
        {
            if (IsMod(msg.Username))
                return;
            foreach(string s in blacklistedWords.Items)
            {
                if (msg.Text.ToLower().Contains(s.ToLower()))
                {
                    _bot.Timeout(msg.Username, 60 * 2);
                    //_bot.Say("The word \"{0}\" is blacklisted, please refrain from using it.", s);
                }
            }
        }

        void AddToGiveaway(string username)
        {
            if (!acceptGiveawayEntries.Checked)
                return;
            if(MultipleGiveawayEntries.Checked)
            {
                giveawayEntries.Items.Add(username);
            }
            else
            {
                if(!giveawayEntries.Items.Contains(username))
                    giveawayEntries.Items.Add(username);
            }
            currentGiveawayEntrieCount.Text = giveawayEntries.Items.Count.ToString();
        }

        #endregion

        private void MainWindow_Load(object sender, EventArgs e)
        {

        }

        private void CommandsContext_Opening(object sender, CancelEventArgs e)
        {
            
        }

        private void addNewCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AddCommandForm acf = new AddCommandForm())
            {
                if(acf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if(!AddCommand(acf.Command))
                        MessageBox.Show("There is alredy a command with this flag.");
                }
            }
        }

        private void commandsList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void commandsList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(commandList.SelectedIndices.Count > 0)
            {
                ListViewItem i = commandList.SelectedItems[0];
                TBotCommand cd = (TBotCommand)i.Tag;
                using(AddCommandForm acf = new AddCommandForm(cd, i.Text))
                {
                    if(acf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        i.Text = acf.Command.Flag;
                        i.Tag = acf.Command;
                        i.SubItems[1].Text = acf.Command.Data.Type;
                        if (acf.Command.FlagIsRegex)
                            i.SubItems[1].Text += " [R]";
                        if (acf.Command.FlagCaseSensitive)
                            i.SubItems[1].Text += " [C]";
                        if (acf.Command.RequiresModerator)
                            i.Group = commandList.Groups["mod"];
                        else
                            i.Group = commandList.Groups["all"];
                    }
                }
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                if(textBox1.Text != string.Empty)
                {
                    _bot.SayAsync(textBox1.Text);
                    textBox1.Text = "";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Are you sure you want to clear the usernames in the current giveaway list?", "Are you sure?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                giveawayEntries.Items.Clear();
                currentGiveawayEntrieCount.Text = giveawayEntries.Items.Count.ToString();
                GiveawayWinner.Text = "-BahNahNah-";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            acceptGiveawayEntries.Checked = false;
            if (giveawayEntries.Items.Count < 1)
            {
                GiveawayWinner.Text = "Nobody wins.";
                return;
            }
            string winner = (string)giveawayEntries.Items[r.Next(0, giveawayEntries.Items.Count - 1)];
            GiveawayWinner.Text = winner;
            _bot.SayAsync("{0} has won the giveaway!", winner);
            MessageBox.Show(string.Format("{0} has won the giveaway!", winner));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SayGiveawayCommands();
        }

        
        private void removeSelectedCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (commandList.SelectedIndices.Count > 0)
            {
                ListViewItem i = commandList.SelectedItems[0];
                commandList.Items.Remove(i);
            }
        }

        private void addNewWordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(PromptStringBox psb = new PromptStringBox("Enter a word to blacklist", "Blacklist"))
            {
                if(psb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (psb.InputText != string.Empty && !blacklistedWords.Items.Contains(psb.InputText))
                    {
                        blacklistedWords.Items.Add(psb.InputText);
                    }
                }
            }
        }

        private void loadWordsFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Text file|*.txt";
                if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!System.IO.File.Exists(ofd.FileName))
                        return;
                    using(StreamReader _blFile = new StreamReader(ofd.FileName))
                    {
                        string line = string.Empty;
                        while((line = _blFile.ReadLine()) != null)
                        {
                            if(line != string.Empty && !blacklistedWords.Items.Contains(line))
                            {
                                blacklistedWords.Items.Add(line);
                            }
                        }
                        _blFile.Close();
                    }
                }
            }
        }

        private void clearListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Are you sure you want to clear the blacklisted words?", "Clear Blacklist", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                blacklistedWords.Items.Clear();
            }
        }

        private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(blacklistedWords.SelectedIndex != -1)
                blacklistedWords.Items.RemoveAt(blacklistedWords.SelectedIndex);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _bot.AntiBotOn();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _bot.AntiBotOff();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            _bot.SubsriberChat();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _bot.SubscriberChatOff();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            _bot.Slow(30);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            _bot.Slowoff();
        }

        private void addModeratorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(PromptStringBox pfs = new PromptStringBox("Username of moderator: ", "Add new moderator"))
            {
                if(pfs.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if(!modList.Items.Contains(pfs.InputText.ToLower()))
                    {
                        modList.Items.Add(pfs.InputText.ToLower());
                    }
                }
            }
        }

        private void removeSelectedModeratorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(modList.SelectedIndex != -1)
            {
                modList.Items.RemoveAt(modList.SelectedIndex);
            }
        }

        private void clearListToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Clear moderator list?", "Clear list?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                modList.Items.Clear();
            }
        }
    }
}
