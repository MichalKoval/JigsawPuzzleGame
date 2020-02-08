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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(custom_KeyDown);
            this.KeyUp += new KeyEventHandler(custom_KeyUp);

        }

        // okno s dialogom pre vyber zdrojoveho obrazka
        private OpenFileDialog fileDialog = new OpenFileDialog();

        // data potrebne pre hru(puzzle kusky a ich nastavenia, nastavenia hracej plochy...)
        private PuzzleGameData gameData = new PuzzleGameData();

        // control starajuci sa o vykreslenie mriezky pre vystrihnutie obrazka
        private GridLayer gridLayer = new GridLayer();

        // hracia plocha, v ktorej sa budu vykreslovat kusky
        private Gameboard gameboard = null;

        // labels pre zobrazenie instrukcii pocas hry + pociatocne nastavenia
        Label choosePictureLabel = new Label();
        Label gameInstructionsLabel = new Label();
        bool showGameInstructions = true;

        //nastavenie farieb, velkosti jednotlivych controls a vztahy medzi controls, viditelnost controls
        private void Form1_Load(object sender, EventArgs e)
        {
            //Color color = ColorTranslator.FromHtml("#0078D7");
            //Color colorPanel3 = ColorTranslator.FromHtml("#0078D7");
            Color color = Color.FromArgb(80,80,80);
            panel1.BackColor = color;
            panel1.Size = new Size(this.Width, this.panel1.Size.Height);
            

            //var buttons = this.Controls.OfType<Button>().ToArray();
            //foreach (var button in buttons)
            //{
            //    button.BackColor = color;
            //    button.ForeColor = Color.White;
            //}
            button1.BackColor = color;
            button1.ForeColor = Color.White;
            button2.BackColor = color;
            button2.ForeColor = Color.White;
            button3.BackColor = color;
            button3.ForeColor = Color.White;
            button4.BackColor = color;
            button4.ForeColor = Color.White;
            //button6.BackColor = color;
            //button6.ForeColor = Color.White;

            pictureBox1.Parent = panel4;
            panel4.Controls.Add(pictureBox1);
            
            gridLayer.Parent = pictureBox1;
            pictureBox1.Controls.Add(gridLayer);

            //
            // labels pre zobrazenie instrukcii pocas hry(pri vybere obrazka, pokyny ako ovladat hru)
            //
            // uvodny text ziada po uzivatelovi vyber obrazka
            panel4.Controls.Add(choosePictureLabel);
            choosePictureLabel.AutoSize = false;
            choosePictureLabel.Width = this.panel4.Width;
            choosePictureLabel.Height = this.panel4.Height;
            choosePictureLabel.Text = "Vyberte obrázok pre puzzle";
            choosePictureLabel.TextAlign = ContentAlignment.MiddleCenter;
            choosePictureLabel.Left = 0;
            choosePictureLabel.ForeColor = Color.Silver;
            choosePictureLabel.Font = new Font(choosePictureLabel.Font.FontFamily, 40, FontStyle.Bold);
            choosePictureLabel.Visible = true;
            choosePictureLabel.BringToFront();

            // ak nie je ziaden obrazok v puzzle editore
            // tento label sa ukaze az pri kliknuti na tlacidlo Play, ak sme nezvoli obrazok
            noPictureLabel.Visible = false;

            // label pre zobrazenie instrukcii pocas hry
            gameInstructionsLabel.AutoSize = false;
            gameInstructionsLabel.Top = 20;
            gameInstructionsLabel.Left = 20;
            gameInstructionsLabel.Width = this.panel6.Width - 60;
            gameInstructionsLabel.Height = this.panel6.Height;
            gameInstructionsLabel.Font = new Font(choosePictureLabel.Font.FontFamily, 9, FontStyle.Bold);
            gameInstructionsLabel.ForeColor = Color.DarkCyan;
            gameInstructionsLabel.Text = "Ľavé tlačidlo myši --> výber a pohyb kúska po hracej ploche." + Environment.NewLine +
                                         "Pravé tlačidlo myši --> posun hracej plochy." + Environment.NewLine +
                                         "Koliesko myši --> zväčšiť / zmenšiť hraciu plochu" + Environment.NewLine +
                                         "D + ľavé tlačidlo myši --> odpojiť kúsok od skupinky";
            this.panel6.Controls.Add(gameInstructionsLabel);
            this.panel6.BringToFront();
            gameInstructionsLabel.BringToFront();
            this.panel6.Visible = false;

            this.autorLabel.BringToFront();
            this.autorLabel.Visible = true;

            // obsahuje button3=="Restart", button4=="Nova hra"
            // tento panel s tlacidlami sa ukaze az po stlaceni tlacidla Play a zaciatku hry
            panel5.Visible = false;

        }

                
        // funkcia sluzi na vyvolanie dialogoveho okna pre vyber obrazka
        private bool LoadImage()
        {
            fileDialog.Filter = "Obrázky(*.BMP; *PNG; *.JPG)| *.BMP; *PNG; *.JPG | Všetky súbory(*.*) | *.*";
            //filter ----file description----|----file types-------| a znova -------file description------|----file types----

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = Path.GetFileName(fileDialog.FileName); //chceme iba nazov subory bez cesty
                Bitmap newPicture = new Bitmap(fileDialog.FileName);

                using (MemoryStream file = new MemoryStream())      //chceme previest konverziu bez ukladania na lokal. disk
                {
                    newPicture.Save(file, ImageFormat.Png);         //docastne si to ulozime do memory strem-u
                    gameData.SourcePicture = new Bitmap(file);
                }
                return true;
            }
            else
            {
                // nebudeme odkazovat na null, lebo mohol byt pred tym otvoreny iny obrazok a stratime nanho referenciu!!
                //gameData.SourcePicture = null; //ak sme nevybrali obrazok
                return false;
            }
        }

        // otvori sa dialogove okno pre vyber obrazka, nastavy sa zdrojovy obrazok
        private void button1_Click(object sender, EventArgs e)
        {
            noPictureLabel.Visible = false;
            bool result = LoadImage();
            if (gameData.SourcePicture != null)
            {
                // ak sme nacitali obrazok a dany obrazok je iny ako obrazok, ktory tam uz bol
                if (result)
                {
                    // ak ano predpripravime inu mriezku a prekreslime obrazok, inac nie
                    pictureBox1.Visible = false;
                    gridLayer.Configure(gameData, this);
                }
                
                choosePictureLabel.Visible = false;
                autorLabel.Visible = false;
                pictureBox1.Visible = true;
            }
        }

        // spustenie hry, nastavia sa vsetky potrebne nastavenia a zobrazi sa hracia
        // plocha s puzzle kuskami a oknom s instrukciami(len pri prvom spusteni programu)
        private void button2_Click(object sender, EventArgs e)
        {
            if (gameData.SourcePicture != null)
            {
                textBox1.Visible = false;
                pictureBox1.Visible = false;
                button1.Visible = false;
                button2.Visible = false;
                panel5.Visible = true;  //obsahuje button3==Restart, button4==NewGame

                //
                //nastavime pociatocne data pre hru
                //
                gameData.SourcePicture = PictureEditor.CropImage(gameData.SourcePicture, gridLayer.StartCutLocation, gridLayer.EndCutLocation);
                gameData.PiecesGridDimensions = gridLayer.GridDimensions;
                gameData.PiecesCount = gameData.PiecesGridDimensions.Width * gameData.PiecesGridDimensions.Height;
                gameData.PieceDimensions = gridLayer.PieceDimensions;
                gameData.PieceSurroundingSize = (int)Math.Ceiling(gameData.PieceDimensions.Width * 0.163);
                gameData.GameBoard = this.panel4;
                gameData.GameBoardStartPosition = new Point(50, 50);

                PuzzleGameUtilities.CreatePieces(gameData);
                PuzzleGameUtilities.SetOriginalPiecesLocations(gameData);
                PuzzleGameUtilities.SetPiecesArrangement(gameData);
                PuzzleGameUtilities.SetPiecesImages(gameData);
                PuzzleGameUtilities.SetPiecesOriginalNeighbours(gameData);
                PuzzleGameUtilities.RandomizePiecesLocations(gameData);

                
                //
                //Pociatocne nastavenia gameboard-u
                //
                gameboard = new Gameboard(this, gameData, panel4.Size);
                //gameboard ako instancia Form nesmie byt nastavena na top level control !!
                gameboard.TopLevel = false;
                panel4.Controls.Add(gameboard);
                gameboard.Visible = false;
                gameboard.Anchor = AnchorStyles.None;
                gameboard.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);
                gameboard.Location = new Point(0, 0); //pozicia vo vnutri panel4
                gameboard.Visible = true;

                if (showGameInstructions)
                {
                    this.panel6.BringToFront();
                    this.panel6.Visible = true;
                    // zabezpecime aby sa to ukazalo iba pri prvom spusteni
                    showGameInstructions = false; 
                }

            }
            else
            {
                noPictureLabel.Visible = true;
                // MessageBox.Show("Nebol vybraný obrazok!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Button = restart hry, vygeneruju sa nove nahodne pozicie pre puzzle kusky
        private void button3_Click(object sender, EventArgs e)
        {
            gameData.bucketOfPieces.Clear();
            // zrusime susedov
            foreach (var piece in gameData.Pieces)
            {
                piece.LeftNeighbor = null;
                piece.TopNeighbor = null;
                piece.RightNeighbor = null;
                piece.BottomNeighbor = null;
            }
            PuzzleGameUtilities.RandomizePiecesLocations(gameData);
            gameboard.ResetBackgroundCapture();
        }

        // Button = nova hra, zahodia sa objekty reprezentujuce staru hru, prednastavenia pred dalsou hrou
        private void button4_Click(object sender, EventArgs e)
        {
            // zoomLabel odstranime z panelu3 (tenky pasik)
            this.panel3.Controls.Clear();

            // gameboard sa prestane zobrazovat v panel-y
            this.gameboard.Visible = false;

            // odoberieme gameboard z panela, pred tym nez ho zahodime
            this.panel4.Controls.Remove(PuzzleGameUtilities.ControlByName(panel4, "gameboard"));

            // zahodime staru hraciu plochu
            gameboard = null;

            // zahodime stare data pre hru
            gameData = null;

            // vytvorime nove data pre hru
            gameData = new PuzzleGameData();
            
            // naspat zobrazime controls pre vyber obrazka
            textBox1.Visible = true;
            textBox1.Text = "";
            pictureBox1.Image = null;
            pictureBox1.Visible = false;
            choosePictureLabel.Visible = true;
            choosePictureLabel.BringToFront();
            autorLabel.BringToFront();
            autorLabel.Visible = true;

            button1.Visible = true;
            button2.Visible = true;

            panel5.Visible = false;  //obsahuje button3==Restart, button4==NewGame

            if (showGameInstructions)
            {
                showGameInstructions = false;
                this.panel6.Visible = false;
            }
        }

        // pre zmene okna vycentrujeme text s instrukciami
        private void Form1_Resize(object sender, EventArgs e)
        {
            choosePictureLabel.Width = this.panel4.Width;
            choosePictureLabel.Height = this.panel4.Height;
            this.panel6.Location = new Point((int)((this.panel4.Width - this.panel6.Width) / 2),
                                             (int)((this.panel4.Height - this.panel6.Height) / 2));
        }

        // zmyzne okienko s intrukciami pre uzivatela
        private void button5_Click(object sender, EventArgs e)
        {
            this.panel6.Visible = false;
        }

        // skontrolujeme doposial zlozene kusky, a vyznacime tie co su nespravne
        //private void button6_Click(object sender, EventArgs e)
        //{
        //    // najprv prejdeme kusky a skontrolujeme ich spravnost
        //    PuzzleGameUtilities.CheckPiecesPlacement(gameData);

        //    // nasledne zvyraznime tie kusky, ktore nepatria na dane miesto
        //    foreach (var piece in gameData.Pieces)
        //    {
        //        Debug.WriteLine("ID {0}, VisitState {1}, Pos {2}, CorrectPlace {3}", piece.ID, piece.VisitState, piece.CurrentPosition, piece.IsInTheRightPlace_Final);
        //        if (piece.TopNeighbor != null)
        //        {
        //            Debug.Write("Top {0}," + piece.TopNeighbor.ID.ToString());
        //        }

        //        if (piece.RightNeighbor != null)
        //        {
        //            Debug.Write("Right {0}," + piece.RightNeighbor.ID.ToString());
        //        }

        //        if (piece.BottomNeighbor != null)
        //        {
        //            Debug.Write("Bottom {0}," + piece.BottomNeighbor.ID.ToString());
        //        }

        //        if (piece.LeftNeighbor != null)
        //        {
        //            Debug.Write("Left {0}," + piece.LeftNeighbor.ID.ToString());
        //        }
        //        Debug.WriteLine("");                               
        //        Debug.WriteLine("------------------------------------------------------------------");
        //        if (!piece.IsInTheRightPlace_Final)
        //        {
        //            piece.PieceImageClickDown();
        //            gameboard.RepaintAreaUnderPiece(piece);
        //        }
        //        gameboard.Invalidate(false);
        //    }
        //}

        // ak sme stacili klavesu D
        private void custom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                if (gameData != null)
                {
                    gameData.key_D_Down = true;
                                    
                    //Debug.WriteLine("Stlacene D");             
                }
            }
        }

        // ak sme klavesu D pustili
        private void custom_KeyUp(object sender, KeyEventArgs e)
        {
            if (gameData != null)
            {
                gameData.key_D_Down = false;
            }
        }
    }
}


