using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    //GridLayer je mriezka, ktora urcuje oblast pre vyrez puzzle kuskov zo zdrojoveho obrazka v puzzle editore
    public partial class GridLayer : PictureBox
    {
        //
        //TextBox-y nastavujuce sirku, dlzku hracieho pola a rozmer kuska
        //
        private TextBox widthBox = new TextBox();
        private TextBox heightBox = new TextBox();
        private TextBox dimensionBox = new TextBox();

        // zalohujeme si predchadzajuci text v textboxoch
        string textbox1_BackupText;
        string textbox2_BackupText;
        string textbox3_BackupText;

        #region Pomocne privatne premenne

        private PuzzleGameData data;
        private Size gridDimensions;
        private Size pieceDimensions;
        private Bitmap backGroundImageForGrid;
        private PictureBox parentPictBox;
        private Panel parentPanel;

        private Point pointTopLeftCorner_Grid;
        private Point pointBottomRightCorner_Grid;
        private Size currImageSize;

        #endregion

        // pociatocne nastavenia
        public void Configure(PuzzleGameData data, Form1 form)
        {
            #region Pociatocne nastavenia - rozmiestnenia, velkosti, vztahy medzi controls, events handlers

            this.data = data;                                      //Je potrebny zdrojovy obrazok v gamedata!!
            this.parentPanel = (form.Controls["panel4"] as Panel); //Je potrebne nastavit pred zavolanim Configure(), kto je komu rodicom
            this.parentPictBox = (this.Parent as PictureBox);
            this.parentPictBox.Location = new Point(0 - data.SourcePicture.Width, 0 - data.SourcePicture.Height);
            this.parentPictBox.Size = new Size(data.SourcePicture.Width + this.parentPanel.Size.Width,
                                               data.SourcePicture.Height + this.parentPanel.Size.Height);
            this.parentPictBox.BringToFront();
            //
            this.Location = new Point(data.SourcePicture.Width, data.SourcePicture.Height);   //pozicia gridlayer kvoli posunu picturebox-u
            this.Size = new Size(this.parentPanel.Size.Width, this.parentPanel.Height);       //velkost gridlayer podla velkosti zobrazovaniecho panelu(panel4)
            this.BackColor = Color.Transparent;
            this.BringToFront();
            this.Anchor = AnchorStyles.None;
            this.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);
            this.Controls.Add(widthBox);
            this.Controls.Add(heightBox);
            this.Controls.Add(dimensionBox);
            this.MouseDown += this.customGridLayer_MouseDown;
            this.MouseUp += this.customGridLayer_MouseUp;
            this.MouseMove += this.customGridLayer_MouseMove;
            this.parentPictBox.ClientSizeChanged += customclientSizeChanged_ParentPictureBox;
            //
            this.parentPictBox.Image = PictureEditor.ImageScale(GridRatioFromSourceImageResolution, data.SourcePicture);


            this.parentPictBox.Padding = new Padding(data.SourcePicture.Width +
                                                     (int)((this.parentPanel.Width - this.parentPictBox.Image.Size.Width) / 2),
                                                     data.SourcePicture.Height +
                                                     (int)((this.parentPanel.Height - this.parentPictBox.Image.Size.Height) / 2), 0, 0);
            TextBoxesVisible = false;
            //====================================================================================================================================
            #endregion

            #region Nastavenie Grid
            //
            //Prednastavenia pred vytvorenim Grid okrazka
            //
            backGroundImageForGrid = PictureEditor.ColouredBackGroundImage(Color.Black, this.Size); //Color.RoyalBlue
            backGroundImageForGrid = PictureEditor.ImageOpacity(0.60F, backGroundImageForGrid);
            pieceDimensions = new Size(100, 100);
            gridDimensions = new Size((int)((data.SourcePicture.Width * 0.7) / pieceDimensions.Width),
                                      (int)((data.SourcePicture.Height * 0.7) / pieceDimensions.Height));
            //
            //Nastavenie vygenerovaneho grid obrazka
            //
            this.SetGridImage(GridRatioFromSourceImageResolution);
            ////this.Refresh();


            //
            //Nastavenie textboxov
            //
            widthBox.TextChanged += this.whenTextBox1_Changed;
            widthBox.MouseEnter += this.whenTextBox1_MouseEnter;
            widthBox.MouseLeave += this.whenTextBox1_MouseLeave;

            heightBox.TextChanged += this.whenTextBox2_Changed;
            heightBox.MouseEnter += this.whenTextBox2_MouseEnter;
            heightBox.MouseLeave += this.whenTextBox2_MouseLeave;

            dimensionBox.TextChanged += this.whenTextBox3_Changed;
            dimensionBox.MouseEnter += this.whenTextBox3_MouseEnter;
            dimensionBox.MouseLeave += this.whenTextBox3_MouseLeave;

            //widthBox.Text = "Width";
            //heightBox.Text = "Height";
            //dimensionBox.Text = "Size";

            var textBoxes = this.Controls.OfType<TextBox>().ToArray();
            foreach (var textBox in textBoxes)
            {
                textBox.BackColor = Color.FromArgb(64, 64, 64);
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.ForeColor = Color.White;
                textBox.Size = new Size(50, 20);
                //textBox.Refresh();
            }
            //
            //do textboxov vyplnime pociatocne hodnoty rozmerov grid, a velkost kuska puzzle, zalohujeme text, ktory v nich bol
            //
            textbox1_BackupText = (gridDimensions.Width).ToString();
            widthBox.Text = "Šírka";
            textbox2_BackupText = (gridDimensions.Height).ToString();
            heightBox.Text = "Dĺžka";
            textbox3_BackupText = (pieceDimensions.Width).ToString();
            dimensionBox.Text = "Rozmer";
            //
            //TextBox-y umiestnime do stredu
            //
            TextBoxesInTheMiddle(MiddlePoint_Grid);
            //
            //TextBox-y budu viditelne
            //
            TextBoxesVisible = true;
            this.Refresh();
            //
            // Vzhlad kurzora
            //
            //Pri vybere obrazka sa vzhlad kurzoru zmeni na posovny kurzor, sipky styrmi smermi
            this.Cursor = Cursors.SizeAll;

            //
            // Zistime ako su od seba posunuta mriezka a obrazok a prevedieme to do skutocneho rozdielu, ak by bol obrazok nescalovany
            // Pri zmene velkosti okna totiz nesmie dojst k posunu mriezky vzhladom k obrazku, mriezka by mohla vyjst mimo obrazok
            //
            originalCornersDiff = OriginalCornersDiff;

            #endregion
        }

        // V TextBoxoch
        #region Handlers pre textbox-y + funkcia, ktora zabezpeci aby za mriezka neocitla mimo obrazok

        // ak pri zmene velkosti mriezky dojde k tomu, ze sa obrazok ocitne mimo mriezky, obrazok posunemie
        // potrebujeme to osetrit, aby sme nevystrihovali mimo hranic obrazka
        private void correctGridOutOfBounds(string nameOfTheBox)
        {
            Padding tmpPadding;

            switch (nameOfTheBox)
            {
                #region v pripade, ze sme obrazok prekrocili sirkovo
                case "widthBox":
                    {
                        tmpPadding = this.parentPictBox.Padding;
                        if (TopLeftCornerPoint_GridLayer.X < TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.X)
                        {
                            tmpPadding.Left = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.X;
                        }

                        if (BottomRightCornerPoint_GridLayer.X > BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.X)
                        {
                            tmpPadding.Left += (BottomRightCornerPoint_GridLayer.X -
                                                BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.X);
                        }
                        this.parentPictBox.Padding = tmpPadding;
                        break;
                    }
                #endregion

                #region v pripade, ze sme obrazok prekrocili vyskovo
                case "heightBox":
                    {
                        tmpPadding = this.parentPictBox.Padding;
                        if (TopLeftCornerPoint_GridLayer.Y < TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y)
                        {
                            tmpPadding.Top = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.Y;
                        }

                        if (BottomRightCornerPoint_GridLayer.Y > BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y)
                        {
                            tmpPadding.Top += (BottomRightCornerPoint_GridLayer.Y -
                                               BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y);

                        }
                        this.parentPictBox.Padding = tmpPadding;
                        break;
                    }
                #endregion

                #region v pripade, ze sme obrazok prekrocili rozmermy puzzle kuskov
                case "dimensionsBox":
                    {
                        tmpPadding = this.parentPictBox.Padding;
                        if (TopLeftCornerPoint_GridLayer.X < TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.X)
                        {
                            tmpPadding.Left = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.X;
                        }

                        if (BottomRightCornerPoint_GridLayer.X > BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.X)
                        {
                            tmpPadding.Left += (BottomRightCornerPoint_GridLayer.X -
                                                BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.X);
                        }

                        if (TopLeftCornerPoint_GridLayer.Y < TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y)
                        {
                            tmpPadding.Top = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.Y;
                        }

                        if (BottomRightCornerPoint_GridLayer.Y > BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y)
                        {
                            tmpPadding.Top += (BottomRightCornerPoint_GridLayer.Y -
                                               BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y);
                        }

                        this.parentPictBox.Padding = tmpPadding;

                        break;
                    }
                #endregion

                default:
                    break;
            }
        }

        private void whenTextBox1_Changed(object sender, EventArgs e) //ak doslo k zmene gridWidth
        {
            TextBox box = sender as TextBox;
            int value;

            if ((data.SourcePicture != null) && (int.TryParse(box.Text, out value)))
            {
                int maxWidth = data.SourcePicture.Width;
                value = Math.Abs(value);

                if ((value * pieceDimensions.Width) > maxWidth)
                {
                    box.ForeColor = Color.Red;
                }
                else
                {
                    box.ForeColor = Color.White;
                    gridDimensions.Width = value;
                    this.SetGridImage(GridRatioFromSourceImageResolution);
                    if (this.parentPictBox.Visible)
                    {
                        correctGridOutOfBounds("widthBox");
                    }

                }
            }
            //
        }

        private void whenTextBox2_Changed(object sender, EventArgs e) //ak doslo k zmene gridHeight
        {
            TextBox box = sender as TextBox;
            int value;

            if ((data.SourcePicture != null) && (int.TryParse(box.Text, out value)))
            {
                int maxHeight = data.SourcePicture.Height;
                value = Math.Abs(value);

                if ((value * pieceDimensions.Width) > maxHeight)
                {
                    box.ForeColor = Color.Red;
                }
                else
                {
                    box.ForeColor = Color.White;
                    gridDimensions.Height = value;
                    this.SetGridImage(GridRatioFromSourceImageResolution);
                    if (this.parentPictBox.Visible)
                    {
                        correctGridOutOfBounds("heightBox");
                    }
                }
            }
        }

        private void whenTextBox3_Changed(object sender, EventArgs e) //ak doslo k zmene rozmeru puzzle kuska
        {
            TextBox box = sender as TextBox;
            int value;

            if ((data.SourcePicture != null) && (int.TryParse(box.Text, out value)))
            {
                int maxWidth = data.SourcePicture.Width;
                int maxHeight = data.SourcePicture.Height;
                value = Math.Abs(value);

                if (((value * gridDimensions.Width) > maxWidth) || ((value * gridDimensions.Height) > maxHeight))
                {
                    box.ForeColor = Color.Red;
                }
                else
                {
                    box.ForeColor = Color.White;
                    pieceDimensions.Width = value;
                    pieceDimensions.Height = value;
                    this.SetGridImage(GridRatioFromSourceImageResolution);
                    if (this.parentPictBox.Visible)
                    {
                        correctGridOutOfBounds("dimensionsBox");
                    }
                }
            }
        }

        // ak mys neukazuje na textbox napise v nom o aky textbox ide
        private void whenTextBox1_MouseLeave(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            textbox1_BackupText = box.Text;
            box.Text = "Šírka";
        }

        private void whenTextBox2_MouseLeave(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            textbox2_BackupText = box.Text;
            box.Text = "Dĺžka";
        }

        private void whenTextBox3_MouseLeave(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            textbox3_BackupText = box.Text;
            box.Text = "Rozmer";
        }

        // ak sa mys nachadza ponad textboxy, zobrazia sa rozmery
        private void whenTextBox1_MouseEnter(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            box.Text = textbox1_BackupText;
        }

        private void whenTextBox2_MouseEnter(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            box.Text = textbox2_BackupText;
        }

        private void whenTextBox3_MouseEnter(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            box.Text = textbox3_BackupText;
        }



        #endregion



        // sluzi na vykreslenie alebo prekreslenie mriezky pocat behu programu, pri zmene velkosti okna alebo rozmerov mriezky pod hodnot v textbox-och
        private void SetGridImage(double ratioR)  //nastavuje aj suradnice horneho laveho a dolneho praveho rohu gridu vzhladom k parent picturebox-u
        {
            this.Image = PictureEditor.DrawGridIntoImage(ratioR, pieceDimensions,
                                                                  gridDimensions.Width,
                                                                  gridDimensions.Height,
                                                                  backGroundImageForGrid,
                                                                  this.Size);

            //Urcime, kde konkretne sa nachadzaju rohy gridu(top left, bottom right) v parent picturebox-e vzdy,
            //ked dojde k zmene velkosti alebo pozicii gridu.
            pointTopLeftCorner_Grid = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox;
            pointBottomRightCorner_Grid = BottomRightCornerPoint_GridLayer_TransformedToParentPicturebox;
            currImageSize = new Size((int)(ratioR * data.SourcePicture.Width), (int)(ratioR * data.SourcePicture.Height));
        }

        // ak sme mysou este zatial neklikli v ziadnom bode
        private Point locationMouseDown = Point.Empty;

        // stred mriezky vramci panelu, kde sa nachadza GridLayer
        private Point MiddlePoint_Grid
        {
            get
            {
                return new Point((int)(this.ClientSize.Width / 2),
                                 (int)(this.ClientSize.Height / 2));
            }
        }

        // NEVYUZITE - stred obrazka pod mriezkov, ktory sa nachadza v picturebox-e,
        private Point MiddlePoint_ImageOfParentPictureBox
        {
            get
            {
                return new Point(this.parentPictBox.Padding.Left + (int)(this.parentPictBox.Image.Size.Width / 2),
                                 this.parentPictBox.Padding.Top + (int)(this.parentPictBox.Image.Size.Height / 2));
            }
            set
            {
                this.parentPictBox.Padding = new Padding((value.X - (int)(this.parentPictBox.Image.Size.Width / 2)),
                                                         (value.Y - (int)(this.parentPictBox.Image.Size.Height / 2)), 0, 0);
            }
        }

        // metoda nastavi textbox-y do stredu mriezky
        private void TextBoxesInTheMiddle(Point middleP)
        {
            widthBox.Location = new Point(middleP.X - 26, middleP.Y - 40);
            heightBox.Location = new Point(middleP.X - 26, middleP.Y - 10);
            dimensionBox.Location = new Point(middleP.X - 26, middleP.Y + 20);
        }

        // zobrazenie / skrutie TextBoxov
        public bool TextBoxesVisible
        {
            set
            {
                if (value)
                {
                    widthBox.Visible = true;
                    heightBox.Visible = true;
                    dimensionBox.Visible = true;
                }
                else
                {
                    widthBox.Visible = false;
                    heightBox.Visible = false;
                    dimensionBox.Visible = false;
                }
            }
        }

        private void customGridLayer_MouseDown(object sender, MouseEventArgs e)
        {
            GridLayer puzzlePiece = sender as GridLayer;

            if (e.Button == MouseButtons.Left)
            {
                locationMouseDown = new Point(e.X, e.Y);
            }

        }

        private void customGridLayer_MouseUp(object sender, MouseEventArgs e)
        {
            locationMouseDown = Point.Empty;
        }


        // mriezka sa nehybe, posuvame iba obrazok pod nou, obrazok sa nedostane mimo okrajov mriezky
        // posun obrazka pomocou zmeny paddingu, aj do zapornych suradnic
        // PictureBox zobrazujuci obrazok ma pociatocne suradnice v zapornom kvadrante, aby sa obrazok mohol pohyboval lubovolne pod mriezkou,
        // padding nepodporuje zaporne suradnice 
        private void customGridLayer_MouseMove(object sender, MouseEventArgs e)
        {
            // ak sa kurzor mysi bude nachadzat nad TextBoxami zmeni sa na klasicky cursor mysi
            this.Cursor = Cursors.SizeAll;

            if (locationMouseDown != Point.Empty)
            {
                //posun obrazka vramci picturebox-u zabezpecime pomocu padding-u
                //padding nepodporuje zaporne suradnice, preto je picturebox posunuty do zapornych suradnic vramci Panel control
                //picturebox parent == panel

                Padding padd = this.parentPictBox.Padding;
                padd.Left += e.X - locationMouseDown.X;
                padd.Top += e.Y - locationMouseDown.Y;

                #region Aby obrazok nevysiel mimo vystrihovaci GRID
                if (padd.Left > pointTopLeftCorner_Grid.X)
                {
                    padd.Left = pointTopLeftCorner_Grid.X;
                }
                if (padd.Left < (pointBottomRightCorner_Grid.X - currImageSize.Width))
                {
                    padd.Left = (pointBottomRightCorner_Grid.X - currImageSize.Width);
                }

                if (padd.Top > pointTopLeftCorner_Grid.Y)
                {
                    padd.Top = pointTopLeftCorner_Grid.Y;
                }
                if (padd.Top < (pointBottomRightCorner_Grid.Y - currImageSize.Height))
                {
                    padd.Top = (pointBottomRightCorner_Grid.Y - currImageSize.Height);
                }
                #endregion

                padd.Bottom = 0;
                padd.Right = 0;
                this.parentPictBox.Padding = padd;

                // zaznamename si odchylku medzi mriezkou a obrazkom, pouzite pri zmene velkosti okna
                originalCornersDiff = OriginalCornersDiff;

                locationMouseDown = new Point(e.X, e.Y);

                this.Refresh();
            }
        }

        // rozdiel medzi lavymi hornymi rohmi mriezky a obrazka v originalnej velkosti, pouzite zmene velkosti okna, aby rozdiel vzdialenost ostal rovnaky,
        // teda aby aj po zvacseni ostala mriezka presne nam obrazkom ako bola
        private Point OriginalCornersDiff
        {
            get
            {
                Point diff = new Point(TopLeftCornerPoint_GridLayer.X - ImageUnderGridLayer_Location.X,
                                       TopLeftCornerPoint_GridLayer.Y - ImageUnderGridLayer_Location.Y);

                double ratioToGetOriginalScale = 1.0 / GridRatioFromSourceImageResolution;

                return new Point((int)((double)diff.X * (double)ratioToGetOriginalScale),
                                 (int)((double)diff.Y * (double)ratioToGetOriginalScale));
            }
        }

        // nastavuje poziciu obrazka pod mriezkou, ktory je sucastou pciteboxu
        private Point ImageUnderGridLayer_Location
        {
            get
            {
                //Debug.WriteLine("Padding {0}, {1}", this.parentPictBox.Padding.Left, this.parentPictBox.Padding.Top);
                return new Point(this.parentPictBox.Padding.Left - this.data.SourcePicture.Width,
                                 this.parentPictBox.Padding.Top - this.data.SourcePicture.Height);
                
            }

            set
            {
                Padding padd = this.parentPictBox.Padding;
                padd.Left = value.X;
                padd.Top = value.Y;
                //Debug.WriteLine("ValueXY {0}, {1}", value.X, value.Y);
                this.parentPictBox.Padding = padd;
            }
        }

        // ak sa zmeni velkost okna, potrebujeme prekreslit mriezku, a umiestnit obrazok pod mriezkou na miesto kde bol pred zmenou okna,
        // teda pri akomkolvek natahovani okna programu, musi zostat pozicia mriezky voci obrazku rovnaka
        // mriezka zostava vzdy staticka, posuva sa iba obrazok pod nou              
        private Point originalCornersDiff;
        private void customclientSizeChanged_ParentPictureBox(object sender, EventArgs e)
        {
            // doslo k zmene velkosti panel-u a teda aj k zmene velkosti mriezky
            this.Size = this.parentPanel.Size;

            // vratime zmensenu / zvacsenu verziu povodneho obrazka, podla aktualneho GridRatioFromSourceImageResolution
            this.parentPictBox.Image = PictureEditor.ImageScale(GridRatioFromSourceImageResolution, data.SourcePicture);

            // prekreslime mriezky na nove rozmery okna
            this.SetGridImage(GridRatioFromSourceImageResolution);

            // upravime poziciu obrazka vzhladom k mriezke, aby mriezka ukazovala na rovnaku oblast obrazka aj po zmene velkosti okna
            ImageUnderGridLayer_Location = new Point(TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.X -
                                                     (int)((double)originalCornersDiff.X * (double)GridRatioFromSourceImageResolution),
                                                     TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.Y -
                                                     (int)((double)originalCornersDiff.Y * (double)GridRatioFromSourceImageResolution));

            
            this.TextBoxesInTheMiddle(MiddlePoint_Grid);
                        
        }

        // Pozicia GridLayer bitmapu v GridLayer control(mriezka je vykreslena pomocou vygenerovaneho Bitmap-u)
        public Point Position
        {
            get
            {
                return new Point(this.Padding.Left, this.Padding.Top);
            }

            set
            {
                this.Padding = new Padding(value.X, value.Y, 0, 0);
            } 
        }

        // GridRatioFromSourceImageResolution - o kolko treba zmensit(resp. zvacsit) zobrazeny zdrojovy obrazok oproti originalnemu zdrojov. obrazku,
        // aby zodpovedal velkosti okna. Velkost zdrojoveho obrazka sa nemeni, iba ho vykreslujeme v inom rozliseni.
        // Velkost Grid-u(mriezky, ktora naznacuje oblast pre vystrihnutie zo zdrojoveho obrazka) sa prepocitava tiez podla tejto hodnoty,
        // avsak musi dojst k opetovnemu prekresleniu mriezky pomocou SetGridImage();
        private double GridRatioFromSourceImageResolution
        {
            get
            {
                double ratioWidth = (double)(this.ClientSize.Width) / (double)(this.data.SourcePicture.Width);
                double ratioHeight = (double)(this.ClientSize.Height) / (double)(this.data.SourcePicture.Height);
                return Math.Min(ratioWidth, ratioHeight);
            }
        }

        private Point TopLeftCornerPoint_GridLayer
        {
            get
            {
                double ratio = GridRatioFromSourceImageResolution;
                int scaledGridWidth = (int)((pieceDimensions.Width * gridDimensions.Width) * ratio);
                int scaledGridHeight = (int)((pieceDimensions.Height * gridDimensions.Height) * ratio);
                return new Point((int)((this.ClientSize.Width - scaledGridWidth) / 2),
                                 (int)((this.ClientSize.Height - scaledGridHeight) / 2));
            }
        }

        private Point BottomRightCornerPoint_GridLayer
        {
            get
            {
                double ratio = GridRatioFromSourceImageResolution;
                int scaledGridWidth = (int)((pieceDimensions.Width * gridDimensions.Width) * ratio);
                int scaledGridHeight = (int)((pieceDimensions.Height * gridDimensions.Height) * ratio);
                return new Point(scaledGridWidth + (int)((this.ClientSize.Width - scaledGridWidth) / 2),
                                 scaledGridHeight + (int)((this.ClientSize.Height - scaledGridHeight) / 2));
            }
        }

        // vrati suradnice kam ukazuje horny lavy roh gridu v parent picturebox-e
        private Point TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox
        {
            get
            {
                return new Point(data.SourcePicture.Width + TopLeftCornerPoint_GridLayer.X,
                                 data.SourcePicture.Height + TopLeftCornerPoint_GridLayer.Y);
            }

        }

        // vrati suradnice kam ukazuje dolny pravy roh gridu v parent picturebox-e
        private Point BottomRightCornerPoint_GridLayer_TransformedToParentPicturebox
        {
            get
            {
                return new Point(data.SourcePicture.Width + BottomRightCornerPoint_GridLayer.X,
                                 data.SourcePicture.Height + BottomRightCornerPoint_GridLayer.Y);
            }

        }

        // vrati suradnice horneho laveho roha obrazka pod mriezkou vzhladom ku GridLayerControl
        private Point TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer
        {
            get
            {
                return new Point(this.parentPictBox.Padding.Left - this.data.SourcePicture.Width,
                                 this.parentPictBox.Padding.Top - this.data.SourcePicture.Height); 
            }
        }

        // vrati suradnice dolneho praveho rohu obrazka pod mriezkou vzhladom ku GridLayerControl
        private Point BottomRightCornerPoint_ImageInPictureBox_TransformedToGridLayer
        {
            get
            {
                return new Point(TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.X +
                                 (int)(this.data.SourcePicture.Width * GridRatioFromSourceImageResolution),
                                 TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y +
                                 (int)(this.data.SourcePicture.Height * GridRatioFromSourceImageResolution));
            }
        }

        // vrati pociatocnu poziciu pre vystrihnutie zo zdrojoveho obrazka
        public Point StartCutLocation
        {
            get
            {
                //
                //vrati nam suradnice vo vnutri parent picturebox-u, ktore ukazuju na miesto zaciatku rezu podla mriezky zobrazenej v gridlayer
                //
                Point startCutLocation = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox;
                //odratame padding parent pictureboxu
                startCutLocation = new Point(startCutLocation.X - this.parentPictBox.Padding.Left,
                                             startCutLocation.Y - this.parentPictBox.Padding.Top);

                // dane ratio urcuje kolko krat je potrebne vzdialenost zvacsit(resp. zmensit) aby sme dostali realne miesto rezu v zdrojovom obrazku
                double ratio = 1.0 / GridRatioFromSourceImageResolution;
                
                // suradnice prenasobime ratio-m, aby sme dostali realne suradnice pre zaciatok rezu v zdrojovom obrazku
                startCutLocation.X = (int)(startCutLocation.X * ratio);
                startCutLocation.Y = (int)(startCutLocation.Y * ratio);
                
                return startCutLocation;
            }
        }

        // vrati koncovu poziciu pre vystrihnutie zo zdrojoveho obrazka
        public Point EndCutLocation
        {
            get
            {
                return new Point(StartCutLocation.X + (pieceDimensions.Width * gridDimensions.Width),
                                 StartCutLocation.Y + (pieceDimensions.Height * gridDimensions.Height));
            }
        }

        // pocet kuskov puzzle sirkovo, dlzkovo
        public Size GridDimensions
        {
            get { return this.gridDimensions; }
        }

        // rozmery puzzle kuskov bez zubkov
        public Size PieceDimensions
        {
            get { return this.pieceDimensions; }
        }
    }
}












