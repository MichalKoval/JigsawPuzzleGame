using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    public partial class Gameboard : Form
    {
        /*
         * Pre zlepsenie celkoveho vykonu aplikacie pouzivam Buffer Graphics, ktora prekresluje graficku plochu v pamati a az nasledne sa vykresli
         * v Control grafike, v tomto pripade v Gameboard(inheritated Form, nastaveny ako control vlozeny v Panel control).
         * Zarucuje to plynuly pohyb kuska po grafickej ploche bez zasekavania.
         * Ak dojde ku kliknutiu na kusok, vybrany kusok sa presunie navrch a kusky pod nim sa uz neprekresluju.
         * (Su nacitavane z predgenerovaneho Bitmap-u. Vygenerovany Bitmap vznike pri prvnotnom kliknuti na dany kusok, potom az pri naslednom polozeni.
         * Neprekresluju sa vsetky kusky.)
         * Moj prvy pokus na kreslenie kuskov bolo vyuzitie jednotlivych vrstiev 'inheritated picturebox-ov',
         * image property a padding-u, vid. 'subor Old_approach.cs'.
         * Avsak pri vacsom pocte dochadzalo k zasekavaniu jednotlivych controls(picturebox-ov).
         * Podla dokumentacie Microsoft-u je maximalny pocet Controls 254( v ramci jednej skupiny Controls).
         * Pomocou Buffer Graphics a triku s prekreslovanim moze pocet kuskov puzzle byt ovela vacsi. Radovo 500, 1000 kuskov.
         * Trikom v zrychleni prekreslovania je vyuzitie takzvanej Grid Registration metody kde sa hracia plocha rozdeli virtualne na sachovnicu
         * a okrem toho ze kusky puzzle nahodne rozmiestnime po hracej ploche, tak ich poziciu si zapamatame aj v ramci policka sachovnice,
         * v danom policku sa moze nachadzat viacero kuskov puzzle nie vsak vsetky. To nasledne zrychluje vyhladavanie najblizsich kuskov k aktualnym
         * suradniciam mysi. Namiesto toho aby sa prehladavalo(resp. prekreslovalo) 1000 kuskov tak sa prehladavaju iba niektore policka blizke pozicii mysi.
         * Podobne riesenie by bolo pouzit takzvane Quad-Trees, ktore by 2D hraci priestor delili stale na styri podpriestory rekurzivne.
         * Nakoniec som sa vsak rozhodol pre Grid Registration vdaka jednoduchsej implementacii.
         * Vyhody Grid Registration sa prejavia az pri vacsich poctoch puzzle kuskov,
         * kde sa vypocet obmedzi len na konkretnu malu skupinku puzzle kuskov, ktore sa prekreslia pri zdvihnuti kuska a potom az pri naslednom polozeni kuska.
         * Medzi tym ostanu vsetky ostatne kusky staticke(neprekreslovane) v ramci Bitmap-u.
         */

        public Gameboard(Form1 mainForm, PuzzleGameData gameData, Size controlSize)
        {
            // prednastavenia velkosti a pozicie hracej plochy vramci gameboard control
            this.data = gameData;
            GameBoardLocation = new Point(0, 0);
            startControlSize = controlSize;
            lastControlSize = controlSize;
            this.BackColor = Color.Silver;
            CreateBackgroundCapture();

            InitializeComponent();

            data.key_D_Down = false;

            // pre pouzitie BufferGraphics je potrebne pozmenit konstruktor
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            
            // nastavime velkost graphic buffer na velkost panelu, v ktorom mame gameboard
            UpdateGraphics();


            // zobrazenie aktualneho zvacsenia v percentach, default 100%
            // TO DO, fix: hodnota sa nemeni pri zvoleni novej hry!! straca sa referencia na label!!
            #region Zoom status, pociatocne nastavenia
            mainForm_ref = mainForm;
            var panel = PuzzleGameUtilities.ControlByName(mainForm_ref, "panel3");
            if (panel != null)
            {
                zoomLabel.AutoSize = false;
                zoomLabel.Location = new Point(0, 0);
                zoomLabel.TextAlign = ContentAlignment.MiddleCenter;
                zoomLabel.Size = new Size(this.ClientSize.Width, 13);
                panel.Controls.Add(zoomLabel);
                zoomLabel.ForeColor = Color.White;
                zoomLabel.Text = "Zoom: " + ((int)(currScaleFactor * 100.0)).ToString() + " %";
            }
            #endregion

        }

        Pen dashPen = new Pen(Color.Orange);

        private Form1 mainForm_ref;
        private Size startControlSize;

        private Size lastControlSize;

        private PuzzleGameData data;

        private PuzzlePiece currPiece = null;

        private Point mouseDownLocation = Point.Empty;

        private bool mouseRightClick = false;

        private BufferedGraphics bufferGraphics;

        private Bitmap capture;

        private double currScaleFactor = 1.0;

        private Point zoomMoveDiff = new Point(0, 0);

        private Label zoomLabel = new Label();

        private const double SCALEBYFACTOR = 0.05;

        private List<PuzzlePiece> NeighborPieces;

    //private Point currMouseCursorBounds = new Point();


        // vykreslime pociatocny bitmap s kuskami
        private void CreateBackgroundCapture()
        {
            capture = new Bitmap(startControlSize.Width + 200, startControlSize.Height + 200);

            using (Graphics draw = Graphics.FromImage(capture))
            {
                draw.Clear(Color.Gainsboro);

                foreach (var piece in data.Pieces)
                {
                    //na zaciatok vykreslime vsetky kusky
                    //draw.FillRectangle(Brushes.Black, new Rectangle(0, 0, 3, 3));
                    draw.DrawImage(piece.PieceImage, piece.CurrentPosition);
                    
                    //DrawSnapAreas(draw, piece); //pouzite pri ladeni programu, zobrazuje miesta kde jeden kusok zacvakne do druheho
                }
            }
        }

        // pri staceni tlacidla reset. znova vykreslime bitmap s kuskami
        public void ResetBackgroundCapture()
        {
            capture = new Bitmap(GameBoardLocation_BottomRightGameboardCorner_Diff.X + 200, GameBoardLocation_BottomRightGameboardCorner_Diff.Y + 200);

            using (Graphics draw = Graphics.FromImage(capture))
            {
                draw.Clear(Color.Gainsboro);

                foreach (var piece in data.Pieces)
                {
                    //na zaciatok vykreslime vsetky kusky
                    //draw.FillRectangle(Brushes.Black, new Rectangle(0, 0, 3, 3));
                    draw.DrawImage(piece.PieceImage, piece.CurrentPosition);

                    //DrawSnapAreas(draw, piece); //pouzite pri ladeni programu, zobrazuje miesta kde jeden kusok zacvakne do druheho
                }
            }
            this.Invalidate(false); //sposobi zavolanie OnPaint(vykreslenie, refresh graficke plochy) metody len raz
        }

        // nastavime poziciu hracej plochy vramci gameboard control
        public Point GameBoardLocation 
        {
            get; set;
        }  // pozicia bitmap-u, nie control-u

        // ak sme zmensili alebo zvacsili hraciu plocu aplikujeme scalinf factor
        private int ApplyScaleFactor(int value)
        {
            return (int)(value * currScaleFactor);
        }  //ak doslo k zmene velkosti hracej plochy pohybom kolieska mysi

        // zmenime velkost buffer graphics plochy, pri zmene velkosti okna, alebo pri prvotnom nastaveni velkosti
        private void UpdateGraphics()
        {
            if ((this.Width > 0) && (this.Height > 0))
            {
                BufferedGraphicsContext currentContext = BufferedGraphicsManager.Current;
                currentContext.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
                bufferGraphics = currentContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
            }
        }

        // pozmeneny event pre prekreslovanie
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //OnPaint metoda sa zavola pri nacitani control GameBoard, nasledne az pri pohybe puzzle kuska, alebo pri zmene velkosti okna

            bufferGraphics.Graphics.Clear(this.BackColor);
            
            // Vykreslenie kuskov
            if ((mouseDownLocation != Point.Empty) && (currPiece != null)) //ak je lave tlacidlo mysi stlacene, pohybujeme vybranym kuskom (vybrany kusok sa presuva navrch)
            {
                // kusky pod vybranym kuskom nechavame bez zmeny ulozene v Bitmap-e capture
                bufferGraphics.Graphics.DrawImage(capture, GameBoardLocation.X, GameBoardLocation.Y, ApplyScaleFactor(capture.Width), ApplyScaleFactor(capture.Height));
                // prekreslujeme iba vrchny kusok pocas pohybu
                bufferGraphics.Graphics.DrawImage(currPiece.PieceImage,
                                                  ApplyScaleFactor(currPiece.CurrentPosition.X) + GameBoardLocation.X,
                                                  ApplyScaleFactor(currPiece.CurrentPosition.Y) + GameBoardLocation.Y,
                                                  ApplyScaleFactor(currPiece.SizeOfPieceImage.Width),
                                                  ApplyScaleFactor(currPiece.SizeOfPieceImage.Height));

                // taktiez prekreslime aj kusky, ktore su pripojene ku kusku
                if (NeighborPieces != null)
                {
                    foreach (var piece in NeighborPieces)
                    {
                        if (piece != currPiece)
                        {
                            bufferGraphics.Graphics.DrawImage(piece.PieceImage,
                                                      ApplyScaleFactor(piece.CurrentPosition.X) + GameBoardLocation.X,
                                                      ApplyScaleFactor(piece.CurrentPosition.Y) + GameBoardLocation.Y,
                                                      ApplyScaleFactor(piece.SizeOfPieceImage.Width),
                                                      ApplyScaleFactor(piece.SizeOfPieceImage.Height));
                        }
                    }
                }

                // DrawSnapAreas(bufferGraphics, currPiece); //pouzite pri ladeni programu, zobrazuje miesta kde jeden kusok zacvakne do druheho
                
            }
            else
            {
                // vykresli sa na zaciatku pri inicializacii hracej plochy, pri zmene velkosti okna alebo posunu hracej plochy
                bufferGraphics.Graphics.DrawImage(capture, GameBoardLocation.X, GameBoardLocation.Y, ApplyScaleFactor(capture.Width), ApplyScaleFactor(capture.Height));
            }

            bufferGraphics.Render(e.Graphics);
        }

        // pozmeneny event pre zmenu velkosti okna
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            // posunieme zoomLabel do stredu
            zoomLabel.Size = new Size(this.ClientSize.Width, 13);
            
            RepaintGameboardToNewSize();

            this.Invalidate(false);

            UpdateGraphics(); //pri zmene velkosti okna sa updatuje buffer grafika (okno narastie vzdy o +1 pixel)
        }

        // pozmeneny event pri pohybe kolieskom mysi;
        // nastavi sa scale factor;
        // prekreslia sa kusky
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            bool repaint = false;

            if (e.Delta != 0)   
            {
                if (e.Delta > 0)
                {
                    currScaleFactor += SCALEBYFACTOR;
                    zoomMoveDiff.X = (int)(e.X * SCALEBYFACTOR);
                    zoomMoveDiff.Y = (int)(e.Y * SCALEBYFACTOR);
                }
                else
                {
                    currScaleFactor -= SCALEBYFACTOR;
                    zoomMoveDiff.X = -(int)(e.X * SCALEBYFACTOR);
                    zoomMoveDiff.Y = -(int)(e.Y * SCALEBYFACTOR);

                    // Pri zmenseni je potrebne zvacsit(natiahnut) hracie pole
                    repaint = true;
                }
                // Delta > 0 pohyb kolieska mysi smerom nahor == zvacsujeme
                // Delta < 0 pohyb kolieska mysi smerom nadol == zmensujeme
                
                zoomLabel.Text = "Zoom: " + ((int)(currScaleFactor * 100.0)).ToString() + " %";

                // nastavime offset(o kolko treba posunut hracie pole aby sme vytvorili efekt, ze sa zoomuje v mieste kurzoru mysi)
                GameBoardLocation = new Point(GameBoardLocation.X - zoomMoveDiff.X, GameBoardLocation.Y - zoomMoveDiff.Y);

                if (repaint)
                {
                    RepaintGameboardToNewSize();
                }
                
                // Debug.WriteLine("Gameboard pozicia: {0},{1}:", GameBoardLocation.X, GameBoardLocation.Y);
                
                this.Invalidate(false);
            }
        }

        // pozmeneny event pri pohybe mysou, pohyb kuska na ktory sme klikli mysou;
        // ak sme v kolizii s inym kuskom pokusime sa do neho zacvaknut pomocou TrySnapTogetherWith();
        // ak sme stacili prave tlacidlo mysi posunieme hraciu plochu

        //protected override void OnMouseEnter(EventArgs e)
        //{
        //    base.OnMouseEnter(e);
        //    Cursor.Clip = CurrentGameBoardBounds;
        //}

        //protected override void OnMouseLeave(EventArgs e)
        //{
        //    base.OnMouseLeave(e);

        //    if (mouseDownLocation != Point.Empty)
        //    {
        //        Cursor.Position
        //    }
        //}

        bool dontMoveX;
        bool dontMoveY;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            #region Ak sme stlacili lave tlacidlo mysi nad puzzle kuskom == uvedieme kusok do pohybu

            if ((mouseDownLocation != Point.Empty) && (currPiece != null)) //ak bolo stlacene tlacidlo mysi v nejakej pozici, a zaroven bol vybrany kusok
            {
                double scaleDiff = 1 / currScaleFactor;
                //Point point = currPiece.CurrentPosition;
                Point point;
                Point diff = new Point();

                // aby kusky nesli mimo hracieho pola v TOP LEFT Corner
                diff.X = (int)((e.X - mouseDownLocation.X) * scaleDiff);
                diff.Y = (int)((e.Y - mouseDownLocation.Y) * scaleDiff);

                // zistime ci nejaky kusok nevyjde mimo TOP LEFT Corner

                if (NeighborPieces != null)
                {
                    foreach (var piece in NeighborPieces)
                    {
                        if ((piece.CurrentPosition.X + diff.X) < 0)
                        {
                            diff.X = 0;
                        }

                        if ((piece.CurrentPosition.Y + diff.Y) < 0)
                        {
                            diff.Y = 0;
                        }

                    }

                    // az nasledne posunieme kusky
                    if (diff.X != 0)
                    {
                        foreach (var piece in NeighborPieces)
                        {
                            point = piece.CurrentPosition;
                            point.X += diff.X;
                            piece.CurrentPosition = point;
                        }
                    }

                    if (diff.Y != 0)
                    {
                        foreach (var piece in NeighborPieces)
                        {
                            point = piece.CurrentPosition;
                            point.Y += diff.Y;
                            piece.CurrentPosition = point;
                        }
                    }
                }
                else
                {
                    point = currPiece.CurrentPosition;

                    if ((currPiece.CurrentPosition.X + diff.X) < 0)
                    {
                        diff.X = 0;
                    }
                    else
                    {
                        point.X += diff.X;
                    }

                    if ((currPiece.CurrentPosition.Y + diff.Y) < 0)
                    {
                        diff.Y = 0;
                    }
                    else
                    {
                        point.Y += diff.Y;
                    }

                    currPiece.CurrentPosition = point;
                }
                

               
                mouseDownLocation = new Point(e.X, e.Y);
                
                this.Invalidate(false); //sposobi zavolanie OnPaint(vykreslenie, refresh graficke plochy) metody len raz
            }
            #endregion

            #region Ak sme stlacili prave tlacislo mysi ponad hraciu plochu == pohyb hracej plochy

            if ((mouseDownLocation != Point.Empty) && mouseRightClick)
            {
                Point point = GameBoardLocation;
                point.X += e.X - mouseDownLocation.X;
                point.Y += e.Y - mouseDownLocation.Y; //BOTTOM RIGHT corner
                GameBoardLocation = point;

                mouseDownLocation = new Point(e.X, e.Y);

                // Zvacsime prekreslovanu plochu, ak sme posunutim hracej plochy presli cez vykreslenu medzu
                RepaintGameboardToNewSize();
                this.Invalidate(false); //zavolame OnPaint() jedenkrat
            }            
            #endregion
        }
        
        // pozmeneny event pri zdvyhnuti tlacidla mysi;
        // prekreslime kusky pod kuskom, ktory sme polozili, prekreslujeme teda iba urcite miesto hracej plochy, nie vsetky kusky
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // ak sa dany control nenachadza navrchu
            //if (this.Parent.Controls.GetChildIndex(this) != 0)
            //{
            //    this.BringToFront();
            //}

            if (e.Button == MouseButtons.Left)
            {
                this.Cursor = Cursors.Default;
                //presunut vybrany kusok na vrch(odobrat ho zo skupiniek kde uz nebude patrit, kedze ho zaciname presuvat na ine miesto)
                currPiece = clickedImageMoveToFront();
                
                #region Prekreslime kusky pod vybranym kuskom
                if (currPiece != null)
                {
                    dontMoveX = false;
                    dontMoveY = false;
                    using (Graphics draw = Graphics.FromImage(capture))
                    {
                        draw.DrawImage(RepaintAreaUnderPiece(currPiece),
                                       currPiece.CurrentPosition.X,
                                       currPiece.CurrentPosition.Y,
                                       currPiece.SizeOfPieceImage.Width,
                                       currPiece.SizeOfPieceImage.Height);
                    }

                    // prekreslime aj vsetky ostatne kusky, ktore su spojene s danym kuskom

                    if (NeighborPieces != null)
                    {                    
                        foreach (var piece in NeighborPieces)
                        {
                            if (piece != currPiece)
                            {
                                using (Graphics draw = Graphics.FromImage(capture))
                                {
                                    draw.DrawImage(RepaintAreaUnderPiece(piece),
                                                   piece.CurrentPosition.X,
                                                   piece.CurrentPosition.Y,
                                                   piece.SizeOfPieceImage.Width,
                                                   piece.SizeOfPieceImage.Height);
                                }
                            }
                        }
                    }
                }
                #endregion

                mouseDownLocation = new Point(e.X, e.Y);
                // Debug.WriteLine("MousePos: {0},{1}", e.X, e.Y);
                // Debug.WriteLine("MousePosScaled: {0},{1}", (int)((1 / currScaleFactor) * e.X), (int)((1 / currScaleFactor) * e.Y));
                
            }

            if (e.Button == MouseButtons.Right)
            {
                this.Cursor = Cursors.SizeAll;
                mouseRightClick = true;
                mouseDownLocation = new Point(e.X, e.Y);
                // Debug.WriteLine("MousePos: {0},{1}", e.X, e.Y);
                // Debug.WriteLine("MousePosScaled: {0},{1}", (int)((1 / currScaleFactor) * e.X), (int)((1 / currScaleFactor) * e.Y));
            }
        }

        // pozmeneny event pri stlaceni tlacidla mysi;
        // prekreslime kusky pod kuskom, ktory sme zdvyhli, prekreslujeme teda iba urcite miesto hracej plochy, nie vsetky kusky
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            mouseDownLocation = Point.Empty;
            if (mouseRightClick)
            {
                // Debug.WriteLine("Gameboard pozicia: {0},{1}: ", GameBoardLocation.X, GameBoardLocation.Y);
                this.Cursor = Cursors.Default;
                mouseRightClick = false;
            }

            // Ked sa polozi kusok na nove miesto treba ho zaregistrovat v novych grid polickach, ktorych sa dotyka, prekryva
            // nasledne dane okolie treba aj prekreslit.
            // To postupne prevedieme aj na vsetky ostatne pripojene kusku ku kusku
            if (currPiece != null)
            {
                #region TrySnap, ak je kusok v kolizii

                // ako prvy pripevnime ten ktory drzime
                SnapPiece(currPiece);

                #region Zaregistrovat(do sachovnice) a prekreslit kusok, ktory bol drzany

                PuzzleGameUtilities.RegisterPieceInGrid(data, currPiece);
                //Repaint
                using (Graphics draw = Graphics.FromImage(capture))
                {
                    draw.DrawImage(RepaintAreaUnderPiece(currPiece),
                                       currPiece.CurrentPosition.X,
                                       currPiece.CurrentPosition.Y,
                                       currPiece.SizeOfPieceImage.Width,
                                       currPiece.SizeOfPieceImage.Height);

                    // DrawSnapAreas(draw, currPiece); //pouzite pri ladeni programu, zobrazuje miesta kde jeden kusok zacvakne do druheho
                }

                #endregion

                // potom pripevnime vsetky ostatne
                if (NeighborPieces != null)
                {
                    foreach (var piece in NeighborPieces)
                    {
                        if (piece != currPiece)
                        {
                            SnapPiece(piece);

                        #region Zaregistrovat(do sachovnice) a prekreslit kusok

                        PuzzleGameUtilities.RegisterPieceInGrid(data, piece);

                        using (Graphics draw = Graphics.FromImage(capture))
                        {
                            draw.DrawImage(RepaintAreaUnderPiece(piece),
                                               piece.CurrentPosition.X,
                                               piece.CurrentPosition.Y,
                                               piece.SizeOfPieceImage.Width,
                                               piece.SizeOfPieceImage.Height);
                        }

                        #endregion
                        }
                    }
                }

                //V pripade ze drzany kusok sa neprilepi spravne---------------------
                if (NeighborPieces != null)
                {
                    #region Odregistrovat
                    PuzzleGameUtilities.DeregisterPieceFromGrid(data, currPiece);
                    //Repaint
                    using (Graphics draw = Graphics.FromImage(capture))
                    {
                        draw.DrawImage(RepaintAreaUnderPiece(currPiece),
                                           currPiece.CurrentPosition.X,
                                           currPiece.CurrentPosition.Y,
                                           currPiece.SizeOfPieceImage.Width,
                                           currPiece.SizeOfPieceImage.Height);
                    }

                    #endregion

                    SnapPiece(currPiece);

                    #region Zaregistrovat(do sachovnice) a prekreslit kusok, ktory bol drzany

                    PuzzleGameUtilities.RegisterPieceInGrid(data, currPiece);
                    //Repaint
                    using (Graphics draw = Graphics.FromImage(capture))
                    {
                        draw.DrawImage(RepaintAreaUnderPiece(currPiece),
                                           currPiece.CurrentPosition.X,
                                           currPiece.CurrentPosition.Y,
                                           currPiece.SizeOfPieceImage.Width,
                                           currPiece.SizeOfPieceImage.Height);
                    }

                    #endregion

                }

                //else
                //{
                //    PuzzleGameUtilities.RegisterPieceInGrid(data, currPiece);
                //    //Repaint
                //    using (Graphics draw = Graphics.FromImage(capture))
                //    {
                //        draw.DrawImage(RepaintAreaUnderPiece(currPiece),
                //                           currPiece.CurrentPosition.X,
                //                           currPiece.CurrentPosition.Y,
                //                           currPiece.SizeOfPieceImage.Width,
                //                           currPiece.SizeOfPieceImage.Height);

                //        // DrawSnapAreas(draw, currPiece); //pouzite pri ladeni programu, zobrazuje miesta kde jeden kusok zacvakne do druheho
                //    }
                //}

                // zahodime referenciu na drzany kusok
                currPiece = null;

                // zahodime referenciu na zoznam pripojenych kuskov
                NeighborPieces = null;

                #endregion

                this.Invalidate(false);
            }
        }

        // najde kusok a odoberie ho z policok kde sa fyzicky vyskytoval, kusok sa stane top most vramci vsetkych kuskov
        private PuzzlePiece clickedImageMoveToFront()
        {
            /*Ak dojde ku kliknutiu a mys ukazuje na kusok puzzle(teda je v kolizii podla funckie IsInCollisionWithMouse) sa dany kusok presunie v poradi nahor.
            Presunutie znamena ze v danom List<PuzzlePiece> odoberie vybrany kus, a prida sa na vrch list-u.
            Pri prekreslovani sa vykresluju jednotlive kusky v poradi akom su v list-e (UPDATE: prekresluju sa len kusky v blizkosti zdvihnutia a polozenia kuska!!!),
            to vytvara efekt, ze sme kusok presunuli navrch a na vybrane miesto a nezostalo po nom nic na predchadzajucom mieste.*/

            Point currMouseCoords = this.PointToClient(Cursor.Position);
            double scaleDiff = 1 / currScaleFactor;
            currMouseCoords = new Point((int)(currMouseCoords.X * scaleDiff), (int)(currMouseCoords.Y * scaleDiff));

            currMouseCoords.X -= (int)(GameBoardLocation.X / currScaleFactor);
            currMouseCoords.Y -= (int)(GameBoardLocation.Y /currScaleFactor);
            
            

            List<PuzzlePiece> bucketUnderMouse = PuzzleGameUtilities.BucketUnderMouse(data, currMouseCoords);
            
            PuzzlePiece result = null;

            if (bucketUnderMouse != null)
            {
                for (int i = (bucketUnderMouse.Count - 1); i >= 0; i--) //idem v poradi zhora dole aby sa vybral ten kusok vyssie ak sa dve prekryvaju..
                {
                    if (IsInCollisionWithMouse(bucketUnderMouse[i], currMouseCoords))      //ak mys ukazuje na viacero kuskov naraz, vyberie sa najvyssi z nich
                    {
                        var pieceToRemove = bucketUnderMouse[i];

                        //odregistrujeme kusok z virtualnej hracej sachovnice
                        PuzzleGameUtilities.DeregisterPieceFromGrid(data, pieceToRemove);

                        // Debug.WriteLine("Realna pozicia stlaceneho kuska: {0},{1}", pieceToRemove.CurrentPosition.X, pieceToRemove.CurrentPosition.Y);
                        result = pieceToRemove; 
                        break;
                    }
                }
            }

            // najdeme este vsetky susediace kusky a odregistrujeme ich z gridu, ak sme nestlacili pismeno D (D == odpojit kusok so skupinky)
            if ( (result != null) && (!data.key_D_Down) )
            {
                NeighborPieces = new List<PuzzlePiece>();
                PuzzlePiecesDFS(result, ref NeighborPieces);

                foreach (var piece in NeighborPieces)
                {
                    // nechcem znova odregistrovat kusok, ktory sme uz odregistrovalo
                    if (piece != result)
                    {
                        PuzzleGameUtilities.DeregisterPieceFromGrid(data, piece);
                    }
                    // mozeme hned aj nastavit ze tieto kusky budu znova navstivitelne
                    piece.VisitState = false;
                }
            }
            else if (data.key_D_Down) // ak sme stacili tlacidlo D, chceme odpojit kusok
            {
                if (result.TopNeighbor != null)
                {
                    result.TopNeighbor.BottomNeighbor = null;
                    result.TopNeighbor = null;
                }

                if (result.RightNeighbor != null)
                {
                    result.RightNeighbor.LeftNeighbor = null;
                    result.RightNeighbor = null;
                }

                if (result.BottomNeighbor != null)
                {
                    result.BottomNeighbor.TopNeighbor = null;
                    result.BottomNeighbor = null;
                }

                if (result.LeftNeighbor != null)
                {
                    result.LeftNeighbor.RightNeighbor = null;
                    result.LeftNeighbor = null;
                }

            }
            

            return result; //vratime najdeny kusok
        }
        
        // zistime ci je mys v kolizii s nejakym kuskom, resp. ci sa mys dotyka nejakeho kuska, dotykat sa moze len kuskov, ktore su viac hore  
        private bool IsInCollisionWithMouse(PuzzlePiece piece, Point mouseLocation)
        {
            bool result = false;
            int insidePieceX = mouseLocation.X - piece.CurrentPosition.X;
            int insidePieceY = mouseLocation.Y - piece.CurrentPosition.Y;
            int pieceWidth = data.PieceDimensions.Width;
            int pieceHeight = data.PieceDimensions.Height;
            int hrubkaRamceka = data.PieceSurroundingSize;

            if (((insidePieceX >= 0) && (insidePieceX < (pieceWidth + 2 * hrubkaRamceka))) &&  //virtualne ohranicenie, TO DO zmenit na rect bounds!!
                 ((insidePieceY >= 0) && (insidePieceY < (pieceHeight + 2 * hrubkaRamceka))) &&
                 (piece.PieceImage.GetPixel(insidePieceX, insidePieceY).A != 0)) //ak alfa kanal na danom pixely je rozny od nuly tak ukazujeme na kusok
            {
                result = true;
            }

            return result;
        }

        //funkcia na prekrelenie pozadia kuska, prekresla sa iba kusku blizke vybranemu kusku a nachadzaju sa po vybranym kusokom
        public Bitmap RepaintAreaUnderPiece(PuzzlePiece piece)
        {
            Point[] pieceCorners;
            Point bucketCoords;
            List<PuzzlePiece> value;
            Size drawAreaUnderPiece_Size = new Size((data.PieceDimensions.Width + 2 * data.PieceSurroundingSize) * 2,
                                                    (data.PieceDimensions.Height + 2 * data.PieceSurroundingSize) * 2 );
            Size quadrant_Size = new Size(data.PieceDimensions.Width + 2 * data.PieceSurroundingSize,
                                          data.PieceDimensions.Height + 2 * data.PieceSurroundingSize);

            //dradrawAreaUnderPiece pozostava zo styroch kvadrantov, kazdy roh kuska masvoj vlastny kvadrant
            Bitmap drawAreaUnderPiece = new Bitmap(drawAreaUnderPiece_Size.Width, drawAreaUnderPiece_Size.Height);
            Bitmap[] quadrants = new Bitmap[4];

            for (int i = 0; i < 4; i++)
            {
                quadrants[i] = new Bitmap(quadrant_Size.Width, quadrant_Size.Height);
            }

            pieceCorners = PuzzleGameUtilities.RealPieceCornerPoints(piece.CurrentPosition,   //ziskame rohy kuska, na ktory prave ukazujeme
                                                                     piece.SizeOfPieceImage); //aby sme prekreslili policka pod nim, ktorych sa dotyka(prekryva)
            //
            //Prekreslime kusky pod vybranym kuskom. Prekreslime len tie policka, do ktorych fyzicky kusok patril,(do policok rozmerov 2x2)
            //
            for (int i = 0; i < 4; i++)
            {
                bucketCoords = PuzzleGameUtilities.BucketCoordinates(piece.SizeOfPieceImage, pieceCorners[i]);

                if (data.bucketOfPieces.TryGetValue(bucketCoords, out value))
                {
                    using (Graphics draw = Graphics.FromImage(quadrants[i]))
                    {
                        draw.Clear(Color.Gainsboro);

                        foreach (var item in value)
                        {
                            //na zaciatok vykreslime vsetky kusky vramci kvadrantu, kusky ktore sa nezmestia ostanu odkrojene
                            draw.DrawImage(item.PieceImage, new Point(item.CurrentPosition.X - quadrant_Size.Width * bucketCoords.X,
                                                                       item.CurrentPosition.Y - quadrant_Size.Height * bucketCoords.Y));
                        }
                    }
                }
                else
                {
                    using (Graphics draw = Graphics.FromImage(quadrants[i]))
                    {
                        draw.Clear(Color.Gainsboro);
                    }
                }
            }
            //pospajame jednotlive kvadranty
            using (Graphics draw = Graphics.FromImage(drawAreaUnderPiece))
            {
                draw.DrawImage(quadrants[0], new Point(0, 0));
                draw.DrawImage(quadrants[1], new Point(quadrant_Size.Width, 0));
                draw.DrawImage(quadrants[2], new Point(quadrant_Size.Width, quadrant_Size.Height));
                draw.DrawImage(quadrants[3], new Point(0, quadrant_Size.Height));
            }

            //vystrihneme vysledne pozadie pod kuskom, ktory sa chystame presunut
            bucketCoords = PuzzleGameUtilities.BucketCoordinates(piece.SizeOfPieceImage, piece.CurrentPosition);

            Point startPoint = new Point(piece.CurrentPosition.X - quadrant_Size.Width * bucketCoords.X,
                                         piece.CurrentPosition.Y - quadrant_Size.Height * bucketCoords.Y);

            Point endPoint = new Point(startPoint.X + piece.SizeOfPieceImage.Width,
                                       startPoint.Y + piece.SizeOfPieceImage.Height);

            drawAreaUnderPiece = PictureEditor.CropImage(drawAreaUnderPiece, startPoint, endPoint);

            return drawAreaUnderPiece;
        }

        private void SnapPiece(PuzzlePiece piece)
        {
            Point[] pieceCorners;
            Point bucketCoords;
            List<PuzzlePiece> value;
            int maxDepthIndex;

            pieceCorners = PuzzleGameUtilities.RealPieceCornerPoints(piece.CurrentPosition,   //ziskame rohy kuska, na ktory prave ukazujeme
                                                                     piece.SizeOfPieceImage);

            for (int i = 0; i < 4; i++)
            {
                bucketCoords = PuzzleGameUtilities.BucketCoordinates(piece.SizeOfPieceImage, pieceCorners[i]);

                if (data.bucketOfPieces.TryGetValue(bucketCoords, out value))
                {
                    // skusame spajat len s najvysie polozenymi kuskami (hlbka max 3 kusky, aby nedoslo zbytocne k nahodnym pripojeniam ku kuskom,
                    // ktore su hlboko v kosiku s puzzle kuskami)

                    if (value.Count > 3)
                    {
                        maxDepthIndex = (value.Count - 1) - 3;
                    }
                    else
                    {
                        maxDepthIndex = 0;
                    }
                    for (int k = (value.Count - 1); k >= maxDepthIndex; k--)
                    {
                        if (TrySnapTogetherWith(value[k], piece))  
                        {
                            break;
                        }
                    }

                }
            }
        }

        //tato funckia sa pokusi spojit kusky ak su dostatocne blizko pri sebe
        private bool TrySnapTogetherWith(PuzzlePiece collisionPiece, PuzzlePiece clickedPiece)
        {
            #region Pomocne premenne

            bool result = false;
            Rectangle[] snapAreas = collisionPiece.SnapAreas;
            Point pieceCollisionPoint = clickedPiece.CurrentPosition;
            pieceCollisionPoint.X += data.PieceSurroundingSize;
            pieceCollisionPoint.Y += data.PieceSurroundingSize;
            int diffX = collisionPiece.SizeOfPiece.Width;

            int diffY = collisionPiece.SizeOfPiece.Height;
            #endregion

            //Podla toho, ktory roh natrafime, podla toho nastavime, z ktorej strany ide kusok ku kusku + ci k sebe pasuju strany == Arrangement
            for (int i = 0; i < 4; i++)
            {
                if (snapAreas[i].Contains(pieceCollisionPoint))
                {
                    switch (i)
                    {
                        // snap zhora
                        case 0:
                            if ( ((clickedPiece.Arrangement.BottomSide == "in") && (collisionPiece.Arrangement.TopSide == "out")) ||
                                 ((clickedPiece.Arrangement.BottomSide == "out") && (collisionPiece.Arrangement.TopSide == "in")) )
                            {
                                diffX *= 0;
                                diffY *= (-1);
                                result = true;
                                // nastavime ich navzajom ako susedov
                                collisionPiece.TopNeighbor = clickedPiece;
                                clickedPiece.BottomNeighbor = collisionPiece;
                            }
                            break;
                        // snap zprava
                        case 1:
                            if ( ((clickedPiece.Arrangement.LeftSide == "in") && (collisionPiece.Arrangement.RightSide == "out")) ||
                                 ((clickedPiece.Arrangement.LeftSide == "out") && (collisionPiece.Arrangement.RightSide == "in")) )
                            {
                                diffX *= 1;
                                diffY *= 0;
                                result = true;
                                collisionPiece.RightNeighbor = clickedPiece;
                                clickedPiece.LeftNeighbor = collisionPiece;
                            }
                            break;
                        // snap zdola
                        case 2:
                            if ( ((clickedPiece.Arrangement.TopSide == "in") && (collisionPiece.Arrangement.BottomSide == "out")) ||
                                 ((clickedPiece.Arrangement.TopSide == "out") && (collisionPiece.Arrangement.BottomSide == "in")) )
                            {
                                diffX *= 0;
                                diffY *= 1;
                                result = true;
                                collisionPiece.BottomNeighbor = clickedPiece;
                                clickedPiece.TopNeighbor = collisionPiece;
                            }
                            break;
                        // snap zlava
                        case 3:
                            if ( ((clickedPiece.Arrangement.RightSide == "in") && (collisionPiece.Arrangement.LeftSide == "out")) ||
                                 ((clickedPiece.Arrangement.RightSide == "out") && (collisionPiece.Arrangement.LeftSide == "in")) )
                            {
                                diffX *= (-1);
                                diffY *= 0;
                                result = true;
                                collisionPiece.LeftNeighbor = clickedPiece;
                                clickedPiece.RightNeighbor = collisionPiece;
                            }
                            break;
                    }
                    break;
                }
            }

            if (result)
            {
                Point currLoc = clickedPiece.CurrentPosition;
                double scaleDiff = 1 / currScaleFactor;
                //int max_X, max_Y;

                currLoc.X = collisionPiece.CurrentPosition.X + diffX;

                if (currLoc.X < 0) { currLoc.X = 0; }

                currLoc.Y = collisionPiece.CurrentPosition.Y + diffY;

                if (currLoc.Y < 0) { currLoc.Y = 0; }

                clickedPiece.CurrentPosition = currLoc;
            }

            return result;
        }

        // prehladavanie kuskov do sirky
        // funkcia vrati zoznam vsetkych najdenych kuskov pri prehladavani do sirky
        // nezabudnut vytvorit strukturu do ktorej pridame najdene kusky
        private void PuzzlePiecesDFS(PuzzlePiece piece, ref List<PuzzlePiece> searchedPieces)
        {
            // ak sme este nenavstivili kusok
            if (!piece.VisitState)
            {
                searchedPieces.Add(piece);
                piece.VisitState = true;
            }

            // navstivime laveho suseda ak existuje  a ak este nebol navstiveny
            if (piece.LeftNeighbor != null)
            {
                if (!piece.LeftNeighbor.VisitState)
                {
                    PuzzlePiecesDFS(piece.LeftNeighbor, ref searchedPieces);
                }                
            }

            // navstivime horneho suseda ak existuje a ak este nebol navstiveny
            if (piece.TopNeighbor != null)
            {
                if (!piece.TopNeighbor.VisitState)
                {
                    PuzzlePiecesDFS(piece.TopNeighbor, ref searchedPieces);
                }
            }

            // navstivime praveho suseda ak existuje a ak este nebol navstiveny
            if (piece.RightNeighbor != null)
            {
                if (!piece.RightNeighbor.VisitState)
                {
                    PuzzlePiecesDFS(piece.RightNeighbor, ref searchedPieces);
                }
            }

            // navstivime dolneho suseda ak existuje a ak este nebol navstiveny
            if (piece.BottomNeighbor != null)
            {
                if (!piece.BottomNeighbor.VisitState)
                {
                    PuzzlePiecesDFS(piece.BottomNeighbor, ref searchedPieces);
                }
            }

        }
        
        // pomocne funkcie pre zobrazenie miest pre automaticke zacvaknutie kuskov, pouzite pri ladeni
        private void DrawSnapAreas(Graphics draw, PuzzlePiece piece)
        {
            draw.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[0]);
            draw.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[1]);
            draw.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[2]);
            draw.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[3]);
        }
        //
        private void DrawSnapAreas(BufferedGraphics draw, PuzzlePiece piece)
        {
            draw.Graphics.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[0]);
            draw.Graphics.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[1]);
            draw.Graphics.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[2]);
            draw.Graphics.DrawRectangle(new Pen(Brushes.Red), piece.SnapAreas[3]);
        } //+1 overload
        //
        
        //
        private Point GameBoardLocation_BottomRightGameboardCorner_Diff
        {
            get
            {
                double scaleDiff = 1 / currScaleFactor;
                return new Point((int)(Math.Abs(GameBoardLocation.X - this.Width) * scaleDiff),
                                 (int)(Math.Abs(GameBoardLocation.Y - this.Height) * scaleDiff));
            }
        }

        private void RepaintGameboardToNewSize() // metoda nevola sama invalidate(false)!!
        {
            Bitmap temp;
            if (capture.Width < GameBoardLocation_BottomRightGameboardCorner_Diff.X)
            {
                temp = new Bitmap(GameBoardLocation_BottomRightGameboardCorner_Diff.X + 200, capture.Height);
                using (Graphics draw = Graphics.FromImage(temp))
                {
                    draw.Clear(Color.Gainsboro);
                    draw.DrawImage(capture, 0, 0);
                }
                capture = temp;
            }

            if (capture.Height < GameBoardLocation_BottomRightGameboardCorner_Diff.Y)
            {
                temp = new Bitmap(capture.Width, GameBoardLocation_BottomRightGameboardCorner_Diff.Y + 200);
                using (Graphics draw = Graphics.FromImage(temp))
                {
                    draw.Clear(Color.Gainsboro);
                    draw.DrawImage(capture, 0, 0);
                }
                capture = temp;
            }
        }

        private Rectangle CurrentGameBoardBounds
        {
            get
            {
                return new Rectangle(GameBoardLocation,
                                     new Size(GameBoardLocation_BottomRightGameboardCorner_Diff.X + 200,
                                              GameBoardLocation_BottomRightGameboardCorner_Diff.Y + 200));
            }
        }
    }
}







