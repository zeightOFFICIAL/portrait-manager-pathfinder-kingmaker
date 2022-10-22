﻿using System;
using System.Windows.Forms;

namespace PathfinderKingmakerPortraitManager.AuxForms
{
    public partial class MyMessageDialog : Form
    {
        public MyMessageDialog(string labelText)
        {
            InitializeComponent();
            LabelMain.Text = labelText;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
