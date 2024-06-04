using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markuse_mälupulk_2_0
{
    public class Folder
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required Bitmap? Image { get; set; }
    }
}
