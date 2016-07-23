using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DocumentationCube
{
    /// <summary>
    /// Логика взаимодействия для DocumentationControl.xaml
    /// </summary>
    public partial class DocumentationControl : UserControl
    {
        public DocumentationControl()
        {
            InitializeComponent();
            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(OnNavigationRequest));
            MainTreeView.ItemsSource = Entities;
        }

        public void LoadDocument(string filename)
        {
            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                var content = new TextRange(mainDocument.ContentStart, mainDocument.ContentEnd);

                if (content.CanLoad(DataFormats.Rtf))
                {
                    content.Load(fs, DataFormats.Rtf);
                }

                docViewer.Document = mainDocument;
            }
        }

        public List<DocumentationEntity> Entities
        {
            get
            {
                return XmlOperator.LoadFromXml(@"Docs.xml").Children;
            }
        }


        public string FileName { get; set; }

        public DocumentationEntity SelectedEntity { get; set; }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                Document doc = e.NewValue as Document;
                LoadDocument(doc.FileName);
            }
            catch (Exception) { }
        }

        public void OnNavigationRequest(object sender, RoutedEventArgs e)
        {
            //var paginator = ((IDocumentPaginatorSource)mainDocument).DocumentPaginator as DynamicDocumentPaginator;
            //var position = paginator.GetPagePosition(paginator..GetPage(reader.PageNumber - 1)) as TextPointer;
            //bookmark = position.Paragraph;
            //bookmark.BringIntoView();

            Hyperlink link = e.OriginalSource as Hyperlink; 
            string uri = link.NavigateUri.ToString();

            if (uri.Contains("http"))
            {
                Process.Start(uri);
            }
            else
            {
                LoadDocument(uri);
            }
            
        }
    }
}
