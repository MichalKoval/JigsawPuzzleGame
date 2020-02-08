using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    public partial class PuzzleGameData
    {
        private double scalingFactor = 1.0;

        // pocet kuskov puzzle
        public int PiecesCount { get; set; }

        // pridane vymedzene okolie puzzle kuska navyse, aby sme vystrihli aj zubky puzzle kuska
        public int PieceSurroundingSize { get; set; }

        //realne velkost puzzle kuska, bez zubkov
        public Size PieceDimensions { get; set; }

        // urcuje rozmery mriezky (grid-u), ktora bude urcovat oblast na vystrihnutie
        public Size PiecesGridDimensions { get; set; }

        // referencia na vsetky vystrihnute kusky
        public List<PuzzlePiece> Pieces { get; set; }

        // pristup k jednotlivym skupinkam kuskov v konstatnom(amortizovanom) case
        public Dictionary<Point, List<PuzzlePiece>> bucketOfPieces = new Dictionary<Point, List<PuzzlePiece>>();

        public Panel GameBoard { get; set; }

        public Point GameBoardStartPosition { get; set; }

        // hlavny zdrojovy obrazok, z ktoreho sa neskor budu vystrihovat puzzle
        public Bitmap SourcePicture { get; set; }

        public bool key_D_Down { get; set; }
        
        // zatial nevyuzite!
        public double ScalingFactor
        {
            get
            {
                return scalingFactor;
            }
            set
            {
                this.scalingFactor = value;
            }
        }
    }
}
