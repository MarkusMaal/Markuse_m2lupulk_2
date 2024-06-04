using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markuse_mälupulk_2_0
{
    internal class BuildDateTimeAttribute : Attribute
    {
        public DateTime Built { get; }
        public BuildDateTimeAttribute(string date)
        {
            this.Built = DateTime.Parse(date);
        }
    }
}