//Old Gameboard(predchadzajuci navrhy)----------------------------------------------------------

//private Point locationMouseDown = Point.Empty;
//private PuzzlePiece currentPiece;

//public void customPuzzlePiece_MouseDown(object sender, MouseEventArgs e)
//{
//    PuzzlePiece puzzlePiece = sender as PuzzlePiece;

//    if (e.Button == MouseButtons.Left)
//    {
//        locationMouseDown = new Point(e.X, e.Y);
//    }

//}

//public void customPuzzlePiece_MouseUp(object sender, MouseEventArgs e)
//{
//    locationMouseDown = Point.Empty;
//}

//private int currID = -1;
//private bool collision = false;
//private PuzzlePiece previousPuzzlePiece = null;

//public void customPuzzlePiece_MouseMove(object sender, MouseEventArgs e)
//{
//    PuzzlePiece puzzlePiece = sender as PuzzlePiece;

//    if (locationMouseDown == Point.Empty)
//    {
//        for (int i = (gameData.PiecesCount - 1); i >= 0; i--) //idem v poradi zhora dole aby sa vybral ten kusok vyssie ak sa dve prekryvaju..
//        {
//            if (IsInCollisionWithMouse(gameData.Pieces[i], new Point(e.X, e.Y))) //ak mys ukazuje na viacero kuskov naraz zvyrazni sa najvyssi kusok
//            {
//                currID = i;
//                collision = true;
//                break; //zarazka
//            }
//            else { collision = false;
//                currentPiece = null;
//            }
//        }
//        //if (collision)
//        //{
//        //    gameData.Pieces[currID].PieceImageClickDown();
//        //    //previousPuzzlePiece = puzzlePiece;
//        //}

