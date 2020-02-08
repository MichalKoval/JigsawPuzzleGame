using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication3
{
    public partial class PieceArrangement
    {
        /// <summary>
        /// Nastavenie stran puzzle kuska (Top, Bottom, Left, Right side)
        /// 1) "in", ak je zub puzzle dovnutra
        /// 2) "out", ak je zub puzzle smerom von
        /// 3) "none", ak je dana strana puzzle bez zubu 
        /// </summary>

        public string TopSide { get; set; }
        public string BottomSide { get; set; }
        public string LeftSide { get; set; }
        public string RightSide { get; set; }
                
    }
}