//-----old(niektore nepouzite metody)----------------------------------------
//Point newCornersDiff = new Point(TopLeftCornerPoint_GridLayer.X - ImageUnderGridLayer_Location.X,
//                                 TopLeftCornerPoint_GridLayer.Y - ImageUnderGridLayer_Location.Y);

//Debug.WriteLine("newDiff {0}, {1}", newCornersDiff.X, newCornersDiff.Y);

//Point diffOfMovement = new Point(newCornersDiff.X - originalCornersDiff.X,
//                       newCornersDiff.Y - originalCornersDiff.Y);

//Debug.WriteLine("diffOfMovement {0}, {1}",diffOfMovement.X, diffOfMovement.Y);

//Debug.WriteLine("imageLocBefore {0}, {1}", ImageUnderGridLayer_Location.X, ImageUnderGridLayer_Location.Y);

//if ((diffOfMovement.X != 0) && (diffOfMovement.Y != 0))
//{
//    ImageUnderGridLayer_Location = new Point(diffOfMovement.X,
//                                             diffOfMovement.Y);
//}

//if (diffOfMovement.X != 0)
//{
//    ImageUnderGridLayer_Location = new Point(diffOfMovement.X,
//                                             0);
//}

//if (diffOfMovement.Y != 0)
//{
//    ImageUnderGridLayer_Location = new Point(0,
//                                             diffOfMovement.Y);
//}