//    }

//    if ((locationMouseDown != Point.Empty) && collision) //doslo k stlaceniu mysi v bode a zaroven mys ukazuje na puzzle piece
//    {
//        if ((currID > 0) && (currID < (gameData.PiecesCount - 1)))
//        {
//            gameData.Pieces[currID + 1].Parent = gameData.Pieces[currID - 1];
//            gameData.Pieces[currID].Parent = gameData.Pieces[gameData.PiecesCount - 1];
//            gameData.Pieces[currID].BringToFront(); //vramci controls to presunieme puzzle piece na vrch
//            var item = gameData.Pieces[currID];     //to iste urobime aj v ramci poradia v List<PuzzlePiece>
//            gameData.Pieces.RemoveAt(currID);
//            gameData.Pieces.Add(item);

//            foreach (var piece in gameData.Pieces)
//            {
//                piece.Refresh();
//            }
//            currID = (-1);
//            currentPiece = item;

//            //
//            //Point newposition = currentPiece.CurrentPosition;
//            //newposition.X += e.X - locationMouseDown.X;
//            //newposition.Y += e.Y - locationMouseDown.Y;
//            //currentPiece.CurrentPosition = newposition;
//            //locationMouseDown = new Point(e.X, e.Y);
//            //currentPiece.Refresh();
//            //
//        }