//old_stuff
//-------------------------------------------------
//
//private void custom_KeyDown(object sender, KeyEventArgs e)
//{
//    if (e.KeyCode == Keys.D)
//    {
//        keyDown = true;

//    }

//    Debug.WriteLine("Stlacene fu");
//}

//private void custom_KeyUp(object sender, KeyEventArgs e)
//{
//    keyDown = false;
//}

//protected override void OnKeyDown(KeyEventArgs e)
//{
//    base.OnKeyDown(e);

//    if (e.KeyCode == Keys.D)
//    {
//        keyDown = true;
//        Debug.WriteLine("Stlacene D");
//    }
//}

//protected override void OnKeyUp(KeyEventArgs e)
//{
//    base.OnKeyUp(e);

//    keyDown = false;
//}

//--------------------------------------------------

//zvacsime backGround Bitmap
//if (this.Width >= lastControlSize.Width)
//{
//    currControlSize.Width = this.Width;
//}
//else
//{
//    currControlSize.Width = lastControlSize.Width;
//}

//if (this.Height >= lastControlSize.Height)
//{
//    currControlSize.Height = this.Height;
//}
//else
//{
//    currControlSize.Height = lastControlSize.Height;
//}

//Bitmap temp = new Bitmap(currControlSize.Width + 200, currControlSize.Height + 200);
//using (Graphics draw = Graphics.FromImage(temp))
//{
//    draw.Clear(this.BackColor);
//    draw.DrawImage(capture, 0, 0);
//}
//capture = temp;