//Debug.WriteLine("imageLocAfter {0}, {1}", ImageUnderGridLayer_Location.X, ImageUnderGridLayer_Location.Y);

//originalCornersDiff = newCornersDiff;

//Debug.WriteLine("oldDiff {0}, {1}", originalCornersDiff.X, originalCornersDiff.Y);


// Obrazok pod grid-om posunieme tak, aby grid stale vyrezaval zvolenu cast bezohladu na zmenu velkosti okna.
//int newImageScaledWidth = (int)(this.data.SourcePicture.Width * GridRatioFromSourceImageResolution);
//int newImageScaledHeight = (int)(this.data.SourcePicture.Height * GridRatioFromSourceImageResolution);

//Point newCornersDiff = new Point((int)((double)(oldCornersDiff.X) * ((double)GridRatioFromSourceImageResolution / (double)(oldGridRatio))),
//                                 (int)((double)(oldCornersDiff.Y) * ((double)GridRatioFromSourceImageResolution / (double)(oldGridRatio))));

////Debug.WriteLine("newImage, newCorDiff = {0},{1}", newImageScaledWidth, newCornersDiff.X);
////Debug.WriteLine("newImage, newCorDiff = {0},{1}", newImageScaledHeight, newCornersDiff.Y);
////Debug.WriteLine("-------------------------------------");