//        if (currentPiece == (gameData.Pieces[gameData.PiecesCount - 1]))
//        {
//            Point newposition = currentPiece.CurrentPosition;
//            newposition.X += e.X - locationMouseDown.X;
//            newposition.Y += e.Y - locationMouseDown.Y;
//            currentPiece.CurrentPosition = newposition;
//            locationMouseDown = new Point(e.X, e.Y);
//            currentPiece.Refresh();
//        }

//        //if (collision)
//        //{
//        //    Point newposition = puzzlePiece.CurrentPosition;
//        //    newposition.X += e.X - locationMouseDown.X;
//        //    newposition.Y += e.Y - locationMouseDown.Y;
//        //    puzzlePiece.CurrentPosition = newposition;
//        //    locationMouseDown = new Point(e.X, e.Y);
//        //    puzzlePiece.Refresh();
//        //}
//    }
//}

//-----------------------------------------------------------------------

//Old methods---------------------------------------------
//private void whenTextBox1_Changed(object sender, EventArgs e) //ak doslo k zmene gridWidth
//{
//    TextBox box = sender as TextBox;
//    int value;
//    if ((gameData.SourcePicture != null) && (int.TryParse(box.Text, out value)))
//    {
//        int maxWidth = gameData.SourcePicture.Width;
//        value = Math.Abs(value);

//        if ((value * currPieceDimensions.Width) > maxWidth)
//        {
//            box.ForeColor = Color.Red;
//        }
//        else
//        {
//            box.ForeColor = Color.White;
//            currGridDimensions.Width = value;
//            gridLayer.Image = PictureEditor.DrawGridIntoImage(gridRatio, currPieceDimensions,
//                                                          currGridDimensions.Width,
//                                                          currGridDimensions.Height,
//                                                          backGroundImageForGrid);
//        }
//    }
//}

