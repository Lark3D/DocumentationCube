using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentationCube
{
    public class DocumentationEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Document : DocumentationEntity
    {
        public string FileName { get; set; }
    }

    public class MarkupEntity : DocumentationEntity
    {
        public List<DocumentationEntity> Children { get; set; }
    }

     public class Documentation : MarkupEntity
    { }

    public class SubSection : MarkupEntity
    { }


}
