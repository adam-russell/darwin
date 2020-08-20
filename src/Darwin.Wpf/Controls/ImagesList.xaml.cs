// This file is part of DARWIN.
// Copyright (C) 1994 - 2020
//
// DARWIN is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// DARWIN is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DARWIN.  If not, see<https://www.gnu.org/licenses/>.

using Darwin.Database;
using Darwin.Model;
using Darwin.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
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
    /// Interaction logic for ImagesList.xaml
    /// </summary>
    public partial class ImagesList : UserControl, INotifyPropertyChanged
    {
        private const int CalculatedSingleHeightPadding = 15;
        private const double HeightProportion = 0.75;
        public static DependencyProperty DatabaseIndividualProperty = DependencyProperty.Register("DatabaseIndividual", typeof(DatabaseFin), typeof(ImagesList),
                        new FrameworkPropertyMetadata(null) { BindsTwoWayByDefault = true });

        public DatabaseFin DatabaseIndividual
        {
            get { return (DatabaseFin)GetValue(DatabaseIndividualProperty); }
            set { SetValue(DatabaseIndividualProperty, value); }
        }

        public static DependencyProperty ImagesProperty = DependencyProperty.Register("Images", typeof(ObservableCollection<DatabaseImage>), typeof(ImagesList),
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

        private int _numImagesPerRow;
        public int NumImagesPerRow
        {
            get => _numImagesPerRow;
            set
            {
                _numImagesPerRow = value;
                RaisePropertyChanged("NumImagesPerRow");
                RaisePropertyChanged("ImageBoxWidth");
                RaisePropertyChanged("ImageBoxHeight");
            }
        }

        private int _imageBoxMargin;
        public int ImageBoxMargin
        {
            get => _imageBoxMargin;
            set
            {
                _imageBoxMargin = value;
                RaisePropertyChanged("ImageBoxMargin");
                RaisePropertyChanged("ImageBoxWidth");
                RaisePropertyChanged("ImageBoxHeight");
            }
        }

        public double ImageBoxWidth
        {
            get
            {
                if (NumImagesPerRow <= 0)
                    return ImagesListBox.ActualWidth;

                if (NumImagesPerRow == 1 || Images.Count == 1)
                    return ImagesListBox.ActualWidth - 2 * ImageBoxMargin;

                int numImages = NumImagesPerRow;

                if (Images.Count < NumImagesPerRow)
                    numImages = Images.Count;

                return ((double)ImagesListBox.ActualWidth - 2 * ImageBoxMargin * numImages) / numImages;
            }
        }

        public double ImageBoxHeight
        {
            get
            {
                if (NumImagesPerRow <= 0)
                    return ImagesScrollViewer.ViewportHeight - CalculatedSingleHeightPadding;

                if (Images.Count == 1)
                    return ImagesScrollViewer.ViewportHeight - 2 * ImageBoxMargin - CalculatedSingleHeightPadding;

                return ImageBoxWidth * HeightProportion;
            }
        }

        static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ImagesList).OnChanged();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            //Trace.WriteLine("Width: " + sizeInfo.NewSize.Width);

            if (sizeInfo.NewSize.Width < 500)
                NumImagesPerRow = 1;
            else if (sizeInfo.NewSize.Width < 1050)
                NumImagesPerRow = 2;
            else if (sizeInfo.NewSize.Width < 1550)
                NumImagesPerRow = 3;
            else if (sizeInfo.NewSize.Width < 2050)
                NumImagesPerRow = 4;
            else
                NumImagesPerRow = 5;

            RaisePropertyChanged("ImageBoxWidth");
            RaisePropertyChanged("ImageBoxHeight");
            base.OnRenderSizeChanged(sizeInfo);
        }

        void OnChanged()
        {
            if (Images != null)
                Images.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Images_CollectionChanged);
        }

        void Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("ImageBoxWidth");
            RaisePropertyChanged("ImageBoxHeight");
        }

        public ImagesList()
        {
            ImageBoxMargin = 5;
            NumImagesPerRow = 3;
            InitializeComponent();
        }

        private void OutlineButton_Click(object sender, RoutedEventArgs e)
        {
            var outlineWindowVM = new OutlineWindowViewModel(MainWindow.CurrentDatabase, DatabaseIndividual);

            var outlineWindow = new OutlineWindow(outlineWindowVM);
            outlineWindow.Show();
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
