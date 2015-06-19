﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TwitchBot.CommandTypeForms
{
    public partial class Command_AntiBotOnOff : UserControl
    {
        CommandCallback callback;
        public Command_AntiBotOnOff(CommandCallback cc)
        {
            callback = cc;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (callback != null)
                callback(new CommandData(TBotCommandType.AntiBot), ParamiterType.HasParamiters);
        }
    }
}
