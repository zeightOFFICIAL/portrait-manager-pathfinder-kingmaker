﻿/*    
    Pathfinder Portrait Manager. Desktop application for managing in game
    portraits for Pathfinder: Kingmaker and Pathfinder: Wrath of the Righteous
    Copyright (C) 2023 Artemii "Zeight" Saganenko
    LICENSE terms are written in LICENSE file
    Primal license header is written in Program.cs
*/

using PathfinderPortraitManager.sources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace PathfinderPortraitManager
{
    public partial class MainForm : Form
    {
        private static readonly GameTypeClass WrathType = new GameTypeClass("Wrath of the Righteous",
            Color.FromArgb(255, 20, 147), Color.FromArgb(20, 6, 30),
            Properties.Resources.icon_wotr, Properties.Resources.title_wotr,
            Properties.Resources.bg_wotr, Properties.Resources.placeholder_wotr,
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow")
            + "\\Owlcat Games\\Pathfinder Wrath Of The Righteous\\Portraits", "Pathfinder Portrait Manager (WoTR)");
        private static readonly GameTypeClass KingmakerType = new GameTypeClass("Kingmaker",
            Color.FromArgb(218, 165, 32), Color.FromArgb(9, 28, 11),
            Properties.Resources.icon_path, Properties.Resources.title_path,
            Properties.Resources.bg_path, Properties.Resources.placeholder_path,
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow")
            + "\\Owlcat Games\\Pathfinder Kingmaker\\Portraits", "Pathfinder Portrait Manager (Kingmaker)");
        private static readonly Dictionary<char, GameTypeClass> GAME_TYPES = new Dictionary<char, GameTypeClass>
        {
            { 'p', KingmakerType},
            { 'w', WrathType}
        };
        private static readonly Dictionary<char, string> ACTIVE_PATHS = new Dictionary<char, string>
        {
            { 'p', Properties.CoreSettings.Default.KINGPath },
            { 'w', Properties.CoreSettings.Default.WOTRPath }
        };

        private const string TEMPFULL_APPEND = "temp\\portrait_full.png";
        private const string TEMPPOOR_APPEND = "temp\\portrait_poor.png";
        private const string LARGE_APPEND = "\\Fulllength.png";
        private const string MEDIUM_APPEND = "\\Medium.png";
        private const string SMALL_APPEND = "\\Small.png";
        private const float LARGE_ASPECT = 1.479768786f;
        private const float MEDIUM_ASPECT = 1.309090909f;
        private const float SMALL_ASPECT = 1.308108108f;

        private Point _mousePosition = new Point();
        private int _isDragging = 0;
        private bool _isAnyLoaded = false;
        private char _gameSelected = Properties.CoreSettings.Default.GameType;
        private PrivateFontCollection _fontCollection;

        public MainForm()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Properties.CoreSettings.Default.ActiveLocal);
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Properties.UseStamps.Default.isFirstAny)
            {
                Properties.CoreSettings.Default.KINGPath = KingmakerType.DefaultDirectory;
                Properties.CoreSettings.Default.WOTRPath = WrathType.DefaultDirectory;
                Properties.CoreSettings.Default.MaxWindowHeight = Size.Height;
                Properties.CoreSettings.Default.MaxWindowWidth = Size.Width;                
                Properties.CoreSettings.Default.Save();
                Properties.UseStamps.Default.isFirstAny = false;
                Properties.UseStamps.Default.Save();
                ACTIVE_PATHS['w'] = Properties.CoreSettings.Default.WOTRPath;
                ACTIVE_PATHS['p'] = Properties.CoreSettings.Default.KINGPath;
            }
            Width = Properties.CoreSettings.Default.MaxWindowWidth;
            Height = Properties.CoreSettings.Default.MaxWindowHeight;
            CenterToScreen();

            ParentLayoutsSetDockFill();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            PicPortraitTemp.AllowDrop = true;
            PicPortraitLrg.MouseWheel += PicPortraitLrg_MouseWheel;
            PicPortraitMed.MouseWheel += PicPortraitMed_MouseWheel;
            PicPortraitSml.MouseWheel += PicPortraitSml_MouseWheel;

            _fontCollection = SystemControl.FileControl.InitCustomFont(Properties.Resources.BebasNeue_Regular);
            FontsAndTextLoad(_fontCollection);
            UpdateColorScheme();

            ParentLayoutsHide();
            LayoutHide(LayoutURLDialog);
            LayoutHide(LayoutFinalPage);
            LayoutReveal(LayoutMainPage);
            if (!ValidatePotraitPath(ACTIVE_PATHS[_gameSelected]))
            {
                using (forms.MyMessageDialog Mesg = new forms.MyMessageDialog(Properties.TextVariables.MESG_GAMEFOLDERNOTFOUND))
                {
                    Mesg.StartPosition = FormStartPosition.CenterScreen;
                    Mesg.ShowDialog();
                }
            }
        }
        private void ButtonToFilePage_Click(object sender, EventArgs e)
        {
            SafeCopyAllImages("!DEFAULT!");
            LoadAllTempImages();
            _isAnyLoaded = false;
            ParentLayoutsHide();
            LayoutReveal(LayoutFilePage);
            ResizeVisibleImagesToWindow();
            if (Properties.UseStamps.Default.isFirstPortrait == true)
            {
                using (forms.MyMessageDialog HintDialog = new forms.MyMessageDialog(Properties.TextVariables.HINT_FILEPAGE))
                {
                    HintDialog.ShowDialog();
                }
            }
        }
        private void ButtonToScalePage_Click(object sender, EventArgs e)
        {
            if (!_isAnyLoaded)
            {
                DialogResult dr;
                using (forms.MyInquiryDialog InquiryDialog = new forms.MyInquiryDialog(Properties.TextVariables.INQR_NOIMAGECHOSEN))
                {
                    InquiryDialog.ShowDialog();
                    dr = InquiryDialog.DialogResult;
                }
                if (dr == DialogResult.OK)
                {
                    ParentLayoutsHide();
                    LayoutReveal(LayoutScalePage);
                    LoadAllTempImages();
                    ResizeVisibleImagesToWindow();
                }
                else
                {
                    return;
                }
            }
            else
            {
                ParentLayoutsHide();
                LayoutReveal(LayoutScalePage);
                LoadAllTempImages();
                ResizeVisibleImagesToWindow();
            }
            if (Properties.UseStamps.Default.isFirstScaling)
            {
                using (forms.MyMessageDialog HintDialog = new forms.MyMessageDialog(Properties.TextVariables.HINT_SCALEPAGE))
                {
                    HintDialog.ShowDialog();
                }
                Properties.UseStamps.Default.isFirstScaling = false;
                Properties.UseStamps.Default.Save();
            }
        }
        private void ButtonToFilePage2_Click(object sender, EventArgs e)
        {
            LoadAllTempImages();
            ParentLayoutsHide();
            LayoutReveal(LayoutFilePage);
            ResizeVisibleImagesToWindow();
        }
        private void ButtonExit_Click(object sender, EventArgs e)
        {
            DisposePrimeImages();
            ClearImageLists(ListGallery, ImgListGallery);
            ClearImageLists(ListExtract, ImgListExtract);
            SystemControl.FileControl.TempImagesClear();
            Application.Exit();
        }
        private void ButtonToExtract_Click(object sender, EventArgs e)
        {
            ParentLayoutsHide();
            LayoutReveal(LayoutExtractPage);
            if (Properties.UseStamps.Default.isFirstExtract == true)
            {
                using (forms.MyMessageDialog HintDialog = new forms.MyMessageDialog(Properties.TextVariables.HINT_EXTRACTPAGE))
                {
                    HintDialog.ShowDialog();
                }
                Properties.UseStamps.Default.isFirstExtract = false;
                Properties.UseStamps.Default.Save();
            }
            ButtonExtractAll.Enabled = false;
            ButtonExtractSelected.Enabled = false;
            ButtonOpenFolders.Enabled = false;
        }
        private void ButtonToGalleryPage_Click(object sender, EventArgs e)
        {
            ParentLayoutsHide();
            LayoutReveal(LayoutGallery);
            if (!LoadGallery(GAME_TYPES[_gameSelected].DefaultDirectory))
            {
                ButtonToMainPage3_Click(sender, e);
                return;
            }
            if (Properties.UseStamps.Default.isFirstGallery == true)
            {
                using (forms.MyMessageDialog HintDialog = new forms.MyMessageDialog(Properties.TextVariables.HINT_GALLERYPAGE))
                {
                    HintDialog.ShowDialog();
                }
                Properties.UseStamps.Default.isFirstGallery = false;
                Properties.UseStamps.Default.Save();
            }
        }
        private void ButtonToMainPage_Click(object sender, EventArgs e)
        {
            ClearTempImages();
            ParentLayoutsHide();
            LayoutReveal(LayoutMainPage);
        }
        private void ButtonToMainPage2_Click(object sender, EventArgs e)
        {
            _extractFolderPath = "!NONE!";
            ClearImageLists(ListExtract, ImgListExtract);
            ParentLayoutsHide();
            LayoutReveal(LayoutMainPage);
        }
        private void ButtonToMainPage3_Click(object sender, EventArgs e)
        {
            ClearImageLists(ListGallery, ImgListGallery);
            ParentLayoutsHide();
            LayoutReveal(LayoutMainPage);
        }
        private void ButtonToMainPage4_Click(object sender, EventArgs e)
        {
            LayoutHide(LayoutFinalPage);
            ClearTempImages();
            _isAnyLoaded = false;
            ParentLayoutsHide();
            LayoutReveal(LayoutMainPage);
            ButtonToMainPageAndFolder.Enabled = true;
        }
        private void ButtonToMainPage5_Click(object sender, EventArgs e)
        {
            LayoutHide(LayoutSettingsPage);
            LayoutReveal(LayoutMainPage);
            if (ButtonValidatePath.Text == Properties.TextVariables.BUTTON_OK)
            {
                if (_gameSelected == 'p')
                    Properties.CoreSettings.Default.KINGPath = TextBoxFullPath.Text;
                else
                    Properties.CoreSettings.Default.WOTRPath = TextBoxFullPath.Text;
            }
            Properties.CoreSettings.Default.MaxWindowHeight = Height;
            Properties.CoreSettings.Default.MaxWindowWidth = Width;
            Properties.CoreSettings.Default.Save();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            ButtonValidatePath.Text = Properties.TextVariables.BUTTON_VALIDATE;
            ButtonValidatePath.BackColor = Color.Black;
            ButtonValidatePath.ForeColor = Color.White;
            ButtonValidatePath.Enabled = true;
            ButtonOpenPath.BackColor = Color.Black;
            ButtonOpenPath.ForeColor = Color.White;
            ButtonOpenPath.Text = Properties.TextVariables.BUTTON_OPENPATH;
            ButtonOpenPath.Enabled = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            CenterToScreen();
        }
        private void ButtonToSettingsPage_Click(object sender, EventArgs e)
        {
            LayoutHide(LayoutMainPage);
            TextBoxFullPath.Text = ACTIVE_PATHS[_gameSelected];
            LayoutReveal(LayoutSettingsPage);
            ButtonToMainPage5.ForeColor = Color.White;
            ButtonToMainPage5.BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.Sizable;
        }
    }
}
