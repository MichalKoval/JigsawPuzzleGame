using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    /// <summary>
    /// Pomocne metody pre spravu Bitmap-v, pouzite v GridLayer(mriezke), alebo pri vystrohovani puzzle kuskov
    /// </summary>

    class PictureEditor
    {
        // pouzite pri vystrihovani kuskov, vystrihovani puzzle obrazka zo zdrojoveho obrazka
        public static Bitmap CropImage(Bitmap image, Point startPoint, Point endPoint)
        {
            int width = Math.Abs(startPoint.X - endPoint.X);
            int height = Math.Abs(startPoint.Y - endPoint.Y);

            Rectangle cropArea = new Rectangle(startPoint.X, startPoint.Y, width, height);
            Bitmap croppedImage = image.Clone(cropArea, image.PixelFormat);

            return croppedImage;
        }
        
        // pouzite pri prekreslovani mriezky pre vyber puzzle obrazka
        public static Bitmap DrawGridIntoImage(double scale, Size pieceDimensions,
                                               int gridWidth, int gridHeight,
                                               Bitmap backGround, Size backGroundSize)
        {
            int scaledPieceWidth = (int)(pieceDimensions.Width * scale);
            int scaledPieceHeight = (int)(pieceDimensions.Height * scale);
            Bitmap grid = new Bitmap(backGroundSize.Width, backGroundSize.Height);
            
            Bitmap gridImage = new Bitmap(scaledPieceWidth * gridWidth + 2, scaledPieceHeight * gridHeight + 2);

            Rectangle area = new Rectangle(0, 0, backGroundSize.Width, backGroundSize.Height);

            Rectangle areaRct = new Rectangle(1, 1, scaledPieceWidth * gridWidth, scaledPieceHeight * gridHeight);

            Rectangle areaCutOut = new Rectangle((int)((backGroundSize.Width - gridImage.Width) / 2),
                                                 (int)((backGroundSize.Height - gridImage.Height) / 2),
                                                 scaledPieceWidth * gridWidth,
                                                 scaledPieceHeight * gridHeight);
            Pen pen;

            //Point[] p = new Point[5];
            //p[0] = new Point(3, 3);
            //p[1] = new Point(scaledPieceWidth * gridWidth - 3, 3);                                      // p0-------p1 p0 == p4
            //p[2] = new Point(scaledPieceWidth * gridWidth - 3, scaledPieceHeight * gridHeight - 3);     // |         |
            //p[3] = new Point(3, scaledPieceHeight * gridHeight - 3);                                    // |         |
            //p[4] = p[0];                                                                                // p3-------p2
            
            using (Graphics draw = Graphics.FromImage(gridImage))
            {
                //draw.DrawImage(transparentImage, area, new Rectangle(new Point(0, 0), transparentImage.Size), GraphicsUnit.Pixel);
                //draw.DrawImage(backGround, area, new Rectangle(new Point(0, 0), backGround.Size), GraphicsUnit.Pixel);

                pen = new Pen(Color.Silver, 1.8F);    
                
                Point pointS = new Point(0, 1);
                Point pointE = new Point(0, scaledPieceHeight * gridHeight + 1);
                
                for (int i = 1; i < gridWidth; i++)
                {
                    pointS.X = i * scaledPieceWidth;
                    pointE.X = i * scaledPieceWidth;
                    draw.DrawLine(pen, pointS, pointE);
                }

                pointS = new Point(1, 0);
                pointE = new Point(scaledPieceWidth * gridWidth + 1, 0);

                for (int i = 1; i < gridHeight; i++)
                {
                    pointS.Y = i * scaledPieceHeight;
                    pointE.Y = i * scaledPieceHeight;
                    draw.DrawLine(pen, pointS, pointE);
                }

                pen = new Pen(Color.White, 1.9F);
                //pen = new Pen(Color.FromArgb(64,64,64), 1.8F);
                draw.DrawRectangle(pen, areaRct);
            }

            using (Graphics draw = Graphics.FromImage(grid))
            {
                draw.DrawImage(backGround, area, new Rectangle(new Point(0, 0), backGround.Size), GraphicsUnit.Pixel);
                draw.FillRectangle(new SolidBrush(Color.Green), areaCutOut);
            }
            grid.MakeTransparent(Color.Green);

            using (Graphics draw = Graphics.FromImage(grid))
            {
                draw.DrawImage(gridImage, (int)((backGroundSize.Width - gridImage.Width) / 2) - 1,
                                          (int)((backGroundSize.Height - gridImage.Height) / 2) - 1);
            }
            return grid;
        }

        // nastavuje priehladnost bitmap-u
        public static Bitmap ImageOpacity(float valueOfOpacity, Bitmap sourceImage)
        //zmena alfa kanalu obrazka, priehladnost obrazka
        {
            Bitmap changedImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            Rectangle area = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
            ColorMatrix cMatrix = new ColorMatrix();
            ImageAttributes imAttributes = new ImageAttributes();

            using (Graphics draw = Graphics.FromImage(changedImage))
            {
                cMatrix.Matrix33 = valueOfOpacity;  // Matrix33 znamena zmena alfa kanalu na kazdom tretom stlpci a kazdom tretom riadku v matici, indexujeme od nuly, slo by to aj cyklom azmenit kazdy treti bajt
                imAttributes.SetColorMatrix(cMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                draw.DrawImage(sourceImage, area, 0, 0,
                               sourceImage.Width,
                               sourceImage.Height,
                               GraphicsUnit.Pixel,
                               imAttributes);
                //nemusim dispose-vat kedze using zahodi graphic draw
            }

            return changedImage;
        }

        // pouzite pri vykresleni tmavej priehladnej oblasti okolo mriezky
        public static Bitmap ColouredBackGroundImage(Color clr, Size dimensions)
        {
            Bitmap clrImage = new Bitmap(dimensions.Width, dimensions.Height);
            Rectangle area = new Rectangle(0, 0, dimensions.Width, dimensions.Height);
            Brush brush = new SolidBrush(clr);

            using (Graphics draw = Graphics.FromImage(clrImage))
            {
                draw.FillRectangle(brush, area);
            }

            return clrImage;
        }

        // scaling obrazka podla ratio, toto zmeni rozlisenie obrazka
        public static Bitmap ImageScale(double scaleValue, Bitmap sourceImage) 
        {
            int scaledWidth = (int)(sourceImage.Size.Width * scaleValue);
            int scaledHeight = (int)(sourceImage.Size.Height * scaleValue);

            Bitmap scaledImage = new Bitmap(scaledWidth, scaledHeight);
            Rectangle scaledImageArea = new Rectangle(0, 0, scaledWidth, scaledHeight);
            Rectangle sourceImageArea = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);

            using (Graphics draw = Graphics.FromImage(scaledImage))
            {
                draw.DrawImage(sourceImage, scaledImageArea, sourceImageArea, GraphicsUnit.Pixel);
            }

            return scaledImage;
        }
    }
}
