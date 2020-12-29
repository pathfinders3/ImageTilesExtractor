using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTilesExtractor
{
    public class CIEColorDiff
    {
        public static System.Drawing.Color UIntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return System.Drawing.Color.FromArgb(a, r, g, b);
        }

        static public uint ColorToUInt(System.Drawing.Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) |
                          (color.G << 8) | (color.B << 0));
        }

        /// <summary>
        /// Integer R,G,B to UINT
        /// </summary>
        /// <param name="rr"></param>
        /// <param name="gg"></param>
        /// <param name="bb"></param>
        /// <returns></returns>
        static public uint RGBToUInt(int rr, int gg, int bb)
        {
            int aa = 255;
            return (uint)((aa << 24) | (rr << 16) |
                          (gg << 8) | (bb << 0));
        }


        public static int Get94DeltaEDiff(uint col1, uint col2)
        {
            byte[] rgb1 = GetRGB24Hex(col1);
            byte[] rgb2 = GetRGB24Hex(col2);

            int r1 = rgb1[2];
            int g1 = rgb1[1];
            int b1 = rgb1[0];

            int r2 = rgb2[2];
            int g2 = rgb2[1];
            int b2 = rgb2[0];


            XYZ xyz1 = RGBtoXYZ(r1, g1, b1);
            XYZ xyz2 = RGBtoXYZ(r2, g2, b2);

            LAB lab1 = XYZtoLAB(xyz1.X, xyz1.Y, xyz1.Z);
            LAB lab2 = XYZtoLAB(xyz2.X, xyz2.Y, xyz2.Z);

            var deltaL = lab1.L - lab2.L;
            var deltaA = lab1.A - lab2.A;
            var deltaB = lab1.B - lab2.B;

            var c1 = Math.Sqrt(Math.Pow(lab1.A, 2) + Math.Pow(lab1.B, 2));
            var c2 = Math.Sqrt(Math.Pow(lab2.A, 2) + Math.Pow(lab2.B, 2));

            var deltaC = c1 - c2;

            var deltaH = Math.Pow(deltaA, 2) + Math.Pow(deltaB, 2) - Math.Pow(deltaC, 2);
            deltaH = deltaH < 0 ? 0 : Math.Sqrt(deltaH);

            const double sl = 1.0;
            const double kc = 1.0;
            const double kh = 1.0;

            double Kl = 1.0;
            double K1 = .045;
            double K2 = .015;

            double sc = 1.0 + K1 * c1;
            double sh = 1.0 + K2 * c1;
            //double sh = 1.0 + K2 * c2;

            var i = Math.Pow(deltaL / (Kl * sl), 2) +
                            Math.Pow(deltaC / (kc * sc), 2) +
                            Math.Pow(deltaH / (kh * sh), 2);
            var finalResult = i < 0 ? 0 : Math.Sqrt(i);

            return Convert.ToInt16(Math.Round(finalResult));
        }


        public static byte[] GetRGB24Hex(uint val)
        {
            byte[] values = BitConverter.GetBytes(val);

            byte rr = values[2];
            byte gg = values[1];
            byte bb = values[0];

            return values;
        }

        public struct XYZ
        {
            public double X;
            public double Y;
            public double Z;
        }

        public struct LAB
        {
            public double L;
            public double A;
            public double B;
        }

        public static XYZ RGBtoXYZ(int RVal, int GVal, int BVal)
        {
            double R = Convert.ToDouble(RVal) / 255.0;       //R from 0 to 255
            double G = Convert.ToDouble(GVal) / 255.0;       //G from 0 to 255
            double B = Convert.ToDouble(BVal) / 255.0;       //B from 0 to 255

            if (R > 0.04045)
            {
                R = Math.Pow(((R + 0.055) / 1.055), 2.4);
            }
            else
            {
                R = R / 12.92;
            }
            if (G > 0.04045)
            {
                G = Math.Pow(((G + 0.055) / 1.055), 2.4);
            }
            else
            {
                G = G / 12.92;
            }
            if (B > 0.04045)
            {
                B = Math.Pow(((B + 0.055) / 1.055), 2.4);
            }
            else
            {
                B = B / 12.92;
            }

            R = R * 100;
            G = G * 100;
            B = B * 100;

            XYZ ret = new XYZ();

            //Observer. = 2°, Illuminant = D65
            ret.X = R * 0.4124 + G * 0.3576 + B * 0.1805;
            ret.Y = R * 0.2126 + G * 0.7152 + B * 0.0722;
            ret.Z = R * 0.0193 + G * 0.1192 + B * 0.9505;

            return ret;
        }


        public static LAB XYZtoLAB(double X, double Y, double Z)
        {
            // based upon the XYZ - CIE-L*ab formula at easyrgb.com (http://www.easyrgb.com/index.php?X=MATH&H=07#text7)
            double ref_X = 95.047;
            double ref_Y = 100.000;
            double ref_Z = 108.883;

            double var_X = X / ref_X;         // Observer= 2°, Illuminant= D65
            double var_Y = Y / ref_Y;
            double var_Z = Z / ref_Z;

            if (var_X > 0.008856)
            {
                var_X = Math.Pow(var_X, (1 / 3.0));
            }
            else
            {
                var_X = (7.787 * var_X) + (16 / 116.0);
            }
            if (var_Y > 0.008856)
            {
                var_Y = Math.Pow(var_Y, (1 / 3.0));
            }
            else
            {
                var_Y = (7.787 * var_Y) + (16 / 116.0);
            }
            if (var_Z > 0.008856)
            {
                var_Z = Math.Pow(var_Z, (1 / 3.0));
            }
            else
            {
                var_Z = (7.787 * var_Z) + (16 / 116.0);
            }

            LAB ret = new LAB();

            ret.L = (116 * var_Y) - 16;
            ret.A = 500 * (var_X - var_Y);
            ret.B = 200 * (var_Y - var_Z);

            return ret;
        }

    }
}
