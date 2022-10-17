﻿using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace SystemControl
{
    public class FileControl
    {
        public static string OpenFile()
        {
            string fullPath;
            OpenFileDialog OpenFileDialog = new OpenFileDialog()
            {
                Title = "Choose image",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                SupportMultiDottedExtensions = false,
                Filter = "Image files|*.jpg; *.jpeg; *.gif; *.bmp; *.png",
            };
            if (OpenFileDialog.ShowDialog() == DialogResult.OK)
                fullPath = OpenFileDialog.FileName;
            else
                fullPath = "-1";
            OpenFileDialog.Dispose();
            return fullPath;
        }
        public static void CreateTemp(string fullPath, string newFullpathFull, string newFullpathPoor)
        {
            if (!Directory.Exists("temp/")) 
                Directory.CreateDirectory("temp/");
            if (fullPath == "-1")
            {
                using (Image img = new Bitmap(PathfinderKINGPortrait.Properties.Resources.fulldefault))
                {
                    img.Save(newFullpathFull);
                    img.Save(newFullpathPoor);
                }
            }
            else
            {
                using (Image img = new Bitmap(fullPath))
                {
                    img.Save(newFullpathFull);
                    ImageControl.Wraps.CreatePoorImage(img, newFullpathPoor);
                }
            }
        }
        public static void TempClear()
        {
            try
            {
                if (Directory.Exists("temp/")) 
                    Directory.Delete("temp/", true);
            }
            catch (System.IO.IOException)
            {
                return;
            }
        }
        public static bool DirExists(string path)
        {
            if (Directory.Exists(path))
                return true;
            else
                return false;
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
    }
}