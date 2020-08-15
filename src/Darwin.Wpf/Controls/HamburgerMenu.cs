// Below code based on HamburgerMenu by Alican Erdogan 
// https://github.com/alicanerdogan/HamburgerMenu
// Copied here because the original NuGet doesn't work 100% with .NET Core
// Original license MIT

// Below code based on HamburgerMenu by Alican Erdogan 
// https://github.com/alicanerdogan/HamburgerMenu
// Original license MIT

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

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Darwin.Wpf.Controls
{
    public class HamburgerMenu : ContentControl
    {
        static HamburgerMenu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenu), new FrameworkPropertyMetadata(typeof(HamburgerMenu)));
        }

        public new List<HamburgerMenuItem> Content
        {
            get { return (List<HamburgerMenuItem>)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public new static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(List<HamburgerMenuItem>), typeof(HamburgerMenu),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public override void BeginInit()
        {
            Content = new List<HamburgerMenuItem>();
            base.BeginInit();
        }

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set
            {
                SetValue(IsOpenProperty, value);
            }
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(HamburgerMenu), new PropertyMetadata(true));

        public System.Windows.Media.Brush MenuIconColor
        {
            get { return (System.Windows.Media.Brush)GetValue(MenuIconColorProperty); }
            set { SetValue(MenuIconColorProperty, value); }
        }

        public static readonly DependencyProperty MenuIconColorProperty =
            DependencyProperty.Register("MenuIconColor", typeof(System.Windows.Media.Brush), typeof(HamburgerMenu), new PropertyMetadata(System.Windows.Media.Brushes.White));


        public Brush SelectionIndicatorColor
        {
            get { return (Brush)GetValue(SelectionIndicatorColorProperty); }
            set { SetValue(SelectionIndicatorColorProperty, value); }
        }

        public static readonly DependencyProperty SelectionIndicatorColorProperty =
            DependencyProperty.Register("SelectionIndicatorColor", typeof(Brush), typeof(HamburgerMenu), new PropertyMetadata(Brushes.Red));

        public Brush MenuItemForeground
        {
            get { return (Brush)GetValue(MenuItemForegroundProperty); }
            set { SetValue(MenuItemForegroundProperty, value); }
        }

        public static readonly DependencyProperty MenuItemForegroundProperty =
            DependencyProperty.Register("MenuItemForeground", typeof(Brush), typeof(HamburgerMenu), new PropertyMetadata(Brushes.Transparent));

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(HamburgerMenu), new PropertyMetadata(0));
    }
}
