﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;

namespace ImageTilesExtractor
{
    public partial class Form1 : Form
    {
        Image g_image = null;
        private byte[] g_imgArray;    // Image Array To Process

        public Form1()
        {
            InitializeComponent();
        }

        Image PasteImage(Image image, bool bUseTransparentKey = false)
        {
            if (bUseTransparentKey)
            {
                Bitmap ret = (Bitmap)image;
                ret.MakeTransparent(Color.White);
                image = (Image)ret;
            }

            image.RotateFlip(RotateFlipType.Rotate180FlipX);

            return image;
        }

        /// <summary>
        /// BMP Type 1: starts from index 54, Type 2: starts from index 66.
        /// (Don't know difference yet)
        /// </summary>
        /// <returns></returns>
        private Image GetImageFromClipboard()
        {
            if (Clipboard.GetDataObject() == null) return null;
            if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Dib))
            {
                var dib = ((System.IO.MemoryStream)Clipboard.GetData(DataFormats.Dib)).ToArray();
                var width = BitConverter.ToInt32(dib, 4);
                var height = BitConverter.ToInt32(dib, 8);
                var bpp = BitConverter.ToInt16(dib, 14);
                if (bpp == 32)
                {
                    var gch = GCHandle.Alloc(dib, GCHandleType.Pinned);
                    Bitmap bmp = null;
                    try
                    {
                        var ptr = new IntPtr((long)gch.AddrOfPinnedObject() + 40);
                        // var ptr = new IntPtr((long)gch.AddrOfPinnedObject() + 52);
                        bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr);
                        return new Bitmap(bmp);
                    }
                    finally
                    {
                        gch.Free();
                        if (bmp != null) bmp.Dispose();
                    }
                }
            }
            return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
        }

        private void pasteImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage() == true)
            {
                Image image = GetImageFromClipboard();
                g_image = PasteImage(image);
                pictureBox1.Image = g_image;

                // TEST IMAGE PROCESSING HERE
                g_imgArray = ConvImageToByteArray(g_image);
            }
        }

        public byte[] ConvImageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }


        /// <summary>
        /// Find White Area, and return true if All White-Like
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xp">Base X Position</param>
        /// <returns></returns>
        public bool CheckVLinesToCutByV(byte[] img, int imgWidth, int imgHeight, int xp)
        {
            // BMP HEADER SIZE IS 54 BYTES
            int pos = 54;

            for (int y = 0; y < imgHeight; y++)
            {
                int idx = pos + (xp * 4) + (y * 4 * imgWidth);
                {
                    int[] xy = ColorCalc.GetImageXYFromIndex(imgWidth, imgHeight, idx);
                    bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx + 1], img[idx]);
                    if (tf == false)
                    {
                        //int[] xy = ColorCalc.GetImageXYFromIndex(imgWidth, imgHeight, i);
                        Console.WriteLine("Not White Found at ({0},{1})", xy[0], xy[1]);
                        return false;
                    }
                }
            }

            Console.WriteLine("[!] White Found at ({0},ALL)", xp);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xx"></param>
        /// <param name="yy">Clicked e.Y location</param>
        /// <returns></returns>
        public int GoDownForWhite(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {
            //int idx = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, xx, yy);
            //if (!ColorCalc.IsIndexOk(idx, imgWidth, imgHeight))
            //    return -1;

            Queue<int> que = new Queue<int>(5);

            Console.Write("Start YY: {0} | ", yy);
            //yy = ColorCalc.GetRealY(yy, imgHeight);
            //Console.WriteLine("Real Start YY: {0}", yy);

            //for (int y1 = yy; y1 >= 0; y1--)
            for (int y1 = yy; y1 < imgHeight; y1++)
            {
                int yRev = ColorCalc.GetRealY(y1, imgHeight);   // Actual Position in the Array
                int idx = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, xx, yRev);

                // evaluate it if index is ok 
                if (!ColorCalc.IsIndexOk(idx, imgWidth, imgHeight))
                    return -1;

                // COLOR CHECKING LOG PRINT.
                //Console.Write("(X,Y) ({0}, {1}='{2}') (OnlyFirstLineEffective)", xx, y1, yRev);
                //Console.WriteLine("Evaluating Y1: RGB {0}, {1}, {2}", img[idx+2], img[idx + 1], img[idx]);
                bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx + 1], img[idx]);

                if (tf)
                    que.Enqueue(y1);
                else
                    que.Clear();

                if (que.Count == 5)
                    return y1-5;  // 14/13/12/11/10 => 9 (is Picture Area's last Y)
            }

            //return -1;
            return imgHeight;
        }

        /// <summary>
        /// e.g. It should be White, White, WHite, White, White (if confirm up for 5 whites)
        /// if all whites : fine, some other colors: NG
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <returns>successful and how many whites</returns>
        int2 GoConfirmWhiteUp(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {
            int2 ret = new int2();
            ret.value = -1;
            ret.fine = false;

            Queue<int> que = new Queue<int>(5);

            for (int y1 = yy; y1 >= ((yy-5 < 0) ? 0:yy-5); y1--)
            {
                int yRev = ColorCalc.GetRealY(y1, imgHeight);   // Actual Position in the Array
                int idx = ColorCalc.GetValidIndex(imgWidth, imgHeight, xx, yRev);

                if (idx > -1)
                {
                    bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx + 1], img[idx]);
                    if (tf)
                        que.Enqueue(y1);
                    else
                    {
                        ret.fine = false;
                        ret.value = y1; // where the error met.
                        return ret;
                    }
                }

                if (que.Count == 5)
                {
                    ret.fine = true;
                    ret.value = y1;  // 14/13/12/11/10 => 9 (is Picture Area's last Y)
                    return ret;
                }
            }

            return ret;
        }



        /// <summary>
        /// 
        /// This is GoUpForWhite, whereas Non-White, Non-White, Non-White, and if found White, 4 strait White more.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <returns></returns>
        public int GoUpForWhite(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {
            Queue<int> que = new Queue<int>(5);

            for (int y1=yy; y1>=0; y1--)
            {
                //int idx = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, xx, yRev);

                int yRev = ColorCalc.GetRealY(y1, imgHeight);   // Actual Position in the Array
                int idx = ColorCalc.GetValidIndex(imgWidth, imgHeight, xx, yRev);

                if (idx > -1)
                {
                    bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx + 1], img[idx]);
                    if (tf)
                        que.Enqueue(y1);
                    else
                        que.Clear();
                }
                else
                {

                }

                //if (que.Count == 5)
                //    return y1 + 5;  // 14/13/12/11/10 => 9 (is Picture Area's last Y)
            }

            //return -1;
            return 0;
        }


        /// <summary>
        /// xx should be 'white' (start position should be)
        /// finding a new right(x) of non-white
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <returns></returns>
        public int GoRightForFore(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {
            int yRev = ColorCalc.GetRealY(yy, imgHeight);   // Actual Position in the Array
            //int idx0 = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, xx, yRev);
            //if (!ColorCalc.IsIndexOk(idx0, imgWidth, imgHeight))
            //    return -1;
            //bool tf0 = ColorCalc.IsWhiteLike2(img[idx0 + 2], img[idx0 + 1], img[idx0]);
            //if (!tf0)
            //    return -1;  // if Not White-like, there is a problem because, it should find the first non-white.

            for (int i=xx; i<imgWidth; i++)
            {
                int idx = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, i, yRev);

                bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx+ 1], img[idx]);
                if (!tf)
                    return i;   // new x value (new right position of non-white to re-start for 5 whites)
            }

            return -1;
        }



        /// <summary>
        /// Go Right until meet the end of x (width).
        /// Non-white, white, white, white, white, white...
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <returns></returns>
        public int GoRightForWhite(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {
            Queue<int> que = new Queue<int>(5);

            Console.Write("Start_R XX: {0} | ", xx+1);

            // Check the Source Point, and iterate for the 5 whites.
            int yRev = ColorCalc.GetRealY(yy, imgHeight);   // Actual Position in the Array
            int idx0 = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, xx, yRev);
            if (!ColorCalc.IsIndexOk(idx0, imgWidth, imgHeight))
                return -1;
            bool tf0 = ColorCalc.IsWhiteLike2(img[idx0 + 2], img[idx0 + 1], img[idx0]);
            if (tf0)
                return -1;  // if White-like, -1

            for (int x1 = xx+1; x1 < imgWidth; x1++)
            {
                int idx = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, x1, yRev);

                // evaluate it if index is ok 
                if (!ColorCalc.IsIndexOk(idx, imgWidth, imgHeight))
                    return -1;

                //Console.Write("(X,Y) ({0}, {1}='{2}') (OnlyFirstLineEffective)", x1, yRev, x1);
                //Console.WriteLine("Evaluating Y1: RGB {0}, {1}, {2}", img[idx + 2], img[idx + 1], img[idx]);
                bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx + 1], img[idx]);

                if (tf)
                    que.Enqueue(x1);
                else
                    que.Clear();

                if (que.Count == 5)
                    return x1 - 5;  // 14/13/12/11/10 => 9 (is Picture Area's last Y = No White)
            }

            //return -1;  // Effective Picture Area, to the very end.
            return imgWidth;
        }

        public int GoLeftForWhite(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {
            Queue<int> que = new Queue<int>(5);

            Console.Write("Start XX: {0} | ", xx);

            //for (int x1 = xx; x1 < imgWidth; x1++)
            for (int x1=xx; x1>=0; x1--)
            {
                int yRev = ColorCalc.GetRealY(yy, imgHeight);   // Actual Position in the Array
                int idx = ColorCalc.GetIndexFromXY(imgWidth, imgHeight, x1, yRev);

                // evaluate it if index is ok 
                if (!ColorCalc.IsIndexOk(idx, imgWidth, imgHeight))
                    return -1;

                //Console.Write("(X,Y) ({0}, {1}='{2}') (OnlyFirstLineEffective)", x1, yRev, x1);
                //Console.WriteLine("Evaluating Y1: RGB {0}, {1}, {2}", img[idx + 2], img[idx + 1], img[idx]);
                bool tf = ColorCalc.IsWhiteLike2(img[idx + 2], img[idx + 1], img[idx]);

                if (tf)
                    que.Enqueue(x1);
                else
                    que.Clear();

                if (que.Count == 5)
                    return x1 + 5;  // 14/13/12/11/10 => 9 (is Picture Area's last Y = No White)
            }

            //return -1;  // Effective Picture Area, to the very end. (or que is not enough)
            return 0;
        }

        /// <summary>
        /// the clicked point and down, up, left, right
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="left_x"></param>
        /// <param name="right_x"></param>
        /// <returns></returns>
        Image CreateImageWithDrawnLine(int x, int y, int left_x, int right_x, int top, int bottom)
        {
            int width = pictureBox1.Image.Width;
            int height = pictureBox1.Image.Height;
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var gg = Graphics.FromImage(bmp))
            {
                gg.FillRectangle(Brushes.Transparent, new Rectangle(0, 0, width, height));

                gg.DrawLine(Pens.Green, x, y, left_x, y);
                gg.DrawLine(Pens.Red, x, y, right_x, y);

                gg.DrawLine(Pens.Green, x, y, x, top);
                gg.DrawLine(Pens.Red, x, y, x, bottom);
            }

            return (Image)bmp;
        }

        struct int2
        {
            public bool fine; // successful or not
            public int value; // value
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <returns>fine : background colors all around, NG: some foreground colors. value: position to re-start</returns>
        int2 TryGoRight(byte[] img, int imgWidth, int imgHeight, int xx, int yy)
        {   //(g_imgArray, g_image.Width, g_image.Height, e.X, e.Y);
            int2 ret = new int2();

            ret.fine = true;
            ret.value = -1;

            int right = GoRightForWhite(img, imgWidth, imgHeight, xx, yy);
            ret.value = right;

            if (right == imgWidth)
            {
                ret.fine = false;
                return ret;
            }

            Console.WriteLine("445: Clicked X:{0}, RIght(from clicked):{1}", xx, right);
            // IS IT REAL RIGHT? CHECK VERTICALLY, UP AND DOWN (Try 3 times. i<3)
            for (int i=0; i<3; i++)
            {
                int2 v1 = GoConfirmWhiteUp(img, imgWidth, imgHeight, right, yy);
                if (v1.fine)
                {
                    ret.value = right;  // position to re-start
                    ret.fine = true;   // it has foreground.
                    return ret;
                }
                else   // if met some foreground colors (black, red...).
                {
                    ret.value = right;  // position to re-start
                    ret.fine = false;   // it has foreground.
                }

                //// NON-EFFECTIVE RIGHT X, BECAUSE OF SOME TOP-DOWN NON-WHITES
                Console.Write("old right...{0}, ", right);

                right = GoRightForFore(img, imgWidth, imgHeight, right + 1, yy);
                Console.WriteLine(" new right : {0}", right);
                if (right == -1)
                {
                    ret.fine = false; // because no fore any more
                    ret.value = -1;
                    return ret;
                }
                else
                {
                    //Console.WriteLine(" new right : {0}", right);
                }
            }

            return ret;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //int ey = ColorCalc.GetRealY(e.Y, g_image.Height);

                int bottom = GoDownForWhite(g_imgArray, g_image.Width, g_image.Height, e.X, e.Y);
                int top = GoUpForWhite(g_imgArray, g_image.Width, g_image.Height, e.X, e.Y);
                int right = GoRightForWhite(g_imgArray, g_image.Width, g_image.Height, e.X, e.Y);
                int left = GoLeftForWhite(g_imgArray, g_image.Width, g_image.Height, e.X, e.Y);

                Console.WriteLine("Clicked ({0}, {1}) => Down for White: {2}", e.X, e.Y, bottom);
                Console.WriteLine("{0}, {1} => Top for White: {2}", e.X, e.Y, top);
                Console.WriteLine("{0}, {1} => Right for White: {2}", e.X, e.Y, right);
                Console.WriteLine("{0}, {1} => Left for White: {2}", e.X, e.Y, left);

                int2 right2 = TryGoRight(g_imgArray, g_image.Width, g_image.Height, e.X, e.Y);
                //if (right2.fine)
                {
                    Console.WriteLine("{0}, {1} => Right2 for White: {2}", e.X, e.Y, right);
                }


                // Clipboard Copy
                Clipboard.SetImage(pictureBox1.Image);

                MessageBox.Show("Click For Line");
                Clipboard.SetImage(CreateImageWithDrawnLine(e.X, e.Y, left, right, top, bottom));
            }
        }

    }
}