//Point pointInsideOfImage = new Point(TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.X + newCornersDiff.X,
//                                     TopLeftCornerPoint_ImageInPictureBox_TransformedToGridLayer.Y + newCornersDiff.Y);

//Point diff = new Point(newCornersDiff.X - oldCornersDiff.X, newCornersDiff.Y - oldCornersDiff.Y);
//Padding padd = this.parentPictBox.Padding;
////if (TopLeftCornerPoint_GridLayer.X > pointInsideOfImage.X)
////{
////    padd.Left = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.X - newCornersDiff.X;
////}

////if (TopLeftCornerPoint_GridLayer.X <= pointInsideOfImage.X)
////{
////    padd.Left = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.X - newCornersDiff.X;
////}

////if (TopLeftCornerPoint_GridLayer.Y > pointInsideOfImage.Y)
////{
////    padd.Top = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.Y - newCornersDiff.Y;
////}

////if (TopLeftCornerPoint_GridLayer.Y <= pointInsideOfImage.Y)
////{
////    padd.Top = TopLeftCornerPoint_GridLayer_TransformedToParentPicturebox.Y - newCornersDiff.Y;
////}


//padd.Left += diff.X;
//padd.Top += diff.Y;
//this.parentPictBox.Padding = padd;


//label.Text = "Ratio = " + GridRatioFromSourceImageResolution.ToString("0.0000") + ", TopLeft= " +
//             TopLeftCornerPoint_ParentImage.ToString() + ", BottRight= " +
//             BottomRightCornerPoint_ParentImage.ToString();

