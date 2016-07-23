using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentationCube
{
    public static class Cube
    {
        private static string _filename;
        public static string FileName
        {
            get { return _filename; }
            set
            {
                _filename = value;
            }
        }

    }
}
