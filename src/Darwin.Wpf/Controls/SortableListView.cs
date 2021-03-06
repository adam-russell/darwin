﻿// Inspired and code from StackOverflow answers at:
// https://stackoverflow.com/questions/994148/best-way-to-make-wpf-listview-gridview-sort-on-column-header-clicking
// Original code license CC BY-SA 3.0 https://creativecommons.org/licenses/by-sa/3.0/
// Also takes code from https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked?redirectedfrom=MSDN

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
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Darwin.Wpf.Controls
{
    public partial class SortableListView : ListView
    {
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;

                        if (_lastHeaderClicked != null)
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                            direction = ListSortDirection.Descending;
                        else
                            direction = ListSortDirection.Ascending;
                    }

                    if (headerClicked.Column.DisplayMemberBinding == null)
                    {
                        _lastHeaderClicked = null;
                        _lastDirection = ListSortDirection.Ascending;
                    }
                    else
                    {
                        string sortString = ((Binding)headerClicked.Column.DisplayMemberBinding).Path.Path;

                        Sort(sortString, direction);

                        if (direction == ListSortDirection.Ascending)
                            headerClicked.Column.HeaderTemplate = FindResource("ListHeaderTemplateArrowUp") as DataTemplate;
                        else
                            headerClicked.Column.HeaderTemplate = FindResource("ListHeaderTemplateArrowDown") as DataTemplate;

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
        }

        public void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(this.ItemsSource != null ? this.ItemsSource : this.Items);

            dataView.SortDescriptions.Clear();
            SortDescription sD = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sD);
            dataView.Refresh();
        }

        public void SetFilter(Predicate<object> filter)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(this.ItemsSource != null ? this.ItemsSource : this.Items);
            dataView.Filter = filter;
        }

        public void RefreshFilter()
        {
            CollectionViewSource.GetDefaultView(this.ItemsSource != null ? this.ItemsSource : this.Items).Refresh();
        }
    }
}