//private void whenTextBox2_Changed(object sender, EventArgs e) //ak doslo k zmene gridHeight
//{
//    TextBox box = sender as TextBox;
//    int value;
//    if ((gameData.SourcePicture != null) && (int.TryParse(box.Text, out value)))
//    {
//        int maxHeight = gameData.SourcePicture.Height;
//        value = Math.Abs(value);

//        if ((value * currPieceDimensions.Width) > maxHeight)
//        {
//            box.ForeColor = Color.Red;
//        }
//        else
//        {
//            box.ForeColor = Color.White;
//            currGridDimensions.Height = value;
//            gridLayer.Image = PictureEditor.DrawGridIntoImage(gridRatio, currPieceDimensions,
//                                                          currGridDimensions.Width,
//                                                          currGridDimensions.Height,
//                                                          backGroundImageForGrid);
//        }
//    }
//}

//private void whenTextBox3_Changed(object sender, EventArgs e) //ak doslo k zmene rozmeru puzzle kuska
//{
//    TextBox box = sender as TextBox;
//    int value;
//    if ((gameData.SourcePicture != null) && (int.TryParse(box.Text, out value)))
//    {
//        int maxWidth = gameData.SourcePicture.Width;
//        int maxHeight = gameData.SourcePicture.Height;
//        value = Math.Abs(value);

//        if (((value * currGridDimensions.Width) > maxWidth) || ((value * currGridDimensions.Height) > maxHeight))
//        {
//            box.ForeColor = Color.Red;
//        }
//        else
//        {
//            box.ForeColor = Color.White;
//            currPieceDimensions.Width = value;
//            currPieceDimensions.Height = value;
//            gridLayer.Image = PictureEditor.DrawGridIntoImage(gridRatio, currPieceDimensions,
//                                                          currGridDimensions.Width,
//                                                          currGridDimensions.Height,
//                                                          backGroundImageForGrid);
//        }
//    }
//}


//public void mouseMoveGridLayer(object sender, MouseEventArgs e)
//{
//    PictureBox gridbox = sender as PictureBox;

//    //if (pictbox.SizeMode == PictureBoxSizeMode.CenterImage)
//    //{
//    //    pictbox.SizeMode = PictureBoxSizeMode.Normal;
//    //}

//    if (locationMouseDown != Point.Empty)
//    {
//        Padding newpadding = gridbox.Padding;   //pomocou paddingu posuvame obrazok v picturebox-e
//        newpadding.Left += e.X - locationMouseDown.X;
//        newpadding.Top += e.Y - locationMouseDown.Y;
//        newpadding.Bottom = 0;
//        newpadding.Right = 0;
//        gridbox.Padding = newpadding;
//        locationMouseDown = new Point(e.X, e.Y);
//        Point middleP = new Point(gridbox.Padding.Left + (int)(gridbox.Image.Size.Width / 2),
//                                  gridbox.Padding.Top + (int)(gridbox.Image.Size.Height / 2));

//        textBoxWidth.Location = new Point(middleP.X - 25, middleP.Y - 40);
//        textBoxHeight.Location = new Point(middleP.X - 25, middleP.Y - 10);
//        textBoxDimension.Location = new Point(middleP.X - 25, middleP.Y + 20);
//        gridbox.Refresh();
//    }
//}

//private void clientSizeChanged_GridLayer(object sender, EventArgs e)
//{
//    double ratioWidth = (double)(pictureBox1.ClientSize.Width) / (double)(pictureBox1.Image.Size.Width);
//    double ratioHeight = (double)(pictureBox1.ClientSize.Height) / (double)(pictureBox1.Image.Size.Height);

//    gridRatio = Math.Min(ratioWidth, ratioHeight);
//    label1.Text = gridRatio.ToString("0.0000");

//    gridLayer.Image = PictureEditor.DrawGridIntoImage(gridRatio, currPieceDimensions,
//                                                      currGridDimensions.Width,
//                                                      currGridDimensions.Height,
//                                                      backGroundImageForGrid);
//}



//---------------------------------------------
//backGroundImageForGrid = PictureEditor.ColouredBackGroundImage(Color.DimGray, gameData.SourcePicture.Size); //Color.RoyalBlue
//backGroundImageForGrid = PictureEditor.ImageOpacity(0.45F, backGroundImageForGrid);
//currGridDimensions = new Size(3, 3);
//currPieceDimensions = new Size(100, 100);

//gridRatio = (double)(pictureBox1.ClientSize.Width) / (double)(pictureBox1.Image.Size.Width);
//label1.Text = gridRatio.ToString("0.0000");