//Point diff = new Point();
//MiddlePoint_ImageOfParentPictureBox = this.parentPictBox.PointToClient(this.PointToScreen(MiddlePoint_Grid));

//// Prevedieme posun obrazka za gridom od stredu gridu
//double ratio = GridRatioFromSourceImageResolution;
//int scaledGridWidth = (int)((pieceDimensions.Width * gridDimensions.Width) * ratio);
//int scaledGridHeight = (int)((pieceDimensions.Height * gridDimensions.Height) * ratio);
//diff.X = (int)((data.SourcePicture.Width - scaledGridWidth) / 2);
//diff.Y = (int)((data.SourcePicture.Height - scaledGridHeight) / 2);


//--------------------------------------------------


//private Point MiddlePoint
//{
//    get
//    {
//        Point middleP = new Point((int)(this.Size.Width / 2),
//                                  (int)(this.Size.Height / 2));
//        return middleP;
//    }
//}

//private Point MiddlePoint_ParentPanel
//{
//    get
//    {
//        return new Point((int)(this.parentPanel.Size.Width / 2),
//                         (int)(this.parentPanel.Size.Height / 2));
//    }
//}

//------------------------------------
//private double GridRatio
//{
//    get
//    {
//        double ratioWidth = (double)(this.ClientSize.Width) / (double)(parentPictBox.Image.Size.Width);
//        double ratioHeight = (double)(this.ClientSize.Height) / (double)(parentPictBox.Image.Size.Height);
//        return Math.Min(ratioWidth, ratioHeight);
//    }
//}

