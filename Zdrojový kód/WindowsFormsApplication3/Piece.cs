using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
    class Piece
    {
        public Size PieceDimensions
        {
            get; set;
        }

        public Point PieceLocation
        {
            get; set;
        }

        public Bitmap PieceImage
        {
            get; set;
        }

    }
}