//gridLayer.Image = PictureEditor.DrawGridIntoImage(gridRatio, currPieceDimensions,
//                                                  currGridDimensions.Width,
//                                                  currGridDimensions.Height,
//                                                  backGroundImageForGrid);
////
////na zaciatku nastavime grid a textbox-y do stredu gridLayer(gridLayer == picturebox ponad zdrojovym obrakom)
////
//Point middleP = new Point((int)(gridLayer.ClientSize.Width / 2),    //bod stredu v gridLayer
//                          (int)(gridLayer.ClientSize.Height / 2));

//gridLayer.Padding = new Padding(middleP.X - gridLayer.Image.Size.Width,
//                                middleP.Y - gridLayer.Image.Size.Height,
//                                0, 0);
//gridLayer.Refresh(); //aby po posunuti paddingu, resp. posunu obrazka v picturebox-e doslo k viditelnej zmene

//textBoxWidth.Location = new Point(middleP.X - 26, middleP.Y - 40);
//textBoxHeight.Location = new Point(middleP.X - 26, middleP.Y - 10);
//textBoxDimension.Location = new Point(middleP.X - 26, middleP.Y + 20);
////
////do textboxov vyplnime pociatocne hodnoty rozmerov grid, a velkost kuska puzzle
////
//textBoxWidth.Text = (currGridDimensions.Width).ToString();
//textBoxHeight.Text = (currGridDimensions.Height).ToString();
//textBoxDimension.Text = (currPieceDimensions.Width).ToString();
////
////Textbox-y nastavime na viditelne ich pozicia je vzdy ponad gridLayer, kedze su to jej potomkovia(jej controls)
////
//textBoxWidth.Visible = true;
//textBoxHeight.Visible = true;
//textBoxDimension.Visible = true;
//gridLayer.Refresh();


//private const int puzzleCount = 2;

//private PictureBox[] listOfPictureBoxes = new PictureBox[puzzleCount];

//private void SetPictureBoxes()        
//{
//    Random rnd = new Random();

//    for (int i = 0; i < listOfPictureBoxes.Count(); i++)
//    {
//        listOfPictureBoxes[i] = new PictureBox();
//        //listOfPictureBoxes[i].BackColor = Color.Green;
//        listOfPictureBoxes[i].BackColor = Color.Transparent;
//        listOfPictureBoxes[i].Size = new Size(this.Width - 200, this.Height);
//        listOfPictureBoxes[i].Location = new Point(0, 0);
//        listOfPictureBoxes[i].Padding = new Padding(rnd.Next(10, this.Width), rnd.Next(10, this.Height),0,0);
//        //listOfPictureBoxes[i].Paint += customPictureBox_Paint;
//        listOfPictureBoxes[i].MouseDown += customPuzzlePiece_MouseDown;
//        listOfPictureBoxes[i].MouseUp += customPuzzlePiece_MouseUp;
//        listOfPictureBoxes[i].MouseMove += customPuzzlePiece_MouseMove;
//        //listOfPictureBoxes[i].Image = picture1;
//        //listOfPictureBoxes[i].Padding = new Padding(100, 100, 0, 0);

//        // listOfPictureBoxes[i].Image = listOfImages[i];
//        // listOfPictureBoxes[i].SizeMode = PictureBoxSizeMode.StretchImage;
//        this.Controls.Add(listOfPictureBoxes[i]);

//        if (i > 0)
//        {
//            listOfPictureBoxes[i].Parent = listOfPictureBoxes[i - 1];
//        }
//        listOfPictureBoxes[i].BringToFront();
//    }


//}

//private void DrawAreaUnderBeziersPoints(PaintEventArgs e)
//{
//    // Create pen.
//    Pen blackPen = new Pen(Color.Green, 1);
//    GraphicsPath bezierpath = new GraphicsPath();

//    //Point[] bezierPoints =
//    //         {
//    //     start, control1, control2, end1,
//    //     control3, control4, end2, control5, control6, end3
//    //    // control7, control8, end4
//    // };

//    // PuzzleBezierovaKrivka bodykrivky = new PuzzleBezierovaKrivka(); //zruseny static - treba vytvorit instanciu, asi docasne riesenie !?

//    int locationx = gameData.PieceSurroundingSize; //offset pre posun vyrezu
//    int locationy = gameData.PieceSurroundingSize;
//    int pieceWidth = gameData.PieceDimensions.Width;
//    int pieceHeight = gameData.PieceDimensions.Height;
//    Point p1, p2, p3, p4;
//    p1 = new Point(locationx, locationy);
//    p2 = new Point(locationx + pieceWidth, locationy);
//    p3 = new Point(locationx + pieceWidth, locationy + pieceHeight);
//    p4 = new Point(locationx, locationy + pieceHeight);

