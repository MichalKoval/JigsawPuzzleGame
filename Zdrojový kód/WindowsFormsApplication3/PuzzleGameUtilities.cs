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
    class PuzzleGameUtilities
    {
        // Vytvori spojenu krivku urcujucu tvar puzzle za pomoci metod z triedy PuzzleBezierovaKrivka a nastaveni hran z PieceArrangement
        private static GraphicsPath CreatePuzzleShapePath(int pieceSurroundingSize,
                                                          Size pieceDimensions,
                                                          PieceArrangement arrang )
        {
            GraphicsPath bezierpath = new GraphicsPath();
                        
            int locationx = pieceSurroundingSize; //offset pre posun vyrezu
            int locationy = pieceSurroundingSize;
            int pieceWidth = pieceDimensions.Width;
            int pieceHeight = pieceDimensions.Height;
            Point p1, p2, p3, p4;
            p1 = new Point(locationx, locationy);
            p2 = new Point(locationx + pieceWidth, locationy);
            p3 = new Point(locationx + pieceWidth, locationy + pieceHeight);
            p4 = new Point(locationx, locationy + pieceHeight);


            #region TopCurve p1 --> p2
            switch (arrang.TopSide)
            {
                case "none":
                    bezierpath.AddLine(p1, p2);
                    break;
                case "in":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.HorizontalPoints(p1, p2, true));
                    break;
                case "out":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.HorizontalPoints(p1, p2, false));
                    break;
                default:
                    break;
            }
            #endregion

            #region RightCurve p2 --> p3
            switch (arrang.RightSide)
            {
                case "none":
                    bezierpath.AddLine(p2, p3);
                    break;
                case "in":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.VerticalPoints(p2, p3, false));
                    break;
                case "out":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.VerticalPoints(p2, p3, true));
                    break;
                default:
                    break;
            }
            #endregion

            #region BottomCurve p3 --> p4
            switch (arrang.BottomSide)
            {
                case "none":
                    bezierpath.AddLine(p3, p4);
                    break;
                case "in":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.HorizontalPoints(p3, p4, false));
                    break;
                case "out":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.HorizontalPoints(p3, p4, true));
                    break;
                default:
                    break;
            }
            #endregion

            #region LeftCurve p4 --> p1
            switch (arrang.LeftSide)
            {
                case "none":
                    bezierpath.AddLine(p4, p1);
                    break;
                case "in":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.VerticalPoints(p4, p1, true));
                    break;
                case "out":
                    bezierpath.AddBeziers(PuzzleBezierovaKrivka.VerticalPoints(p4, p1, false));
                    break;
                default:
                    break;
            }
            #endregion


            //--PRIKLAD-----
            //Point[] bezierPoints1 = PuzzleBezierovaKrivka.HorizontalPoints(p1, p2, true); //static nastaveny
            //Point[] bezierPoints2 = PuzzleBezierovaKrivka.VerticalPoints(p2, p3, false);
            //Point[] bezierPoints3 = PuzzleBezierovaKrivka.HorizontalPoints(p3, p4, true);
            //Point[] bezierPoints4 = PuzzleBezierovaKrivka.VerticalPoints(p4, p1, false);

            //bezierpath.AddBeziers(bezierPoints1);
            //bezierpath.AddBeziers(bezierPoints2);
            //bezierpath.AddBeziers(bezierPoints3);
            //bezierpath.AddBeziers(bezierPoints4);

            return bezierpath;
        }

        // vystrihne jednotlive kusku zo zdrojoveho obrazka za pomoci metody CreatePuzzleShapePath
        private static Bitmap[] CreatePuzzleShapeImagesFromSourceImage(PuzzleGameData data)
        {
            Bitmap[] pShapes = new Bitmap[data.PiecesCount];

            //Pre zjednodesenie vyrezavania pridame hranu hrubky SurroundingSize okolo zdrojoveho obrazka,
            //sourceImage == obrazok + hrany dookola
            int hrubkaHrany = data.PieceSurroundingSize;
            int gridWidth = data.PiecesGridDimensions.Width;
            int gridheight = data.PiecesGridDimensions.Height;
            int pieceWidth = data.PieceDimensions.Width;
            int pieceHeight = data.PieceDimensions.Height;

            Bitmap sourceImage = new Bitmap(data.SourcePicture.Width + 2 * hrubkaHrany,
                                            data.SourcePicture.Height + 2 * hrubkaHrany);

            using (Graphics draw = Graphics.FromImage(sourceImage))
            {
                draw.DrawImage(data.SourcePicture, new Point(hrubkaHrany, hrubkaHrany));
            }

            //Vyrezavanie jednotlivych kuskov
           
            int x = 0, y = 0;
            //Point startP = new Point(x, y);
            //Point endP = new Point((x + pieceWidth + 2 * hrubkaHrany),
            //                       (y + pieceHeight + 2 * hrubkaHrany));
            GraphicsPath shapePath;
                        
            int index;
            for (int j = 0; j < gridheight; j++)
            {
                for (int i = 0; i < gridWidth; i++)
                {
                    index = j * gridWidth + i;
                    pShapes[index] = PictureEditor.CropImage(sourceImage,
                                                             new Point(x, y),
                                                             new Point((x + pieceWidth + 2 * hrubkaHrany),
                                                                       (y + pieceHeight + 2 * hrubkaHrany)));

                    shapePath = CreatePuzzleShapePath(hrubkaHrany, data.PieceDimensions, data.Pieces[index].Arrangement);
                    pShapes[index] = PieceCutter.CutOut(pShapes[index], shapePath);

                    x += pieceWidth;
                }
                x = 0;
                y += pieceHeight;
            }

            return pShapes;
        }

        // prednastavi objekty reprezentujuce jednotlive kusky, obrazok kusku, pozicia, kolizne miesta, ...
        public static void SetPiecesImages(PuzzleGameData data)
        {
            //Vytvorime pole vykrojenych obrazkov pre puzzle kusky
            Bitmap[] puzzleShapes = CreatePuzzleShapeImagesFromSourceImage(data);


            //Vykrojene obrazky aplikujeme na kusky
            for (int i = 0; i < data.PiecesCount; i++)
            {
                data.Pieces[i].PieceImage = puzzleShapes[i];
            }
        }

        // hracia plocha je logicky rozdelena na nadoby, kde kazda nadoba ma svoje vlastne suradnice
        // vrati poziciu nadoby s puzzle kuskami pre zadane suradnice vacsinou z mysi
        public static Point BucketCoordinates(Size realPieceSize, Point coordinates) 
        {
            return new Point((int)(coordinates.X / realPieceSize.Width), (int)(coordinates.Y / realPieceSize.Height));
        }

        // funckia vracia rohy kuska, aby sme neskor vedeli, kde
        public static Point[] RealPieceCornerPoints(Point currPiecePosition, Size realPieceSize)
        {
            Point[] corners = new Point[4];
            corners[0] = new Point(currPiecePosition.X, currPiecePosition.Y);
            corners[1] = new Point(currPiecePosition.X + realPieceSize.Width, currPiecePosition.Y);
            corners[2] = new Point(currPiecePosition.X + realPieceSize.Width, currPiecePosition.Y + realPieceSize.Height);
            corners[3] = new Point(currPiecePosition.X, currPiecePosition.Y + realPieceSize.Height);

            return corners;
        }

        // funkcia nahodne rozmiestni puzzle kusky po hracom poli
        public static void RandomizePiecesLocations(PuzzleGameData data)
        {
            Random rnd = new Random();
            Point startPos, endPos;
            
            startPos = new Point(data.PieceSurroundingSize, data.PieceSurroundingSize);
            endPos = new Point((data.GameBoard.Width - (2 * data.PieceSurroundingSize + data.PieceDimensions.Width)),
                               (data.GameBoard.Height - (2 * data.PieceSurroundingSize + data.PieceDimensions.Height)));
            foreach (var piece in data.Pieces)
            {
                //nahodna pozicia kuska v ramci hracej plochy
                piece.CurrentPosition = new Point(rnd.Next(startPos.X, endPos.X), rnd.Next(startPos.Y, endPos.Y));

                //registracia kuska do virtualnej hracej sachovnice, kde policko v sachovnici reprezentuje skupinu vsetkych kuskov puzzle ktore sa ho priamo dotykaju(prekryvaju)
                RegisterPieceInGrid(data, piece);
                
            }

            //using (var streamWriter = new System.IO.StreamWriter("output.txt"))
            //{
            //    foreach (var item in data.bucketOfPieces)
            //       streamWriter.WriteLine("[{0} {1}]", item.Key, item.Value);
            //}
        }

        // generuje povodne pozicie kuskov, pouzite funkciou SetOriginalPiecesLocations
        private static Point[] GenerateOriginalPiecesLocations(Point startPos, Size pieceDim, int surroundSize, Size gridDim)
        {
            int width = gridDim.Width;
            int height = gridDim.Height;
            int pWidth = pieceDim.Width;
            int pHeight = pieceDim.Height;

            //nastavenia povodnych pozic puzzle vratane ich oramonavia(miesta kde sa prekryvaju zuby puzzle)
            Point[] locations = new Point[width * height];
            startPos.X -= surroundSize;
            startPos.Y -= surroundSize;
            Point currPos = startPos;

            int index;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    index = j * width + i;
                    locations[index] = currPos;
                    currPos.X += pWidth;
                }
                currPos.X = startPos.X;
                currPos.Y += pHeight;
            }
            return locations;
        }

        // pozicia puzzle kuska vramci poskladaneho puzzle, pouzite pri vypocte spravnosti zlozenych kuskov
        public static void SetOriginalPiecesLocations(PuzzleGameData data)
        {
            Point[] locations = GenerateOriginalPiecesLocations(data.GameBoardStartPosition,
                                                                data.PieceDimensions,
                                                                data.PieceSurroundingSize,
                                                                data.PiecesGridDimensions);

            for (int i = 0; i < data.PiecesCount; i++)
            {
                data.Pieces[i].OriginalPosition = locations[i];
                data.Pieces[i].CurrentPosition = locations[i];
            }
        }

        // generuje nahodne strany pre zadany kusok, nie tvary, iba ci bude zubok dovnutra alebo von, zvyslo, vodorovne
        private static PieceArrangement[] GeneratePiecesArrangement(Size gridDimensions) //gridDimensions rozmery pre pocet kuskov v riadkoch a stlpcoch
        {
            int width = gridDimensions.Width;
            int height = gridDimensions.Height;
            PieceArrangement[] arrangement = new PieceArrangement[width * height];
            Random rnd = new Random();

            int index;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    index = j * width + i; //pole cislujeme od 0 preto i namiesto i+1, vytvarame poziciu indexu 2d pola v 1d poli
                    arrangement[index] = new PieceArrangement();

                    #region TopSide
                    if (j == 0)
                    {
                        arrangement[index].TopSide = "none";
                    }
                    else if (arrangement[index - width].BottomSide == "in")
                    {
                        arrangement[index].TopSide = "out";
                    }
                    else
                    {
                        arrangement[index].TopSide = "in";
                    }
                    #endregion

                    #region LeftSide
                    if (i == 0)
                    {
                        arrangement[index].LeftSide = "none";
                    }
                    else if (arrangement[index - 1].RightSide == "in")
                    {
                        arrangement[index].LeftSide = "out";
                    }
                    else
                    {
                        arrangement[index].LeftSide = "in";
                    }
                    #endregion

                    #region BottomSide
                    if (j == (height - 1))
                    {
                        arrangement[index].BottomSide = "none";
                    }
                    else if (rnd.NextDouble() >= 0.5)
                    {
                        arrangement[index].BottomSide = "out";
                    }
                    else
                    {
                        arrangement[index].BottomSide = "in";
                    }
                    #endregion

                    #region RightSide
                    if (i == (width - 1))
                    {
                        arrangement[index].RightSide = "none";
                    }
                    else if (rnd.NextDouble() >= 0.5)
                    {
                        arrangement[index].RightSide = "out";
                    }
                    else
                    {
                        arrangement[index].RightSide = "in";
                    }
                    #endregion

                }
            }

            return arrangement;
        }
        
        // nastavi jednotlivym kuskom ich tvary stran       
        public static void SetPiecesArrangement(PuzzleGameData data)
        {
            if (data.Pieces != null)
            {
                PieceArrangement[] arrang = GeneratePiecesArrangement(data.PiecesGridDimensions);

                for (int i = 0; i < data.PiecesCount; i++)
                {
                    data.Pieces[i].Arrangement = arrang[i];
                }
            }
            else
            {
                MessageBox.Show("Neboli vytvorene instancie PuzzlePiece!","Runtime Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // vracia objekty puzzle novych kuskov, este bez pociatocnych nastaveni 
        public static void CreatePieces(PuzzleGameData data)
        {
            int pocetKuskov = data.PiecesCount;
            data.Pieces = new List<PuzzlePiece>();
            
            for (int i = 0; i < pocetKuskov; i++)
            {
                data.Pieces.Add(new PuzzlePiece());
                data.Pieces[i].ID = i;
                data.Pieces[i].SizeOfPieceImage = new Size(data.PieceDimensions.Width + 2 * data.PieceSurroundingSize,
                                               data.PieceDimensions.Height + 2 * data.PieceSurroundingSize);
                data.Pieces[i].SizeOfPiece = data.PieceDimensions;
                // zatial nie je spojeny s dalsimi kuskami
                data.Pieces[i].LeftNeighbor = null;
                data.Pieces[i].TopNeighbor = null;
                data.Pieces[i].RightNeighbor = null;
                data.Pieces[i].BottomNeighbor = null;

                // povodni susedia, nastavime neskor pomocou SetPiecesOriginalNeighbours()
                data.Pieces[i].OriginalLeftNeighbor = null;
                data.Pieces[i].OriginalTopNeighbor = null;
                data.Pieces[i].OriginalRightNeighbor = null;
                data.Pieces[i].OriginalBottomNeighbor = null;

                // ci su kusky na spravnom mieste
                data.Pieces[i].IsInTheRightPlace_Current = false;
                data.Pieces[i].IsInTheRightPlace_Final = false;

                data.Pieces[i].VisitState = false;


            }
        }

        //pre kazdy kusok nastavi ich originalnych susedov
        public static void SetPiecesOriginalNeighbours(PuzzleGameData data)
        {
            int indexCurrPiece;

            int gridWidth = (data.PiecesGridDimensions.Width - 1);
            int gridHeight = (data.PiecesGridDimensions.Height - 1);
            int maxArrayIndex = (data.PiecesCount - 1);
            List<PuzzlePiece> pieces = data.Pieces;

            for (int j = 0; j < (data.PiecesGridDimensions.Height - 1); j++)
            {
                for (int i = 0; i < (data.PiecesGridDimensions.Width - 1); i++)
                {
                    // jednorozmere pole prechadzame ako dvojrozmerne
                    indexCurrPiece = (j * gridWidth + i);

                    // up neighbour
                    if ((indexCurrPiece - gridWidth) >= 0)
                    {
                        pieces[indexCurrPiece].OriginalTopNeighbor = pieces[indexCurrPiece - gridWidth];
                    }

                    // right neighbour
                    if ((i + 1) <= gridWidth)
                    {
                        pieces[indexCurrPiece].OriginalRightNeighbor = pieces[indexCurrPiece + 1];
                    }

                    // down neighbour
                    if ((indexCurrPiece + gridWidth) <= maxArrayIndex)
                    {
                        pieces[indexCurrPiece].OriginalBottomNeighbor = pieces[indexCurrPiece + gridWidth];
                    }

                    // left neighbour
                    if ((i - 1) >= 0)
                    {
                        pieces[indexCurrPiece].OriginalLeftNeighbor = pieces[indexCurrPiece - 1];
                    }
                }
            }
        }

        // vraci nadobu pod mysou, pouziva funkciu BucketCoordinates
        public static List<PuzzlePiece> BucketUnderMouse(PuzzleGameData data, Point currMouseCoords) //Skupina puzzle kuskov nachadzajuca sa pod suradnicami mysi
        {
            //
            //Vyberame skupinku kuskov najblizsiu k ukazovatelovi mysi (ak skupinka neexistuje, return null).
            //
            List<PuzzlePiece> value;
            Size realPieceSize = data.PieceDimensions;
            realPieceSize.Width += 2 * data.PieceSurroundingSize;
            realPieceSize.Height += 2 * data.PieceSurroundingSize;

            Point bucketCoords = PuzzleGameUtilities.BucketCoordinates(realPieceSize, currMouseCoords);
            
            if (data.bucketOfPieces.TryGetValue(bucketCoords, out value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }
        
        // ak kusok polozime na novu poziciu, pridame ho do nadob, nad ktorymi su jeho rohy vyskytuju
        public static void RegisterPieceInGrid(PuzzleGameData data, PuzzlePiece piece)
        {
            List<PuzzlePiece> value;
            Point[] realPieceCorners;

            realPieceCorners = RealPieceCornerPoints(piece.CurrentPosition, piece.SizeOfPieceImage); //zistime z ktorymi polickami sa pretinaju rohy kuska
            foreach (var cornerPosition in realPieceCorners)
            {
                if (data.bucketOfPieces.TryGetValue(BucketCoordinates(piece.SizeOfPieceImage, cornerPosition), out value))
                {
                    if (value.Last() != piece) // vylucime moznost aby sme zaznamenali viacero rohov kuska do jedneho policka
                    {
                        value.Add(piece);
                    }
                }
                else
                {
                    value = new List<PuzzlePiece>();
                    value.Add(piece);
                    data.bucketOfPieces.Add(BucketCoordinates(piece.SizeOfPieceImage, cornerPosition), value);
                }
            }
        }

        // ak kusok odoberame z pozicie kde bol, odoberieme ho aj z nadob, nad ktorymi boli jeho rohy
        public static void DeregisterPieceFromGrid(PuzzleGameData data, PuzzlePiece piece)
        {
            List<PuzzlePiece> value;
            Point[] pieceToRemove_Corners;
            pieceToRemove_Corners = RealPieceCornerPoints(piece.CurrentPosition,   //ziskame rohy kuska, na ktory prave ukazujeme
                                                          piece.SizeOfPieceImage); //aby sme ho odobrali zo vsetkych policok kde predtym bol

            foreach (var corner in pieceToRemove_Corners)
            {
                if (data.bucketOfPieces.TryGetValue(BucketCoordinates(piece.SizeOfPieceImage, corner), out value))
                {
                    value.Remove(piece); //odoberieme zo vsetkych List<PuzzlePiece> skupiniek(policok) kde kusok patril
                                         //metoda Remove() vracia false ak nebol najdeny objekt pre zmazanie

                    if (!value.Any())     //ak je policko sachovnice (List<>) po odobrani kuska prazdne, odoberieme ho zo zaznamu 
                    {
                        data.bucketOfPieces.Remove(BucketCoordinates(piece.SizeOfPieceImage, corner));
                        value = null;
                    }
                }
            }
        }

        // pomocna funkcia na zistenie mena control-u, ak chceme control odobrat alebo pridat podla jeho mena, funkcia nam vrati objekt typu control
        public static Control ControlByName(Control control, string name) //najde control podla mena zo zadaneho parent control
        {
            Control result = null;
            foreach (Control item in control.Controls)
            {
                if (item.Name == name)
                {
                    result = item;
                    break;
                }
            }

            return result;
        }

        

        private static void TraversePieceNeighbours(List<PuzzlePiece> pieces, PuzzlePiece piece, int gridWidth, Point pieceCoords, ref int count)
        {
            // ak nastane pripad, ze dany kusok este nebol pripojeny k ziademu inemu, teda ze nema ziadnych susedov
            // tak z funkcie vyskocime
            if ((piece.TopNeighbor == null) &&
                (piece.RightNeighbor == null) &&
                (piece.BottomNeighbor == null) &&
                (piece.LeftNeighbor == null))
            {
                // vyhlasime ho za spravne ulozeny, aj ked nie je k nikomu pripojeny, kedze to sa za chybu nepovazuje
                // za chybu sa povazuje kusok, ktory je niekde pripojeny, ale nespravne
                piece.IsInTheRightPlace_Current = true;
                return;
            }

            // ak sme este nenavstivili kusok
            if (!piece.VisitState)
            {
                piece.VisitState = true;
            }

            // ak pozicia navstiveneho kuska, odpoveda pozicii spravneho kuska
            // (poziciou je mysleny index v jednorozmernom poli List<PuzzlePiece>)
            if (piece == pieces[pieceCoords.Y * gridWidth + pieceCoords.X])
            {
                // zvysime aktualny pocet spravne ulozenych kuskov (potencionalne najvacsej skupinky)
                count++;

                // o danom kusku vyhlasime ze je mozno clenom najvacsej doposial zlozenej skupinky puzzle
                piece.IsInTheRightPlace_Current = true;
            }
            else
            {
                piece.IsInTheRightPlace_Current = false;
            }

            // navstivime laveho suseda ak existuje a ak este nebol navstiveny a zistime, ci jeho pozicia odpoveda pozicii spravneho suseda
            if (piece.LeftNeighbor != null)
            {
                if (!piece.LeftNeighbor.VisitState)
                {
                    TraversePieceNeighbours(pieces, piece.LeftNeighbor, gridWidth, new Point(pieceCoords.X - 1, pieceCoords.Y), ref count);
                }
            }

            // navstivime horneho suseda ak existuje a ak este nebol navstiveny
            if (piece.TopNeighbor != null)
            {
                if (!piece.TopNeighbor.VisitState)
                {
                    TraversePieceNeighbours(pieces, piece.TopNeighbor, gridWidth, new Point(pieceCoords.X, pieceCoords.Y - 1), ref count);
                }
            }

            // navstivime praveho suseda ak existuje a ak este nebol navstiveny
            if (piece.RightNeighbor != null)
            {
                if (!piece.RightNeighbor.VisitState)
                {
                    TraversePieceNeighbours(pieces, piece.RightNeighbor, gridWidth, new Point(pieceCoords.X + 1, pieceCoords.Y), ref count);
                }
            }

            // navstivime dolneho suseda ak existuje a ak este nebol navstiveny
            if (piece.BottomNeighbor != null)
            {
                if (!piece.BottomNeighbor.VisitState)
                {
                    TraversePieceNeighbours(pieces, piece.BottomNeighbor, gridWidth, new Point(pieceCoords.X, pieceCoords.Y + 1), ref count);
                }
            }
        }
    }   
}


//------old(nevyuzite metody)----
//--------------------------------------------
// zistime spravnost pospajanych kuskov, metoda kuskom nastavi ich vlastnost IsInTheRightPlace_Final
// metoda vracia pocet spravne zlozenych kuskov
//public static int CheckPiecesPlacement(PuzzleGameData data)
//{
//    // v kazdom kusku sa pokusime zanorit do jeho susedov a zistit ci, prave tento kusok nie je clenom doposial najvacsej zlozenej skupinky;
//    // vyznacime len tie kusky, ktore nepatria v skupinke na dane miesto

//    List<PuzzlePiece> pieces = data.Pieces;
//    int gridWidth = data.PiecesGridDimensions.Width - 1;
//    int gridHeight = data.PiecesGridDimensions.Height - 1;

//    int index;
//    int biggestCount = 0, currCount = 0;
//    for (int j = 0; j < gridHeight; j++)
//    {
//        for (int i = 0; i < gridWidth; i++)
//        {
//            index = (j * gridWidth + i);

//            // prejdeme vsetkych susedov(aj ich podsusedov) daneho kuska, zistime v akej velkej skupine spravne zlozenych kuskov sa dany kusok nachadza
//            TraversePieceNeighbours(pieces, pieces[index], gridWidth, new Point(i, j), ref currCount);

//            #region ak su puzzle zlozene uplne spravne, vratime true, a vyskocime z funkcie (nastavime aj VisitState na false)
//            if (currCount == data.PiecesCount)
//            {
//                foreach (var item in pieces)
//                {
//                    item.VisitState = false;
//                }

//                return data.PiecesCount;
//            }

//            #endregion

//            #region ak sme nasli vacsiu skupinu spravne zlozenych kuskov, ako doposial
//            if (currCount > biggestCount)
//            {
//                biggestCount = currCount;
//                currCount = 0;

//                foreach (var item in pieces)
//                {
//                    item.IsInTheRightPlace_Final = item.IsInTheRightPlace_Current;
//                    item.VisitState = false;
//                }
//            }
//            else
//            {
//                currCount = 0;
//                foreach (var item in pieces)
//                {
//                    item.VisitState = false;
//                }
//            }
//            #endregion
//        }
//    }

//    // vratime najvacsi pocet spravne zlozenych kuskov
//    return biggestCount;
//}
//----------------------------------------------------------------------
//public static void ApplyScaling(PuzzleGameData data, double scalingFactor)
//{
//    // ulozime si aktualnu scaling hodnotu pre pouzitie neskor
//    data.ScalingFactor = scalingFactor;

//    // preratame velkost suradnic u kazdehu kusku
//    foreach (var piece in data.Pieces)
//    {
//        piece.CurrentPosition = new Point((int)(piece.CurrentPosition.X * scalingFactor),
//                                          (int)(piece.CurrentPosition.X * scalingFactor));
//    }

//    // zmenime velkost obrazka


//    // 

//}