//lastControlSize = currControlSize;

//--------------------------------------------------------
//max_X = ((int)(this.Width * scaleDiff) - (2 * data.PieceSurroundingSize + data.PieceDimensions.Width));
//if (currLoc.X > max_X)
//{
//    currLoc.X = max_X;
//}


//max_Y = ((int)(this.Height * scaleDiff) - (2 * data.PieceSurroundingSize + data.PieceDimensions.Height));
//if (currLoc.Y > max_Y)
//{
//    currLoc.Y = max_Y;
//}


// spojime skupiny puzzle kuskov
//if (collisionPiece.Group == null)
//{
//    if (currpiece == null)
//    {
//        collisionPiece.Group = new List<PuzzlePiece>();
//        currpiece.Group = collisionPiece.Group;
//        collisionPiece.Group.Add(collisionPiece);
//        collisionPiece.Group.Add(currpiece);
//    }
//    else
//    {
//        collisionPiece.Group = currpiece.Group;
//        currpiece.Group.Add(collisionPiece);
//    }
//}
//else
//{
//    if (currpiece == null)
//    {
//        currpiece.Group = collisionPiece.Group;
//        collisionPiece.Group.Add(currpiece);
//    }
//    else
//    {

//    }
//}

//------old mouse move event
// ak nie je cursor mimo hraciu plochu
//if (CurrentGameBoardBounds.Contains(point.X + (int)((e.X - mouseDownLocation.X) * scaleDiff),
//                                    point.Y + (int)((e.Y - mouseDownLocation.Y) * scaleDiff)))
//{

