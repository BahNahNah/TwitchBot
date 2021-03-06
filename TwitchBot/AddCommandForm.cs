﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchBot.CommandTypeForms;

namespace TwitchBot
{
    public partial class AddCommandForm : Form
    {
        private TBotCommand _data;
        public TBotCommand Command
        {
            get { return _data; }
        }
        public AddCommandForm()
        {
            InitializeComponent();
            foreach (var cmdName in typeof(TBotCommandType).GetFields())
                commandTypeList.Items.Add(cmdName.GetValue(cmdName));
            if (commandTypeList.Items.Count > 0)
                commandTypeList.SelectedIndex = 0;
            SetComponent(null, "");
        }
        public AddCommandForm(TBotCommand command, string flag)
        {
            InitializeComponent();
            flagIsregex.Checked = command.FlagIsRegex;
            flagCasesensitive.Checked = command.FlagCaseSensitive;
            modOnly.Checked = command.RequiresModerator;
            foreach (var cmdName in typeof(TBotCommandType).GetFields())
                commandTypeList.Items.Add(cmdName.GetValue(cmdName));
            commandTypeList.Text = command.Data.Type;
            SetComponent(command, flag);
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void AddCommandForm_Load(object sender, EventArgs e)
        {

        }

        void SetComponent(TBotCommand command, string flag)
        {
            commandOptionPanel.Controls.Clear();
            if (flag != "")
                textBox1.Text = flag;
            switch (commandTypeList.Text)
            {
                case TBotCommandType.SayText:
                    Command_sayText f = new Command_sayText(CommandChosen);
                    if (command != null)
                        f.SetValues((string)command.Data.TagData[0]);
                    commandOptionPanel.Controls.Add(f);
                    break;
                case TBotCommandType.AddToGiveaway:
                    SetCommandPanel(new Command_AddToGiveaway(CommandChosen));
                    break;
                case TBotCommandType.BanUser:
                    SetCommandPanel(new Command_banUser(CommandChosen));
                    break;
                case TBotCommandType.TimeoutUser:
                    SetCommandPanel(new Command_TimeoutUser(CommandChosen));
                    break;
                case TBotCommandType.AntiBot:
                    SetCommandPanel(new Command_AntiBotOnOff(CommandChosen));
                    break;
                case TBotCommandType.StartGiveaway:
                    SetCommandPanel(new Command_StartGiveaway(CommandChosen));
                    break;
                case TBotCommandType.EndGiveaway:
                    SetCommandPanel(new Command_EndGiveaway(CommandChosen));
                    break;
                    /*
                case TBotCommandType.WisperText:
                    SetCommandPanel(new Command_wisperText(CommandChosen));
                    break;
                    */
                default:
                    SetCommandPanel(new Command_notCompleted());
                    break;
            }
        }

        void SetCommandPanel(Control ctr)
        {
            commandOptionPanel.Controls.Clear();
            commandOptionPanel.Controls.Add(ctr);
        }

        void CommandChosen(CommandData cmd, ParamiterType paramiterOptions)
        {
            if(textBox1.Text == "")
            {
                MessageBox.Show("Enter a flag");
                return;
            }

            if (paramiterOptions != ParamiterType.NoParamiters)
            {
                if(textBox1.Text.Contains(' '))
                {
                    MessageBox.Show("This command does not support spaces in the flag.");
                    return;
                }
                if(flagIsregex.Checked)
                {
                    MessageBox.Show("This command does not support regex flags.");
                    return;
                }
            }

            if (flagIsregex.Checked)
            {
                try
                {
                    Regex.Match("testRegex", textBox1.Text);
                }
                catch
                {
                    MessageBox.Show("Regex is invalid.");
                    return;
                }
            }
            _data = new TBotCommand(cmd, textBox1.Text);
            _data.FlagIsRegex = flagIsregex.Checked;
            _data.FlagCaseSensitive = flagCasesensitive.Checked;
            _data.RequiresModerator = modOnly.Checked;
            _data.Paramiters = paramiterOptions;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void commandTypeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetComponent(null, "");
        }
    }
}
