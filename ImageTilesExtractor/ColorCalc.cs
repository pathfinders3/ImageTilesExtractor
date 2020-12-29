
using System;

namespace ImageTilesExtractor
{
    public class ColorCalc
    {
        /// <summary>
        /// get x,y coord of image by 1d index
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="ii"></param>
        /// <returns></returns>
        static public int[] GetImageXYFromIndex(int width, int height, int ii)
        {
            int i2 = (ii - 54);
            int i3 = i2 / 4;
            int yy = i3 / width;
            int xx = i3 % width;

            yy = GetRealY(yy, height);

            int[] ret = new int[] { xx, yy };

            return ret;
        }

        /// <summary>
        /// get image index from x,y coord.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <returns></returns>
        static public int GetIndexFromXY(int width, int height, int xx, int yy)
        {
            int pos = 54;
            int x2 = xx * 4;
            int y2 = yy * width * 4;

            int ret = pos + x2 + y2;

            return ret;
        }

        /// <summary>
        /// Check index boundary is ok
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        static public bool IsIndexOk(int idx, int width, int height)
        {
            int pos = 54;
            int x2 = (width-1) * 4;
            int y2 = (height-1) * width * 4;
            int ret = pos + x2 + y2;

            bool tf1 = (idx > -1);
            bool tf2 = (idx <= ret);

            return tf1 && tf2;
        }


        static public int GetRealY(int yy, int height)
        {
            return height - 1 - yy;
        }

        static public bool IsWhiteLike2(int rr, int gg, int bb)
        {
            uint col1 = CIEColorDiff.RGBToUInt(rr, gg, bb);
            uint col2 = CIEColorDiff.RGBToUInt(255, 255, 255);
            int diff1 = CIEColorDiff.Get94DeltaEDiff(col1, col2);

            if (diff1 <= 25)
                return true;

            //Console.WriteLine("5000) {0},{1},{2} is WhiteLike? diff: {3}", rr, gg, bb, diff1);

            return false;
        }
        /*
            public static bool IsSimilarColor(uint col1, uint col2, int tol = 25)
                int diff = Get94DeltaEDiff(col1, col2);

            public static int Get94DeltaEDiff(uint col1, uint col2)

                byte[] rgb1 = GetRGB24Hex(col1);
                byte[] rgb2 = GetRGB24Hex(col2);

                XYZ xyz1 = RGBtoXYZ(r1, g1, b1);
                XYZ xyz2 = RGBtoXYZ(r2, g2, b2);

                LAB lab1 = XYZtoLAB(xyz1.X, xyz1.Y, xyz1.Z);
                LAB lab2 = XYZtoLAB(xyz2.X, xyz2.Y, xyz2.Z);
         */
    }

}