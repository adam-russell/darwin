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

        public static readonly DependencyProperty ShowCarouselButtonsProperty =
            DependencyProperty.Register("ShowCarouselButtons",
                typeof(bool),
                typeof(ImageAndCrop),
                new FrameworkPropertyMetadata(true,
                        FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowCarouselButtons
        {
            set { SetValue(ShowCarouselButtonsProperty, value); }
            get { return (bool)GetValue(ShowCarouselButtonsProperty); }
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

        private bool _carouselButtonsEnabled;
        public bool CarouselButtonsEnabled
        {
            get => _carouselButtonsEnabled;
            set
            {
                _carouselButtonsEnabled = value;
                RaisePropertyChanged("CarouselButtonsEnabled");
            }
        }

        public ImageAndCrop()
        {
            // TODO
            ImageBoxHeight = 300;

            InitializeComponent();

            CarouselButtonsEnabled = false;
        }

        static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ImageAndCrop).OnChanged();
        }

        protected void OnChanged()
        {
            if (Images != null)
                Images.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Images_CollectionChanged);

            SelectDatabaseImage(Images?.FirstOrDefault());

            if (Images != null && Images.Count > 1)
                CarouselButtonsEnabled = true;
            else
                CarouselButtonsEnabled = false;
        }

        protected void Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SelectedImage = Images?.FirstOrDefault();

            RaisePropertyChanged("ImageBoxHeight");
        }

        private void ViewImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseIndividual != null && SelectedImage != null)
            {
                var finCopy = new DatabaseFin(DatabaseIndividual);
                var imageCopy = new DatabaseImage(SelectedImage);
                DatabaseImage.FullyLoadDatabaseImage(imageCopy);
                finCopy.SetPrimaryImage(imageCopy);

                var vm = new TraceWindowViewModel(finCopy, MainWindow.CurrentDatabase, "Viewing " + finCopy.IDCode, MainWindow.CurrentInstance);
                TraceWindow traceWindow = new TraceWindow(vm);
                traceWindow.Show();
            }
        }

        private void ViewOriginalImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Little hacky
            if (DatabaseIndividual != null && SelectedImage != null)
            {
                var finCopy = new DatabaseFin(DatabaseIndividual);
                var imageCopy = new DatabaseImage(SelectedImage);
                DatabaseImage.FullyLoadDatabaseImage(imageCopy);
                finCopy.SetPrimaryImage(imageCopy);

                finCopy.PrimaryImage.FinOutline.ChainPoints = null;
                finCopy.PrimaryImage.FinImage = finCopy.PrimaryImage.OriginalFinImage;
                var vm = new TraceWindowViewModel(finCopy, MainWindow.CurrentDatabase, "Viewing " + finCopy.IDCode + " Original Image", MainWindow.CurrentInstance, true);

                TraceWindow traceWindow = new TraceWindow(vm);
                traceWindow.Show();
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

        private void PreviousImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Images == null || SelectedImage == null)
                return;

            var idx = Images.IndexOf(SelectedImage);

            if (idx <= 0)
            {
                SelectDatabaseImage(Images[Images.Count - 1]);
            }
            else
            {
                SelectDatabaseImage(Images[idx - 1]);
            }
        }

        private void NextImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Images == null || SelectedImage == null)
                return;

            var idx = Images.IndexOf(SelectedImage);

            if (idx >= Images.Count - 1)
            {
                SelectDatabaseImage(Images[0]);
            }
            else
            {
                SelectDatabaseImage(Images[idx + 1]);
            }
        }

        private void SelectDatabaseImage(DatabaseImage image)
        {
            if (image == null)
                return;

            if (image.Contour == null || image.ClippedContour == null)
                image.LoadContour();

            SelectedImage = image;
        }
    }
}