//private double GridRatioFromStartingImageResolution
//{
//    get
//    {
//        double ratioWidth = (double)(this.ClientSize.Width) / (double)(parentImageSizeAtBeginning.Width);
//        double ratioHeight = (double)(this.ClientSize.Height) / (double)(parentImageSizeAtBeginning.Height);
//        return Math.Min(ratioWidth, ratioHeight);
//    }
//}

//--------------------------
//new_GridTopLeftCorner = TopLeftCornerPoint_GridLayer;

//diff.X = old_GridTopLeftCorner.X - new_GridTopLeftCorner.X;
//diff.Y = old_GridTopLeftCorner.Y - new_GridTopLeftCorner.Y;

//Padding padd = this.parentPictBox.Padding;
//padd.Left -= diff.X;
//padd.Top -= diff.Y;
//this.parentPictBox.Padding = padd;

//old_GridTopLeftCorner = new_GridTopLeftCorner;

//if (padd.Left > pointTopLeftCorner_Grid.X)
//{
//    padd.Left = pointTopLeftCorner_Grid.X;
//}
//if (padd.Left< (pointBottomRightCorner_Grid.X - currImageSize.Width))
//{
//    padd.Left = (pointBottomRightCorner_Grid.X - currImageSize.Width);
//}

//if (padd.Top > pointTopLeftCorner_Grid.Y)
//{
//    padd.Top = pointTopLeftCorner_Grid.Y;
//}
//if (padd.Top< (pointBottomRightCorner_Grid.Y - currImageSize.Height))
//{
//    padd.Top = (pointBottomRightCorner_Grid.Y - currImageSize.Height);
//}