//Cursor.Position = new Point(GameBoardLocation.X + (Math.Abs(currPiece.CurrentPosition.X - e.X)), Cursor.Position.Y);
//if (point.X > ((int)(this.Width * scaleDiff) - (2 * data.PieceSurroundingSize + data.PieceDimensions.Width)))
//{
//    point.X = ((int)(this.Width * scaleDiff) - (2 * data.PieceSurroundingSize + data.PieceDimensions.Width));
//}

//Cursor.Position = new Point(Cursor.Position.X, GameBoardLocation.Y + (Math.Abs(currPiece.CurrentPosition.X - e.Y)));
//if (point.Y > ((int)(this.Height * scaleDiff) - (2 * data.PieceSurroundingSize + data.PieceDimensions.Height)))
//{
//    point.Y = ((int)(this.Height * scaleDiff) - (2 * data.PieceSurroundingSize + data.PieceDimensions.Height));
//}
//}
//------------------------------------------------------------
//private void WheelZoomAtPoint(Point zoomLocation, bool positove_Delta)  //(positive_Delta == true) priblizujeme, inak oddialujeme
//{
//    transformMatrix.Translate(-zoomLocation.X, -zoomLocation.Y);  //miesto kde sme zacali zoomovat

//    if (positove_Delta) //podla toho, ci je positive_Delta true alebo false vieme, ze doslo bud k zvacsieniu alebo zmenseniu v bode kde ukazuje mys
//    {
//        //transformMatrix.Scale(scaleByFactor, scaleByFactor);
//        transformMatrix.Scale(1.1F, 1.1F);
//    }
//    else
//    {
//        //transformMatrix.Scale(-scaleByFactor, -scaleByFactor);
//        transformMatrix.Scale(0.9F, 0.9F);
//    }

