using Darwin.Model;
using Darwin.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Darwin.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for ImageAndCropControl.xaml
    /// </summary>
    public partial class ImageAndCrop : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty DatabaseIndividualProperty = DependencyProperty.Register("DatabaseIndividual", typeof(DatabaseFin), typeof(ImageAndCrop),
                new FrameworkPropertyMetadata(null) { BindsTwoWayByDefault = true });

        public DatabaseFin DatabaseIndividual
        {
            get { return (DatabaseFin)GetValue(DatabaseIndividualProperty); }
            set { SetValue(DatabaseIndividualProperty, value); }
        }

        public static DependencyProperty ImagesProperty = DependencyProperty.Register("Images", typeof(ObservableCollection<DatabaseImage>), typeof(ImageAndCrop),
                                new FrameworkPropertyMetadata(null, OnChanged) { BindsTwoWayByDefault = true });

        public ObservableCollection<DatabaseImage> Images
        {
            get { return (ObservableCollection<DatabaseImage>)GetValue(ImagesProperty); }
            set
            {
                SetValue(ImagesProperty, value);
                RaisePropertyChanged("ImageBoxWidth");
                RaisePropertyChanged("ImageBoxHeight");
            }
        }

        private DatabaseImage _selectedImage;
        public DatabaseImage SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                RaisePropertyChanged("SelectedImage");
                RaisePropertyChanged("ImageFilenameUri");
                RaisePropertyChanged("OriginalImageFilenameUri");
            }
        }

        public string ImageFilenameUri
        {
            get
            {
                if (SelectedImage == null || string.IsNullOrEmpty(SelectedImage.ImageFilenameUri))
                    return AppSettings.MissingImageUri;

                return SelectedImage.ImageFilenameUri;
            }
        }

        public string OriginalImageFilenameUri
        {
            get
            {
                if (SelectedImage == null || string.IsNullOrEmpty(SelectedImage.OriginalImageFilenameUri))
                    return AppSettings.MissingImageUri;

                return SelectedImage.OriginalImageFilenameUri;
            }
        }

        private double _imageBoxHeight;
        public double ImageBoxHeight
        {
            get => _imageBoxHeight;
            set
            {
                _imageBoxHeight = value;
                RaisePropertyChanged("ImageBoxHeight");
            }
        }

        public ImageAndCrop()
        {
            // TODO
            ImageBoxHeight = 300;

            InitializeComponent();
        }

        static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ImageAndCrop).OnChanged();
        }

        protected void OnChanged()
        {
            if (Images != null)
                Images.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Images_CollectionChanged);

            SelectedImage = Images?.FirstOrDefault();
        }

        protected void Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SelectedImage = Images?.FirstOrDefault();

            RaisePropertyChanged("ImageBoxHeight");
        }

        private void ViewImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseIndividual != null)
            {
                var image = ((Button)sender).DataContext as DatabaseImage;

                if (image != null)
                {
                    var finCopy = new DatabaseFin(DatabaseIndividual);
                    var imageCopy = new DatabaseImage(image);
                    DatabaseImage.FullyLoadDatabaseImage(imageCopy);
                    finCopy.SetPrimaryImage(imageCopy);

                    var vm = new TraceWindowViewModel(finCopy, MainWindow.CurrentDatabase, "Viewing " + finCopy.IDCode, MainWindow.CurrentInstance);
                    TraceWindow traceWindow = new TraceWindow(vm);
                    traceWindow.Show();
                }
            }
        }

        private void ViewOriginalImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Little hacky
            if (DatabaseIndividual != null)
            {
                var image = ((Button)sender).DataContext as DatabaseImage;

                if (image != null)
                {
                    var finCopy = new DatabaseFin(DatabaseIndividual);
                    var imageCopy = new DatabaseImage(image);
                    DatabaseImage.FullyLoadDatabaseImage(imageCopy);
                    finCopy.SetPrimaryImage(imageCopy);

                    finCopy.PrimaryImage.FinOutline.ChainPoints = null;
                    finCopy.PrimaryImage.FinImage = finCopy.PrimaryImage.OriginalFinImage;
                    var vm = new TraceWindowViewModel(finCopy, MainWindow.CurrentDatabase, "Viewing " + finCopy.IDCode + " Original Image", MainWindow.CurrentInstance, true);

                    TraceWindow traceWindow = new TraceWindow(vm);
                    traceWindow.Show();
                }
            }
        }

        private void OutlineButton_Click(object sender, RoutedEventArgs e)
        {
            var outlineWindowVM = new OutlineWindowViewModel(MainWindow.CurrentDatabase, DatabaseIndividual);

            var outlineWindow = new OutlineWindow(outlineWindowVM);
            outlineWindow.Show();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
