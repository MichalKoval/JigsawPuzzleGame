using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    class PuzzleBezierovaKrivka
    {
        #region Konstanty
        private const double ctrlPortionLength1 = 0.135; 
        private const double ctrlPortionWidth1 = -0.0357;
        private const double ctrlPortionLength2 = 0.3760; //0.380 
        private const double ctrlPortionWidth2 = -0.0484;
        private const double endpPortionLength1 = 0.4010;  //0.403
        private const double endpPortionWidth1 = -0.0412; 

        private const double ctrlPortionLength3 = 0.4025;  //0.403  
        private const double ctrlPortionWidth3 = -0.0412;  
        private const double ctrlPortionLength4 = 0.4492;  //0.450
        private const double ctrlPortionWidth4 = -0.02;
        private const double endpPortionLength2 = 0.3932;  //0.394
        private const double endpPortionWidth2 = 0.06; 

        private const double ctrlPortionLength5 = 0.3832;  //0.383
        private const double ctrlPortionWidth5 = 0.119;
        private const double ctrlPortionLength6 = 0.443;  //0.442
        private const double ctrlPortionWidth6 = 0.162;
        private const double endpPortionLength3 = 0.5;  //MIDDLE
        private const double endpPortionWidth3 = 0.163;

        private const double ctrlPortionLength7 = 0.562;  //0.558
        private const double ctrlPortionWidth7 = 0.162;
        private const double ctrlPortionLength8 = 0.621;  //0.617
        private const double ctrlPortionWidth8 = 0.119;
        private const double endpPortionLength4 = 0.610;  //0.606
        private const double endpPortionWidth4 = 0.06;  

        private const double ctrlPortionLength9 = 0.555;  //0.550 
        private const double ctrlPortionWidth9 = -0.02;
        private const double ctrlPortionLength10 = 0.603; //0.597 
        private const double ctrlPortionWidth10 = -0.0412; 
        private const double endpPortionLength5 = 0.603;  //0.597
        private const double endpPortionWidth5 = -0.0412;

        private const double ctrlPortionLength11 = 0.623; //0.620
        private const double ctrlPortionWidth11 = -0.0484;
        private const double ctrlPortionLength12 = 0.865;
        private const double ctrlPortionWidth12 = -0.0357;

        #endregion

        // funkcia vygeneruje body pre nasledne vykreslenie krivky zvysleho zubka, smerujuceho bud do vnutra alebo von
        public static Point[] VerticalPoints(Point startPoint, Point endPoint, bool flip) //static-->nechcem vytvarat instancie triedy, je to iba nastroj
        {
            int length = (startPoint.Y - endPoint.Y); //vzdialenost medzi bodmi vertikalne
            Point[] points = new Point[19];
            int k, l;

            if (length >= 0) { k = (-1); }
            else
            {
                length *= (-1);
                k = 1;
            }

            if (flip) { l = 1; }
            else { l = (-1); }
                      
            #region Set Points

            points[0] = startPoint;
            points[1].X = startPoint.X + (int)(l * length * ctrlPortionWidth1);
            points[1].Y = startPoint.Y + (int)(k * length * ctrlPortionLength1);
            points[2].X = startPoint.X + (int)(l * length * ctrlPortionWidth2);
            points[2].Y = startPoint.Y + (int)(k * length * ctrlPortionLength2);
            points[3].X = startPoint.X + (int)(l * length * endpPortionWidth1);
            points[3].Y = startPoint.Y + (int)(k * length * endpPortionLength1);
            points[4].X = startPoint.X + (int)(l * length * ctrlPortionWidth3);
            points[4].Y = startPoint.Y + (int)(k * length * ctrlPortionLength3);
            points[5].X = startPoint.X + (int)(l * length * ctrlPortionWidth4);
            points[5].Y = startPoint.Y + (int)(k * length * ctrlPortionLength4);
            points[6].X = startPoint.X + (int)(l * length * endpPortionWidth2);
            points[6].Y = startPoint.Y + (int)(k * length * endpPortionLength2);
            points[7].X = startPoint.X + (int)(l * length * ctrlPortionWidth5);
            points[7].Y = startPoint.Y + (int)(k * length * ctrlPortionLength5);
            points[8].X = startPoint.X + (int)(l * length * ctrlPortionWidth6);
            points[8].Y = startPoint.Y + (int)(k * length * ctrlPortionLength6);
            points[9].X = startPoint.X + (int)(l * length * endpPortionWidth3);
            points[9].Y = startPoint.Y + (int)(k * length * endpPortionLength3);
            points[10].X = startPoint.X + (int)(l * length * ctrlPortionWidth7);
            points[10].Y = startPoint.Y + (int)(k * length * ctrlPortionLength7);
            points[11].X = startPoint.X + (int)(l * length * ctrlPortionWidth8);
            points[11].Y = startPoint.Y + (int)(k * length * ctrlPortionLength8);
            points[12].X = startPoint.X + (int)(l * length * endpPortionWidth4);
            points[12].Y = startPoint.Y + (int)(k * length * endpPortionLength4);
            points[13].X = startPoint.X + (int)(l * length * ctrlPortionWidth9);
            points[13].Y = startPoint.Y + (int)(k * length * ctrlPortionLength9);
            points[14].X = startPoint.X + (int)(l * length * ctrlPortionWidth10);
            points[14].Y = startPoint.Y + (int)(k * length * ctrlPortionLength10);
            points[15].X = startPoint.X + (int)(l * length * endpPortionWidth5);
            points[15].Y = startPoint.Y + (int)(k * length * endpPortionLength5);
            points[16].X = startPoint.X + (int)(l * length * ctrlPortionWidth11);
            points[16].Y = startPoint.Y + (int)(k * length * ctrlPortionLength11);
            points[17].X = startPoint.X + (int)(l * length * ctrlPortionWidth12);
            points[17].Y = startPoint.Y + (int)(k * length * ctrlPortionLength12);
            points[18] = endPoint;
            #endregion

            return points;
        }

        // funkcia vygeneruje body pre nasledne vykreslenie krivky vodorovneho zubka, smerujuceho bud nahor alebo dole        
        public static Point[] HorizontalPoints(Point startPoint, Point endPoint, bool flip)
        {
            int length = (startPoint.X - endPoint.X); //vzdialenost medzi bodmi horizontalne
            Point[] points = new Point[19];
            int k, l;

            if (length >= 0) { k = (-1); }
            else
            {
                length *= (-1);
                k = 1;
            }

            if (flip) { l = 1; }
            else { l = (-1); }

            #region Set Points

            points[0] = startPoint;
            points[1].X = startPoint.X + (int)(k * length * ctrlPortionLength1);
            points[1].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth1);
            points[2].X = startPoint.X + (int)(k * length * ctrlPortionLength2);
            points[2].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth2);
            points[3].X = startPoint.X + (int)(k * length * endpPortionLength1);
            points[3].Y = startPoint.Y + (int)(l * length * endpPortionWidth1);
            points[4].X = startPoint.X + (int)(k * length * ctrlPortionLength3);
            points[4].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth3);
            points[5].X = startPoint.X + (int)(k * length * ctrlPortionLength4);
            points[5].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth4);
            points[6].X = startPoint.X + (int)(k * length * endpPortionLength2);
            points[6].Y = startPoint.Y + (int)(l * length * endpPortionWidth2);
            points[7].X = startPoint.X + (int)(k * length * ctrlPortionLength5);
            points[7].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth5);
            points[8].X = startPoint.X + (int)(k * length * ctrlPortionLength6);
            points[8].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth6);
            points[9].X = startPoint.X + (int)(k * length * endpPortionLength3);
            points[9].Y = startPoint.Y + (int)(l * length * endpPortionWidth3);
            points[10].X = startPoint.X + (int)(k * length * ctrlPortionLength7);
            points[10].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth7);
            points[11].X = startPoint.X + (int)(k * length * ctrlPortionLength8);
            points[11].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth8);
            points[12].X = startPoint.X + (int)(k * length * endpPortionLength4);
            points[12].Y = startPoint.Y + (int)(l * length * endpPortionWidth4);
            points[13].X = startPoint.X + (int)(k * length * ctrlPortionLength9);
            points[13].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth9);
            points[14].X = startPoint.X + (int)(k * length * ctrlPortionLength10);
            points[14].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth10);
            points[15].X = startPoint.X + (int)(k * length * endpPortionLength5);
            points[15].Y = startPoint.Y + (int)(l * length * endpPortionWidth5);
            points[16].X = startPoint.X + (int)(k * length * ctrlPortionLength11);
            points[16].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth11);
            points[17].X = startPoint.X + (int)(k * length * ctrlPortionLength12);
            points[17].Y = startPoint.Y + (int)(l * length * ctrlPortionWidth12);
            points[18] = endPoint;
            #endregion

            return points;
        }
    }
}
