// Code based on HamburgerMenu by Alican Erdogan 
// https://github.com/alicanerdogan/HamburgerMenu
// Copied here because the original NuGet doesn't work 100% with .NET Core
// Original license MIT

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Darwin.Wpf.Controls
{
    public class HamburgerMenuItem : ListBoxItem
    {
        static HamburgerMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenuItem), new FrameworkPropertyMetadata(typeof(HamburgerMenuItem)));
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(HamburgerMenuItem), new PropertyMetadata(String.Empty));


        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(HamburgerMenuItem), new PropertyMetadata(null));

        //public ImageSource Icon
        //{
        //    get { return (ImageSource)GetValue(IconProperty); }
        //    set { SetValue(IconProperty, value); }
        //}

        //public static readonly DependencyProperty IconProperty =
        //    DependencyProperty.Register("Icon", typeof(ImageSource), typeof(HamburgerMenuItem), new PropertyMetadata(null));

        public Brush SelectionIndicatorColor
        {
            get { return (Brush)GetValue(SelectionIndicatorColorProperty); }
            set { SetValue(SelectionIndicatorColorProperty, value); }
        }

        public static readonly DependencyProperty SelectionIndicatorColorProperty =
            DependencyProperty.Register("SelectionIndicatorColor", typeof(Brush), typeof(HamburgerMenuItem), new PropertyMetadata(Brushes.Blue));

        public ICommand SelectionCommand
        {
            get { return (ICommand)GetValue(SelectionCommandProperty); }
            set { SetValue(SelectionCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectionCommandProperty =
            DependencyProperty.Register("SelectionCommand", typeof(ICommand), typeof(HamburgerMenuItem), new PropertyMetadata(null));
    }
}
