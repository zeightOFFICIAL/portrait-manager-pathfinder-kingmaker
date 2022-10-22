﻿using System;
using System.Net;
using System.Windows.Forms;

namespace PathfinderKingmakerPortraitManager.AuxForms
{
    public partial class UrlDialog : Form
    {
        public string URL { get; set; }
        public UrlDialog()
        {
            InitializeComponent();
        }
        private void ButtonLoad_Click(object sender, EventArgs e)
        {
            string urlString = TextBoxMain.Text;
            try
            {
                HttpWebRequest request = WebRequest.Create(urlString) as HttpWebRequest;
                request.Method = "HEAD";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                response.Close();
                URL = urlString;
                Close();
            }
            catch
            {
                TextBoxMain.Text = "Incorrect URL";
                URL = "-1";
            }
        }
        private void ButtonDeny_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void TexteditUrl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void TexteditUrl_DragDrop(object sender, DragEventArgs e)
        {
            TextBox senderTextBox = (TextBox)sender;
            senderTextBox.Text = (string)e.Data.GetData(DataFormats.Text);
        }
    }
}
