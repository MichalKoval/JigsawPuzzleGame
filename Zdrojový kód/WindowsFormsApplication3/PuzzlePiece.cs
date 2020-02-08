using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    public partial class PuzzlePiece
    {
        #region Pomocne privatne premenne
        
        private Size sizeOfImage = new Size(0, 0);
        private Size sizeOfPiece = new Size(0, 0);
        private Point origPosition = new Point(0, 0);
        private Point currPosition = new Point(0, 0);
        private Bitmap clickedPiece;
        private Bitmap originalPiece;
        private Bitmap currentPiece;
        private Rectangle pieceArea = new Rectangle();
        private Rectangle[] snapAreas = new Rectangle[4];
        private Point diffOfImagePiece; //sluzi ako stred pre snap oblasti

        #endregion

        //
        // jednotlive metody
        //        
        // nakoniec nepouzite, malo sluzit pre unikatnu identifikaciu kuska
        public int ID { get; set; }

        // velkost vykrojeneho obrazka vratane hrany okolo, v ktorej sa nachadzaju zubky puzzle kuska,
        // napr. velkost == 100px + okraje vypocitane podla velkosti zubok
        // pri nacitani obrazka sa urci optimalna velkost puzzle, tu si vsak uzivatel moze zmenit(vid. GridLayer.cs)
        public Size SizeOfPieceImage
        {
            get { return this.sizeOfImage; }
            set
            {
                this.sizeOfImage = value;                               
            }
        }

        // velkost kuska bez zubkov, velkost zvolena na zaciatku hry, napr. velkost == 100px 
        public Size SizeOfPiece
        {
            get { return this.sizeOfPiece; }
            set
            {
                this.sizeOfPiece = value;
                this.pieceArea.Size = value;

                for (int i = 0; i < 4; i++)
                {
                    snapAreas[i].Size = new Size(20, 20);
                }
                diffOfImagePiece = new Point((int)((this.sizeOfImage.Width - this.sizeOfPiece.Width) / 2),
                                              (int)((this.sizeOfImage.Height - this.sizeOfPiece.Height) / 2)); //nastavime stred pre snap oblasti
            }
        }

        // aktualna pozicia kuska
        public Point CurrentPosition
        {
            get { return this.currPosition; }
            set
            {
                this.currPosition = value;
            } 
        }

        // originalna pozicia kuska, pre neskorsie overienie spravnosti poskladaneho puzzle
        public Point OriginalPosition
        {
            get { return this.origPosition; }
            set { this.origPosition = value; }
        }

        // ulozene nahodne nastavenia zubkov kuska, pomocou PuzzleGameUtilities.GeneratePiecesArrangement();
        public PieceArrangement Arrangement { get; set; }

        // obrazok kuska, uz vystrihnuteho podla tvaru
        public Bitmap PieceImage
        {
            set
            {
                currentPiece = value;
                originalPiece = value;
                clickedPiece = PieceCutter.SetClickEffect(value);
            }

            get { return currentPiece; }
        }

        // zatial nevyuzite, povodny zamer bol vcasna detekcia kolizie dvoch kuskov, to vsak nahradila vlastnost SnapAreas
        public Rectangle PieceArea
        {
            get
            {
                this.pieceArea.Location = new Point(this.currPosition.X + this.diffOfImagePiece.X,
                                                    this.currPosition.Y + this.diffOfImagePiece.Y);
                return pieceArea; //vyuzije sa neskor ako collision object
            }

        }

        // oblasti v ktorych kusok reaguje na zacvaknutie sa s inym kuskom
        public Rectangle[] SnapAreas
        {
            get
            {
                int startPiecePointX = currPosition.X + diffOfImagePiece.X;
                int startPiecePointY = currPosition.Y + diffOfImagePiece.Y;

                int snapAreaCenterX = ((int)(snapAreas[0].Size.Width / 2));
                int snapAreaCenterY = ((int)(snapAreas[0].Size.Height / 2));

                snapAreas[0].Location = new Point(startPiecePointX - snapAreaCenterX,
                                                  startPiecePointY - sizeOfPiece.Height - snapAreaCenterY); //nad vrcholom p0
                snapAreas[1].Location = new Point(startPiecePointX + sizeOfPiece.Width - snapAreaCenterX, 
                                                  startPiecePointY - snapAreaCenterY); //napravo od vrchola p0
                snapAreas[2].Location = new Point(startPiecePointX - snapAreaCenterX,
                                                  startPiecePointY + sizeOfPiece.Height - snapAreaCenterY); //pod vrcholom p0
                snapAreas[3].Location = new Point(startPiecePointX - sizeOfPiece.Width - snapAreaCenterX,
                                                  startPiecePointY - snapAreaCenterY); //nalavo od vrchola p0

                return snapAreas;
            }
        }

        // zatial nepouzite, metoda ma zvyranit kusok ak ponad neho prechadza kurzor mysi
        public void PieceImageClickDown() //pouzije sa pri prechode mysi ponad puzzle kusok a pri "drag and drop" puzzle kuska
        {
            currentPiece = clickedPiece;
        }
        // taktiez zatial nepouzite
        public void PieceImageClickUP()
        {
            currentPiece = originalPiece;
        }

        // referencia na skupinu
        //public List<PuzzlePiece> Group;

        // referencie susedov, s ktorymi kusok prave susedi
        public PuzzlePiece LeftNeighbor { get; set; }
        
        public PuzzlePiece TopNeighbor { get; set; }
        
        public PuzzlePiece RightNeighbor { get; set; }
        
        public PuzzlePiece BottomNeighbor { get; set; }

        // referencie na susedov, ktori patria k danemu kusku
        public PuzzlePiece OriginalLeftNeighbor { get; set; }

        public PuzzlePiece OriginalTopNeighbor { get; set; }

        public PuzzlePiece OriginalRightNeighbor { get; set; }

        public PuzzlePiece OriginalBottomNeighbor { get; set; }

        // hodnota reprezentujuca, ci dany kusok patri alebo nepatri na miesto kde sa nachadza
        public bool IsInTheRightPlace_Final { get; set; }

        public bool IsInTheRightPlace_Current { get; set; }

        // stav kuska pri prehladavani, ci sme ho uz navstivili alebo nie
        public bool VisitState { get; set; }
    }
}
