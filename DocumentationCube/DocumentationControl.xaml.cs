using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class DocumentationControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private Documentation documentation;
        private string _directory;
        private string currentFile;
        private Stack<string> previousFiles = new Stack<string>();
        private Stack<string> nextFiles = new Stack<string>();
        #endregion

        public DocumentationControl()
        {
            InitializeComponent();
            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(OnNavigationRequest));
            this.PropertyChanged += OnDirectoryChanged;
        }

        #region Properties
        public List<DocumentationEntity> Entities
        {
            get
            {
                if ((documentation != null) && (documentation.Children != null))
                {
                    return documentation.Children;
                }
                else return null;
            }
        }

        public string Directory
        {
            get { return _directory; }
            set
            {
                if (File.Exists(value))
                {
                    _directory = value;
                    RaisePropertyChanged("FileName");
                }
            }
        }

     
        #endregion

        #region Methods
        public void LoadDocumentation(string directory)
        {
            _directory = directory;
            documentation = Constructor.Load(_directory);
            contentsTreeView.ItemsSource = Entities;
        }

        private void ShowDocument(string fileName)
        {
            if (File.Exists(fileName))
            {
                using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    var content = new TextRange(mainDocument.ContentStart, mainDocument.ContentEnd);

                    if (content.CanLoad(DataFormats.Rtf))
                    {
                        content.Load(fs, DataFormats.Rtf);
                    }

                    documentViewer.Document = mainDocument;
                }

                if (!string.IsNullOrEmpty(currentFile))
                {
                    previousFiles.Push(currentFile);
                }
                currentFile = fileName;
            }
        }
        #endregion

        #region Event handlers
        private void ContentsSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is Document)
                {
                    Document doc = e.NewValue as Document;
                    ShowDocument(doc.FileName);
                }
            }
            catch (Exception) { }
        }

        private void OnNavigationRequest(object sender, RoutedEventArgs e)
        {
            Hyperlink link = e.OriginalSource as Hyperlink; 
            string uri = link.NavigateUri.ToString();

            if (uri.Contains("http"))
            {
                Process.Start(uri);
            }
            else
            {
                ShowDocument(uri);
            }
        }

        private void OnDirectoryChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Directory")
            {
                LoadDocumentation(_directory);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (previousFiles.Any())
            {
                nextFiles.Push(currentFile);
                ShowDocument(previousFiles.Pop());
                previousFiles.Pop();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (nextFiles.Any())
            {
                ShowDocument(nextFiles.Pop());
            }
        }
        #endregion

        #region INotifyPropertyChanded
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        
    }
}