//    transformMatrix.Translate(zoomLocation.X, zoomLocation.Y);  //

//    bufferGraphics.Graphics.ResetTransform();
//    bufferGraphics.Graphics.Transform = transformMatrix;

//    //po zoomovani zavolame OnPaint metody na prekreslenie, jedenkrat
//    this.Invalidate(false);
//}

//-------------------------------------------------------------------------------

//vychadzame z povodneho scaleFactor-u ktori je na zaciatku == 1
//jeden cvaknutie kolieskom je ekvivaletne 1 delte (delta == 120)

//bufferGraphics.Graphics.Transform

//int delta = (int)(e.Delta / 120F); //zaokruhlenie nadol nie vzdy je delta nasobok 120!!
//if ((delta > 0) && (delta < 10))
//{
//    bufferGraphics.Graphics.ScaleTransform(((delta * 0.1F) + 1.0F) / scaleByFactor,
//                                           ((delta * 0.1F) + 1.0F) / scaleByFactor);
//    scaleFactor = ((delta * 0.1F) + 1.0F) / scaleByFactor;
//    this.Invalidate(false);
//}
//else if ((delta < 0) && (delta > (-10)))
//{
//    bufferGraphics.Graphics.ScaleTransform((1.0F - ((-1F) * delta * 0.1F)) / scaleByFactor,
//                                           (1.0F - ((-1F) * delta * 0.1F)) / scaleByFactor);
//    scaleFactor = (1.0F - ((-1F) * delta * 0.1F)) / scaleByFactor;
//    this.Invalidate(false);
//}