//--------------------------------------------------------------------------------

//this.parentPictBox.Image = PictureEditor.ImageScale(ratio, data.SourcePicture);

//this.SetImage(ratio);

//Point pos = this.Position;
//pos.X = (int)(positionBackUp.X * this.GridRatio) + TopLeftBackup.X;

//pos.Y = (int)(positionBackUp.Y * this.GridRatio) + TopLeftBackup.Y;

//TopLeftBackup = TopLeftCornerPoint_ParentImage;


//pos.X = (int)(positionBackUp.X * this.GridRatio) +
//        (2 * TopLeftCornerPoint_ParentImage.X - positionBackUp.X);
//pos.Y = (int)(positionBackUp.Y * this.GridRatio) +
//        (2 * TopLeftCornerPoint_ParentImage.Y - positionBackUp.Y);

//pos.X = (int)(Math.Abs(positionBackUp.X - TopLeftCornerPoint_ParentImage.X) * this.GridRatio) +
//        TopLeftCornerPoint_ParentImage.X;
//pos.Y = (int)(Math.Abs(positionBackUp.Y - TopLeftCornerPoint_ParentImage.Y) * this.GridRatio) +
//        TopLeftCornerPoint_ParentImage.Y;
//this.Position = pos;




//public void customGridLayer_MouseMove(object sender, MouseEventArgs e)
//{
//    PictureBox gridbox = sender as PictureBox;

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
