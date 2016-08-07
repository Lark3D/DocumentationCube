using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DocumentationCube
{
    public class Constructor
    {
        public static Documentation Load(string path)
        {
            var newDocumentation = new Documentation();
            newDocumentation.Children = LoadChildren(path);
            return newDocumentation;
        }


        private static List<DocumentationEntity> LoadChildren(string path)
        {
            var newList = new List<DocumentationEntity>();

            List<string> files = Directory.EnumerateFiles(path).ToList();
            List<string> directories = Directory.EnumerateDirectories(path).ToList();

            foreach (string directory in directories)
            {
                var newSubSection = new SubSection();
                newSubSection.Description = Path.GetFileName(directory);
                newSubSection.Children = LoadChildren(directory);
                newList.Add(newSubSection);
            }

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".rtf")
                {
                    var newDocument = new Document();
                    newDocument.FileName = file;
                    newDocument.Description = Path.GetFileNameWithoutExtension(file);
                    newList.Add(newDocument);
                }
            }

            return newList;
        }
    }
}