//label1.Text = scaleFactor.ToString();

//bufferGraphics.Graphics.ScaleTransform((delta * 0.1F) + 1.0F, (delta * 0.1F) + 1.0F);
//scaleFactor = (1.0F + (delta * 0.1F));
//---------------------------------------------------------------------------------

//aby sa neprekreslovali vsetky kusky pri pohybe vrchneho kuska mysou + prekreslit len vtedy ak na vrch pride iny kusok!
//if ((lastPiece == null) || (lastPiece != data.Pieces[data.PiecesCount - 1]))
//{
//    capture = new Bitmap(this.Width, this.Height);

//    using (Graphics draw = Graphics.FromImage(capture))
//    {
//        draw.Clear(this.BackColor);

//        for (int i = 0; i < (data.PiecesCount - 1); i++)
//        {
//            //prekreslime skupinu najblizsich kuskov okrem vrchneho
//            draw.DrawImage(data.Pieces[i].PieceImage, data.Pieces[i].CurrentPosition);
//        }
//    }

//    lastPiece = data.Pieces[data.PiecesCount - 1]; //ako posledny kliknuty piece ulozime piece na vrchu,ak nanho hned znova klikneme nebude potrebne prekreslenie
//}

//private BufferedGraphics tempGraphics;

//private BufferedGraphicsContext tempContext;


//private void LoadPieces()
//{
//    Random rnd = new Random();

//    for (int i = 0; i < pieceCount; i++)
//    {
//        Piece item = new Piece();
//        pieces.Add(item);
//        item.PieceImage = new Bitmap(puzzleImage);
//        item.PieceDimensions = new Size(puzzleImage.Size.Width, puzzleImage.Size.Height);
//        item.PieceLocation = new Point(rnd.Next(10, this.Width - item.PieceDimensions.Width),
//                                        rnd.Next(10, this.Height - item.PieceDimensions.Height));
//    }
//}