//    Point[] bezierPoints1 = PuzzleBezierovaKrivka.HorizontalPoints(p1, p2, true); //static nastaveny
//    Point[] bezierPoints2 = PuzzleBezierovaKrivka.VerticalPoints(p2, p3, false);
//    Point[] bezierPoints3 = PuzzleBezierovaKrivka.HorizontalPoints(p3, p4, true);
//    Point[] bezierPoints4 = PuzzleBezierovaKrivka.VerticalPoints(p4, p1, false);

//    bezierpath.AddBeziers(bezierPoints1);
//    bezierpath.AddBeziers(bezierPoints2);
//    bezierpath.AddBeziers(bezierPoints3);
//    bezierpath.AddBeziers(bezierPoints4);

//    //e.Graphics.FillPath(Brushes.Green, bezierpath);


//    //e.Graphics.DrawPath(blackPen, bezierpath);
//    //e.Graphics.SetClip(bezierpath);
//    //e.Graphics.DrawImage(picture2, new Point(0,0));
//    //e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
//    //e.Graphics.DrawPath(new Pen(Color.Gray, 1.5F), bezierpath);

//    int x = 80;
//    int y = 80;
//    Point startP = new Point(x, y);
//    Point endP = new Point((x + pieceWidth + 2 * gameData.PieceSurroundingSize),
//                           (y + pieceHeight + 2 * gameData.PieceSurroundingSize));

//    tempImage = PictureEditor.CropImage(picture2, startP, endP);
//    tempImage = PieceCutter.CutOut(tempImage, bezierpath);

//    e.Graphics.DrawImage(tempImage, new Point(50, 50));


//    //e.Graphics.DrawBeziers(blackPen, bezierPoints1);
//}

////private void SetClipPath(PaintEventArgs e)
////{

////    // Create graphics path.
////    GraphicsPath clipPath = new GraphicsPath();
////    clipPath.AddEllipse(0, 0, 200, 100);

////    // Set clipping region to path.
////    e.Graphics.SetClip(clipPath);

////    // Fill rectangle to demonstrate clipping region.
////    e.Graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, 500, 300);
////}

//private void ChangePNGPixels(Bitmap image)  //pokus o nastavovanie jednotlivych pixelov na alfa kanal podla masky..
//{
//    Rectangle recTmp = new Rectangle(0, 0, image.Width, image.Height);

//    // Lock bitmap bits
//    System.Drawing.Imaging.BitmapData imageData = image.LockBits(
//        recTmp,
//        System.Drawing.Imaging.ImageLockMode.ReadWrite,
//        image.PixelFormat);

//    //adresa prveho riadka, pixelu
//    IntPtr pointerFirstRowOfImage = imageData.Scan0;

//    //bajty(bytes) bitmap-y v poli
//    int bytesCount = Math.Abs(imageData.Stride) * image.Height; //pocet bitov v bitmape
//    byte[] rgbaHodnoty = new byte[bytesCount];

//    //prekopirovat RGBA hodnoty obrazka do pola
//    System.Runtime.InteropServices.Marshal.Copy(pointerFirstRowOfImage, rgbaHodnoty, 0, bytesCount);

//    //-----------------------------------------------------------------------
//    //hlavna cast funkcie - nastavenie vybranych pixelov na transparent color

//    for (int i = 3; i < 100000; i=(i+4))
//    {
//        rgbaHodnoty[i] = 0; //kazdy stvrty byte je Alfa kanal == transparetnost
//    }



//    //-----------------------------------------------------------------------

//    //prekopirovat RGBA hodnoty spat do bitmap-y
//    System.Runtime.InteropServices.Marshal.Copy(rgbaHodnoty, 0, pointerFirstRowOfImage, bytesCount);

//    //Unlock bits
//    image.UnlockBits(imageData);

//}

////public Bitmap picture1 = new Bitmap("puzzle1.png");
//public Bitmap picture2 = new Bitmap("image2.png");
//public Bitmap tempImage;

//private Point drawPoint;

////private Point currentMouseLocation = Point.Empty;

//private void Form1_Paint(object sender, PaintEventArgs e)
//{
//    //DrawAreaUnderBeziersPoints(e);
//    //tempImage.Save("puzzle1.png");


//    //Bitmap picture = new Bitmap("image2.png");

//    //ChangePNGPixels(picture1);

//    //e.Graphics.DrawImage(picture1, 10, 10);


//}

//public class CustomPictureBox : Control
//{
//    private readonly Timer refresher;
//    private Image _image;

//    public CustomPictureBox()
//    {
//        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
//        BackColor = Color.Transparent;
//        refresher = new Timer();
//        refresher.Tick += TimerOnTick;
//        refresher.Interval = 50;
//        refresher.Enabled = true;
//        refresher.Start();
//    }

