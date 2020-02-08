using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    class PieceCutter
    {
        public static Bitmap CutOut(Bitmap uncutPiece, GraphicsPath path)
        {
            Bitmap piece = new Bitmap(uncutPiece.Width, uncutPiece.Height); //vytvori sa prazdny bitmap velkosti obrazka na orezanie

            using (Graphics draw = Graphics.FromImage(piece)) //pouzije sa plocha bitmapu piece, vpise sa do nej neorezany obrazok, a vytvori sa ohranicenie pomocou uz predpripravenej krivky
            {
                draw.InterpolationMode = InterpolationMode.High;
                draw.ResetClip();
                draw.SetClip(path);
                draw.DrawImage(uncutPiece, new Point(0, 0));
            }

            using (Graphics draw = Graphics.FromImage(piece))
            {
                draw.SmoothingMode = SmoothingMode.AntiAlias;
                draw.DrawPath(new Pen(Color.Gray, 1.5F), path);
            }

            return piece;
        }

        public static Bitmap SetClickEffect(Bitmap source) //Vytvori zvyraznenu verziu kuska, napr. pri kliknuti mysou
        {
            Bitmap image = new Bitmap(source);
            Rectangle recTmp = new Rectangle(0, 0, image.Width, image.Height);

            // Lock bitmap bits
            System.Drawing.Imaging.BitmapData imageData = image.LockBits(
                recTmp,
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                image.PixelFormat);

            //adresa prveho riadka, pixelu
            IntPtr pointerFirstRowOfImage = imageData.Scan0;

            //bajty(bytes) bitmap-y v poli
            int bytesCount = Math.Abs(imageData.Stride) * image.Height; //pocet bitov v bitmape
            byte[] rgbaHodnoty = new byte[bytesCount];

            //prekopirovat RGBA hodnoty obrazka do pola
            System.Runtime.InteropServices.Marshal.Copy(pointerFirstRowOfImage, rgbaHodnoty, 0, bytesCount);

            //-----------------------------------------------------------------------
            //hlavna cast funkcie - nastavenie vybranych pixelov na pozadovanu farbu
            //prvy byte == red
            //druhy byte == green
            //treti byte == blue
            //stvrty byte == alfa kanal transparetnost
            //chceme aby pri kliknuti obrazok nastal Sepia efekt

            byte sephiaValue;
            int i = 0;

            while (i < bytesCount)
            {
                sephiaValue = (byte)(0.299 * rgbaHodnoty[i] + 0.587 * rgbaHodnoty[i+1] + 0.114 * rgbaHodnoty[i+2]);
                
                //pouzijeme skrateny zapis if-else statement
                rgbaHodnoty[i] = (byte)((sephiaValue > 206) ? 255 : sephiaValue + 49);
                rgbaHodnoty[i + 1] = (byte)((sephiaValue < 14) ? 0 : sephiaValue - 14);
                rgbaHodnoty[i + 2] = (byte)((sephiaValue < 56) ? 0 : sephiaValue - 56);
                i += 4;
            }
            
            //-----------------------------------------------------------------------

            //prekopirovat RGBA hodnoty spat do bitmap-y
            System.Runtime.InteropServices.Marshal.Copy(rgbaHodnoty, 0, pointerFirstRowOfImage, bytesCount);

            //Unlock bits
            image.UnlockBits(imageData);

            return image;
        }
    }
}
