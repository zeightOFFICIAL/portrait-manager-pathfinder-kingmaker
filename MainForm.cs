﻿/*    
    Pathfinder Portrait Manager. Desktop application for managing in game
    portraits for Pathfinder: Kingmaker and Pathfinder: Wrath of the Righteous
    Copyright (C) 2023 Artemii "Zeight" Saganenko
    LICENSE terms are written in LICENSE file
    Primal license header is written in Program.cs
*/

using PathfinderPortraitManager.forms;
using PathfinderPortraitManager.Properties;
using PathfinderPortraitManager.sources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PathfinderPortraitManager
{
    public partial class MainForm : Form
    {
        private static readonly GameTypeClass WrathType = new GameTypeClass("Wrath of the Righteous",
            Color.FromArgb(255, 20, 147), Color.FromArgb(20, 6, 30),
            Resources.icon_wotr, Resources.title_wotr,
            Resources.bg_wotr, Resources.placeholder_wotr,
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow")
            + "\\Owlcat Games\\Pathfinder Wrath Of The Righteous\\Portraits", "Pathfinder Portrait Manager (WoTR)");
        private static readonly GameTypeClass KingmakerType = new GameTypeClass("Kingmaker",
            Color.FromArgb(218, 165, 32), Color.FromArgb(9, 28, 11),
            Resources.icon_path, Resources.title_path,
            Resources.bg_path, Resources.placeholder_path,
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow")
            + "\\Owlcat Games\\Pathfinder Kingmaker\\Portraits", "Pathfinder Portrait Manager (Kingmaker)");
        private static readonly Dictionary<char, GameTypeClass> GAME_TYPES = new Dictionary<char, GameTypeClass>
        {
            { 'p', KingmakerType},
            { 'w', WrathType}
        };
        private static readonly Dictionary<char, string> ACTIVE_PATHS = new Dictionary<char, string>
        {
            { 'p', CoreSettings.Default.WOTRPath },
            { 'w', CoreSettings.Default.KINGPath }
        };

        private const string TEMP_LARGE_APPEND = "temp_DoNotDeleteWhileRunning\\FULL_DoNotDeleteWhileRunning.png";
        private const string TEMP_MEDIUM_APPEND = "temp_DoNotDeleteWhileRunning\\MEDIUM_DoNotDeleteWhileRunning.png";
        private const string TEMP_SMALL_APPEND = "temp_DoNotDeleteWhileRunning\\SMALL_DoNotDeleteWhileRunning.png";
        private static readonly string[] TEMP_APPENDS = { TEMP_LARGE_APPEND, TEMP_MEDIUM_APPEND, TEMP_SMALL_APPEND };
        private const string LARGE_APPEND = "\\Fulllength.png";
        private const string MEDIUM_APPEND = "\\Medium.png";
        private const string SMALL_APPEND = "\\Small.png";
        private const float LARGE_ASPECT = 1.479768786f;
        private const float MEDIUM_ASPECT = 1.309090909f;
        private const float SMALL_ASPECT = 1.308108108f;

        private static ushort _imageFlag = 0;
        private static Point _mousePosition = new Point();
        private static int _isDragging = 0;
        private static bool _isAnyLoaded = false;
        private static char _gameSelected = CoreSettings.Default.GameType;
        private static PrivateFontCollection _fontCollection;

        private CancellationTokenSource _cancellationTokenSource;

        public MainForm()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (UseStamps.Default.isFirstAny)
            {
                CoreSettings.Default.KINGPath = KingmakerType.DefaultDirectory;
                CoreSettings.Default.WOTRPath = WrathType.DefaultDirectory;
                CoreSettings.Default.MaxWindowHeight = Size.Height;
                CoreSettings.Default.MaxWindowWidth = Size.Width;
                CoreSettings.Default.Save();
                UseStamps.Default.isFirstAny = false;
                UseStamps.Default.Save();
            }
            ACTIVE_PATHS['w'] = CoreSettings.Default.WOTRPath;
            ACTIVE_PATHS['p'] = CoreSettings.Default.KINGPath;
            Width = CoreSettings.Default.MaxWindowWidth;
            Height = CoreSettings.Default.MaxWindowHeight;

            CenterToScreen();
            ParentLayoutsSetDockFill();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            string resxFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                               Thread.CurrentThread.CurrentUICulture.Name,
                                               "Pathfinder Portrait Manager.resources.dll");
            if (File.Exists(resxFilePath))
            {
                SetFontsNotEN();
            }
            else
            {
                _fontCollection = SystemControl.FileControl.InitCustomFont(Resources.BebasNeue_Regular);
                SetFonts(_fontCollection);
            }
            SetTexts();
            UpdateColorScheme();
            PicPortraitTemp.AllowDrop = true;
            PicPortraitLrg.MouseWheel += PicPortraitLrg_MouseWheel;
            PicPortraitMed.MouseWheel += PicPortraitMed_MouseWheel;
            PicPortraitSml.MouseWheel += PicPortraitSml_MouseWheel;

            ParentLayoutsDisable();
            RootFunctions.LayoutDisable(LayoutURLDialog);
            RootFunctions.LayoutDisable(LayoutFinalPage);
            RootFunctions.LayoutEnable(LayoutMainPage);

            if (!ValidatePortraitPath(ACTIVE_PATHS[_gameSelected]))
            {
                using (MyMessageDialog Message = new MyMessageDialog(TextVariables.MESG_GAMEFOLDERNOTFOUND))
                {
                    Message.StartPosition = FormStartPosition.CenterScreen;
                    Message.ShowDialog();
                }
                RemoveClickEventsFromMainButtons();
            }
        }
        private void ButtonToFilePage_Click(object sender, EventArgs e)
        {
            RestoreFilePage();
            SafeCopyAllImages("!DEFAULT!", 100);
            GenerateImageFlagString(100);
            LoadTempImages(100);
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutFilePage);
            ResizeVisibleImagesToWindow();

            if (UseStamps.Default.isFirstPortrait == true)
            {
                using (MyMessageDialog Hint = new MyMessageDialog(TextVariables.HINT_FILEPAGE))
                {
                    Hint.StartPosition = FormStartPosition.CenterParent;
                    Hint.ShowDialog();
                }
                UseStamps.Default.isFirstPortrait = false;
                UseStamps.Default.Save();
            }
        }
        private void ButtonToScalePage_Click(object sender, EventArgs e)
        {
            if (!_isAnyLoaded)
            {
                using (MyInquiryDialog Inquiry = new MyInquiryDialog(TextVariables.INQR_NOIMAGECHOSEN))
                {
                    Inquiry.StartPosition = FormStartPosition.CenterParent;
                    Inquiry.Width = Width - 16;
                    if (Inquiry.ShowDialog() == DialogResult.OK)
                    {
                        ParentLayoutsDisable();
                        LoadAllTempImages();
                        RootFunctions.LayoutEnable(LayoutScalePage);
                        ResizeVisibleImagesToWindow();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                ParentLayoutsDisable();
                LoadAllTempImages();
                RootFunctions.LayoutEnable(LayoutScalePage);
                ResizeVisibleImagesToWindow();
            }
            GenerateImageFlagString(0);

            if (UseStamps.Default.isFirstScaling)
            {
                using (MyMessageDialog Hint = new MyMessageDialog(TextVariables.HINT_SCALEPAGE))
                {
                    Hint.StartPosition = FormStartPosition.CenterParent;
                    Hint.ShowDialog();
                }
                UseStamps.Default.isFirstScaling = false;
                UseStamps.Default.Save();
            }
        }
        private void ButtonToFilePage2_Click(object sender, EventArgs e)
        {
            RestoreFilePage();
            _isAnyLoaded = true;
            LoadTempImages(_imageFlag);
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutFilePage);
            ResizeVisibleImagesToWindow();
        }
        private void ButtonToFilePage3_Click(object sender, EventArgs e)
        {
            ButtonToFilePage3.BackColor = Color.Black;
            ButtonToFilePage3.ForeColor = Color.White;
            RestoreFilePage();
            RootFunctions.LayoutDisable(LayoutFinalPage);
            ReplacePrimeImagesToDefault();
            SystemControl.FileControl.CreateTempImages("!DEFAULT!", TEMP_APPENDS, GAME_TYPES[_gameSelected].PlaceholderImage);
            LoadTempImages(_imageFlag);
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutFilePage);
            ResizeVisibleImagesToWindow();
            GenerateImageFlagString(100);
            ButtonToMainPageAndFolder.Enabled = true;
        }
        private void ButtonExit_Click(object sender, EventArgs e)
        {
            DisposePrimeImages();
            ClearImageLists(ListGallery, ImgListGallery);
            ClearImageLists(ListExtract, ImgListExtract);
            SystemControl.FileControl.ClearTempImages();
            Application.Exit();
        }
        private void ButtonToExtract_Click(object sender, EventArgs e)
        {
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutExtractPage);
            ButtonExtractAll.Enabled = false;
            ButtonExtractSelected.Enabled = false;
            ButtonOpenFolders.Enabled = false;

            if (UseStamps.Default.isFirstExtract == true)
            {
                using (MyMessageDialog Hint = new MyMessageDialog(TextVariables.HINT_EXTRACTPAGE))
                {
                    Hint.StartPosition = FormStartPosition.CenterParent;
                    Hint.ShowDialog();
                }
                UseStamps.Default.isFirstExtract = false;
                UseStamps.Default.Save();
            }
        }
        private void ButtonToGalleryPage_Click(object sender, EventArgs e)
        {
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutGallery);
            if (!LoadGallery(ACTIVE_PATHS[_gameSelected]))
            {
                ButtonToMainPage3_Click(sender, e);
                return;
            }

            if (UseStamps.Default.isFirstGallery == true)
            {
                using (MyMessageDialog Hint = new MyMessageDialog(TextVariables.HINT_GALLERYPAGE))
                {
                    Hint.StartPosition = FormStartPosition.CenterParent;
                    Hint.ShowDialog();
                }
                UseStamps.Default.isFirstGallery = false;
                UseStamps.Default.Save();
            }
        }
        private void ButtonToMainPage_Click(object sender, EventArgs e)
        {
            ReplacePrimeImagesToDefault();
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutMainPage);
        }
        private void ButtonToMainPage2_Click(object sender, EventArgs e)
        {
            _extractFolderPath = "!NONE!";
            _cancellationTokenSource?.Cancel();
            ClearImageLists(ListExtract, ImgListExtract);
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutMainPage);
        }
        private void ButtonToMainPage3_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            ClearImageLists(ListGallery, ImgListGallery);
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutMainPage);
        }
        private void ButtonToMainPage4_Click(object sender, EventArgs e)
        {
            RestoreFilePage();
            ButtonToMainPage4.BackColor = Color.Black;
            ButtonToMainPage4.ForeColor = Color.White;
            RootFunctions.LayoutDisable(LayoutFinalPage);
            ReplacePrimeImagesToDefault();
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutMainPage);
        }
        private void ButtonToMainPage5_Click(object sender, EventArgs e)
        {
            RootFunctions.LayoutDisable(LayoutSettingsPage);
            RootFunctions.LayoutEnable(LayoutMainPage);
            CoreSettings.Default.MaxWindowHeight = Height;
            CoreSettings.Default.MaxWindowWidth = Width;
            CoreSettings.Default.Save();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            ButtonValidatePath.Text = TextVariables.BUTTON_VALIDATE;
            ButtonValidatePath.BackColor = Color.Black;
            ButtonValidatePath.ForeColor = Color.White;
            ButtonValidatePath.Enabled = true;
            CenterToScreen();
        }
        private void ButtonToMainPage6_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            ListBoxCustom.Items.Clear();
            RootFunctions.LayoutDisable(LayoutCustom);
            RootFunctions.LayoutEnable(LayoutMainPage);
        }
        private void ButtonToSettingsPage_Click(object sender, EventArgs e)
        {
            RootFunctions.LayoutDisable(LayoutMainPage);
            TextBoxFullPath.Text = "";
            TextBoxFullPath.Text = ACTIVE_PATHS[_gameSelected];
            RootFunctions.LayoutEnable(LayoutSettingsPage);
            ButtonToMainPage5.ForeColor = Color.White;
            ButtonToMainPage5.BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.Sizable;
        }
        private void ButtonToCustomPortraits_Click(object sender, EventArgs e)
        {
            ParentLayoutsDisable();
            RootFunctions.LayoutEnable(LayoutCustom);
            if (!LoadCustomFolder(ACTIVE_PATHS[_gameSelected]))
            {
                ButtonToMainPage6_Click(sender, e);
                return;
            }

            if (UseStamps.Default.isFirstCustom == true)
            {
                using (MyMessageDialog Hint = new MyMessageDialog(TextVariables.HINT_CUSTOMNPC))
                {
                    Hint.StartPosition = FormStartPosition.CenterParent;
                    Hint.ShowDialog();
                }
                UseStamps.Default.isFirstCustom = false;
                UseStamps.Default.Save();
            }
        }
        private void MainForm_Closed(object sender, FormClosedEventArgs e)
        {
            DisposePrimeImages();
            ClearImageLists(ListGallery, ImgListGallery);
            ClearImageLists(ListExtract, ImgListExtract);
            SystemControl.FileControl.ClearTempImages();
            Dispose();
            Application.Exit();
        }

        private void ListBoxCustom_SelectedValueChanged(object sender, EventArgs e)
        {

        }
    }
}
