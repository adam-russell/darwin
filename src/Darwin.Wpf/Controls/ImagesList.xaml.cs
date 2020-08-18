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
    public partial class ImagesList : UserControl
    {
        public static DependencyProperty DatabaseInvidualProperty = DependencyProperty.Register("DatabaseInvidual", typeof(DatabaseFin), typeof(ImagesList),
                        new FrameworkPropertyMetadata(null) { BindsTwoWayByDefault = true });

        public DatabaseFin DatabaseInvidual
        {
            get { return (DatabaseFin)GetValue(DatabaseInvidualProperty); }
            set { SetValue(DatabaseInvidualProperty, value); }
        }

        public static DependencyProperty ImagesProperty = DependencyProperty.Register("Images", typeof(ObservableCollection<DatabaseImage>), typeof(ImagesList),
                                new FrameworkPropertyMetadata(null, OnChanged) { BindsTwoWayByDefault = true });

        public ObservableCollection<DatabaseImage> Images
        {
            get { return (ObservableCollection<DatabaseImage>)GetValue(ImagesProperty); }
            set { SetValue(ImagesProperty, value); }
        }

        static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ImagesList).OnChanged();
        }

        void OnChanged()
        {
            if (Images != null)
                Images.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Images_CollectionChanged);
        }

        void Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Trace.WriteLine("Changed");
        }

        public ImagesList()
        {
            InitializeComponent();
        }

        private void OutlineButton_Click(object sender, RoutedEventArgs e)
        {
            var outlineWindowVM = new OutlineWindowViewModel(MainWindow.CurrentDatabase, DatabaseInvidual);

            var outlineWindow = new OutlineWindow(outlineWindowVM);
            outlineWindow.Show();
        }

        private void ViewImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseInvidual != null)
            {
                //var fin = _vm.FullyLoadFin();
                var vm = new TraceWindowViewModel(DatabaseInvidual, MainWindow.CurrentDatabase, "Viewing " + DatabaseInvidual.IDCode, MainWindow.CurrentInstance);
                TraceWindow traceWindow = new TraceWindow(vm);
                traceWindow.Show();
            }
        }

        private void ViewOriginalImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Little hacky
            if (DatabaseInvidual != null)
            {
                var fin = new DatabaseFin(DatabaseInvidual);
                fin.PrimaryImage.FinOutline.ChainPoints = null;
                fin.PrimaryImage.FinImage = fin.PrimaryImage.OriginalFinImage;
                var vm = new TraceWindowViewModel(fin, MainWindow.CurrentDatabase, "Viewing " + fin.IDCode + " Original Image", MainWindow.CurrentInstance);
                TraceWindow traceWindow = new TraceWindow(vm);
                traceWindow.Show();
            }
        }
    }
}
