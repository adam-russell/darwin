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
        public static DependencyProperty ImagesProperty =
    DependencyProperty.Register("Images", typeof(ObservableCollection<DatabaseImage>), typeof(ImagesList),
                                new FrameworkPropertyMetadata(null, OnChanged) { BindsTwoWayByDefault = true });

        public ObservableCollection<DatabaseImage> Images
        {
            get { return (ObservableCollection<DatabaseImage>)GetValue(ImagesProperty); }
            set { SetValue(ImagesProperty, value); }
        }

        public static readonly DependencyProperty BlahProperty = DependencyProperty.Register("Blah",
            typeof(string), typeof(ImagesList), new FrameworkPropertyMetadata(null) { BindsTwoWayByDefault = true });

        public string Blah
        {
            get { return (string)GetValue(BlahProperty); }
            set { SetValue(BlahProperty, value); RaisePropertyChanged("Blah"); }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
