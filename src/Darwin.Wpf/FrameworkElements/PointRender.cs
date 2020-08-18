﻿// Based on
// ScatterPlotRender.cs by Charles Petzold, December 2008
// https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/march/foundations-writing-more-efficient-itemscontrols

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

using Darwin.Collections;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Darwin.Wpf.FrameworkElements
{
    public class PointRender : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource",
                typeof(ObservableNotifiableCollection<Darwin.Model.Point>),
                typeof(PointRender),
                new PropertyMetadata(OnItemsSourceChanged));

        public static readonly DependencyProperty BrushesProperty =
            DependencyProperty.Register("Brushes",
                typeof(Brush[]),
                typeof(PointRender),
                new FrameworkPropertyMetadata(null,
                        FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PointSizeProperty =
            DependencyProperty.Register("PointSize",
                typeof(double),
                typeof(PointVisuals),
                new FrameworkPropertyMetadata(2.0,
                        FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FeaturePointSizeProperty =
        DependencyProperty.Register("FeaturePointSize",
            typeof(double),
            typeof(PointVisuals),
            new FrameworkPropertyMetadata(5.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ContourScaleProperty =
            DependencyProperty.Register("ContourScale",
                typeof(double),
                typeof(PointVisuals),
                new FrameworkPropertyMetadata(1.0,
                        FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(PointRender));

        public ObservableNotifiableCollection<Darwin.Model.Point> ItemsSource
        {
            set { SetValue(ItemsSourceProperty, value); }
            get { return (ObservableNotifiableCollection<Darwin.Model.Point>)GetValue(ItemsSourceProperty); }
        }

        public Brush[] Brushes
        {
            set { SetValue(BrushesProperty, value); }
            get { return (Brush[])GetValue(BrushesProperty); }
        }

        public Brush Background
        {
            set { SetValue(BackgroundProperty, value); }
            get { return (Brush)GetValue(BackgroundProperty); }
        }

        public double PointSize
        {
            set { SetValue(PointSizeProperty, value); }
            get { return (double)GetValue(PointSizeProperty); }
        }

        public double ContourScale
        {
            set { SetValue(ContourScaleProperty, value); }
            get { return (double)GetValue(ContourScaleProperty); }
        }

        public double FeaturePointSize
        {
            set { SetValue(FeaturePointSizeProperty, value); }
            get { return (double)GetValue(FeaturePointSizeProperty); }
        }

        static void OnItemsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var render = obj as PointRender;
            
            if (render != null)
                render.OnItemsSourceChanged(args);
        }

        void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
        {
            if (args.OldValue != null)
            {
                ObservableNotifiableCollection<Darwin.Model.Point> coll = args.OldValue as ObservableNotifiableCollection<Darwin.Model.Point>;
                coll.CollectionChanged -= OnCollectionChanged;
                coll.ItemPropertyChanged -= OnItemPropertyChanged;
            }

            if (args.NewValue != null)
            {
                ObservableNotifiableCollection<Darwin.Model.Point> coll = args.NewValue as ObservableNotifiableCollection<Darwin.Model.Point>;
                coll.CollectionChanged += OnCollectionChanged;
                coll.ItemPropertyChanged += OnItemPropertyChanged;
            }

            InvalidateVisual();
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            InvalidateVisual();
        }

        void OnItemPropertyChanged(object sender, ItemPropertyChangedEventArgs args)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Background != System.Windows.Media.Brushes.Transparent)
                dc.DrawRectangle(Background, null, new Rect(RenderSize));

            if (ItemsSource == null || Brushes == null)
                return;

            foreach (Darwin.Model.Point dataPoint in ItemsSource)
            {
                dc.DrawEllipse(Brushes[0], null,
                    new System.Windows.Point(dataPoint.X / ContourScale, dataPoint.Y / ContourScale), PointSize, PointSize);
            }
        }
    }
}
