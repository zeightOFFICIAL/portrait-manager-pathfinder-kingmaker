﻿using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace SystemControl
{
    public class FileControl
    {
        public static string OpenFileLocation()
        {
            string fullPath;
            OpenFileDialog OpenFileDialog = new OpenFileDialog()
            {
                Title = PathfinderPortraitManager.Properties.TextVariables.openFileTitle,
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                SupportMultiDottedExtensions = false,
                Filter = PathfinderPortraitManager.Properties.TextVariables.imageFilter+"|*.jpg; *.jpeg; *.gif; *.bmp; *.png",
            };
            if (OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                fullPath = OpenFileDialog.FileName;
            }
            else
            {
                fullPath = "-1";
            }
            OpenFileDialog.Dispose();
            return fullPath;
        }
        public static void TempImagesCreate(string newImagePath, string tempPathFull, string tempPathPoor, Image defaultImg)
        {
            if (!DirectoryExists("temp/"))
                DirectoryCreate("temp/");
            if (newImagePath == "-1")
            {
                using (Image img = new Bitmap(defaultImg))
                {
                    img.Save(tempPathFull);
                    img.Save(tempPathPoor);
                }
            }
            else
            {
                using (Image img = new Bitmap(newImagePath))
                {
                    img.Save(tempPathFull);
                    ImageControl.Wraps.CreatePoorImage(img, tempPathPoor);
                }
            }
        }
        public static void TempImagesClear()
        {
            DirectoryDeleteRecursive("temp/");
        }
        public static void DirectoryDeleteRecursive(string path)
        {
            try
            {
                if (DirectoryExists(path))
                    Directory.Delete(path, true);
            }
            catch (IOException)
            {
                return;
            }
        }
        public static bool DirectoryExists(string path)
        {
            if (Directory.Exists(path))
                return true;
            else
                return false;
        }
        public static void DirectoryCreate(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (IOException)
            {
                return;
            }
        }
        public static bool FileExist(string path, string filename)
        {
            if (File.Exists(path + filename))
                return true;
            else
                return false;
        }
        public static string GetFileExtension(string path, string filename)
        {
            return Path.GetExtension(path + filename);
        }
        public static PrivateFontCollection InitCustomFont(byte[] font)
        {
            PrivateFontCollection pfc = new PrivateFontCollection();
            int fontLength = font.Length;
            byte[] fontData = font;
            System.IntPtr data = Marshal.AllocCoTaskMem(fontLength);
            Marshal.Copy(fontData, 0, data, fontLength);
            pfc.AddMemoryFont(data, fontLength);
            return pfc;
        }
    }
}