//    protected override CreateParams CreateParams
//    {
//        get
//        {
//            CreateParams cp = base.CreateParams;
//            cp.ExStyle |= 0x20;
//            return cp;
//        }
//    }

//    protected override void OnMove(EventArgs e)
//    {
//        RecreateHandle();
//    }


//    protected override void OnPaint(PaintEventArgs e)
//    {
//        if (_image != null)
//        {
//            e.Graphics.DrawImage(_image, (Width / 2) - (_image.Width / 2), (Height / 2) - (_image.Height / 2));
//        }
//    }

//    protected override void OnPaintBackground(PaintEventArgs e)
//    {
//        //Do not paint background
//    }

//    //Hack
//    public void Redraw()
//    {
//        RecreateHandle();
//    }

//    private void TimerOnTick(object source, EventArgs e)
//    {
//        RecreateHandle();
//        refresher.Stop();
//    }

//    public Image Image
//    {
//        get
//        {
//            return _image;
//        }
//        set
//        {
//            _image = value;
//            RecreateHandle();
//        }
//    }
//}



//public partial class CustomPictureBox : PictureBox
//{
//    protected override void OnPaint(PaintEventArgs e)
//    {
//        if ((this.BackColor == Color.Transparent) && (Parent != null))
//        {
//            Bitmap behind = new Bitmap(Parent.Width, Parent.Height);
//            foreach (Control c in Parent.Controls)
//            {
//                if (c != this && c.Bounds.IntersectsWith(this.Bounds))
//                {
//                    c.DrawToBitmap(behind, c.Bounds);
//                }
//            }
//            e.Graphics.DrawImage(behind, -Left, -Top);
//            behind.Dispose();
//        }
//    }
//}

//public class CustomPictureBox : PictureBox
//{
//    public Image Image
//    {
//        get;
//        set;
//    }

//    public CustomPictureBox()
//    {
//        SetStyle(ControlStyles.AllPaintingInWmPaint |
//                 ControlStyles.SupportsTransparentBackColor, true);
//        base.BackColor = Color.FromArgb(0, 0, 0, 0);//Added this because image wasnt redrawn when resizing form
//    }

//    protected override void OnPaintBackground(PaintEventArgs e)
//    {

//    }

//    protected override void OnPaint(PaintEventArgs e)
//    {
//        if (Image != null)
//        {
//            e.Graphics.DrawImage(Image, 0, 0, Image.Width, Image.Height);
//        }
//    }

//    protected override CreateParams CreateParams
//    {
//        get
//        {
//            CreateParams cp = base.CreateParams;
//            cp.ExStyle |= 0x20;
//            return cp;
//        }
//    }
//}

//private void Form1_MouseMove(object sender, MouseEventArgs e)
//{
//    this.Text = "X = " + e.X + ", Y = " + e.Y;

//    if (!mouseDown)
//    {
//        foreach (var piece in gameData.Pieces)
//        {
//            if (IsInCollisionWithMouse(piece, new Point(e.X, e.Y))) //akmys ukazuje na viacero kuskov naraz zvyrazni sa najvyssi kusok
//            {
//                piece.PieceImageClickDown();
//            }
//            else
//            {
//                piece.PieceImageClickUP();
//            }
//        }
//    }
//}




//----------------------------------------------------------------------------


//private void pictureBox1_Paint(object sender, PaintEventArgs e)
//{
//    e.Graphics.DrawImage(picture1, drawPoint);
//}

//private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
//{
//    if (locationMouseDown != Point.Empty)
//    {
//        Point newlocation = this.pictureBox1.Location;
//        newlocation.X += e.X - locationMouseDown.X;
//        newlocation.Y += e.Y - locationMouseDown.Y;
//        this.pictureBox1.Location = newlocation;
//    }
//}

//private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
//{
//    locationMouseDown = Point.Empty;

//}

//private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
//{
//    if (e.Button == MouseButtons.Left)
//    {
//        locationMouseDown = new Point(e.X, e.Y);
//        this.pictureBox1.BringToFront();
//        this.pictureBox1.Refresh();
//    }
//}

//------------------------------------------------------------------------------


//public class CustomPictureBox : PictureBox
//{
//    protected override CreateParams CreateParams
//    {
//        get
//        {
//            CreateParams cp = base.CreateParams;
//            cp.ExStyle |= 0x20;
//            return cp;
//        }
//    }

//    protected override void OnPaintBackground(PaintEventArgs e)
//    {
//        // do nothing
//    }

//    protected override void OnMove(EventArgs e)
//    {
//        RecreateHandle();
//    }


//}

