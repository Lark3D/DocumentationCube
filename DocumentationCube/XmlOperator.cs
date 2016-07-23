using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DocumentationCube
{
    internal static class XmlOperator
    {
        internal static Documentation LoadFromXml(string fileName)
        {
            XDocument doc = new XDocument();

            try
            {
                doc = XDocument.Load(fileName);
            }

            catch (Exception ex)
            {
                string msg = string.Format("Не удалось найти файл документации {0}", fileName);
                //Log.Error(msg, ex);
            }

            var newDocumentation = new Documentation();

            try
            {
                XElement xDocumentation = doc.Element("Documentation");
                newDocumentation.Children = LoadChildren(xDocumentation);
            }

            catch (Exception ex)
            {
                string msg = String.Format("Содержимое файла документации повреждено");
                //Log.Error(msg, ex);
            }

            return newDocumentation;
        }

        private static List<DocumentationEntity> LoadChildren(XElement xElement)
        {
            var newList = new List<DocumentationEntity>();
            foreach (XElement xEntity in xElement.Elements())
            {
                DocumentationEntity newEntity;
                if (xEntity.Name == "Document")
                {
                    newEntity = new Document();
                    Document newDocument = newEntity as Document;
                    newDocument.FileName = (string)xEntity.Element("FileName");
                }
                else if (xEntity.Name == "SubSection")
                {
                    newEntity = new SubSection();
                    SubSection newSubSection = newEntity as SubSection;
                    newSubSection.Children = LoadChildren(xEntity);
                }
                else
                {
                    newEntity = new DocumentationEntity();
                }
                newEntity.Name = (string)xEntity.Element("Name");
                newEntity.Description = (string)xEntity.Element("Description");
                newList.Add(newEntity);
            }
            return newList;
        }
    }
}
