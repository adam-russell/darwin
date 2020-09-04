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
using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Wpf.ViewModel
{
    public class DeveloperToolsViewModel : BaseViewModel
    {
        private DarwinDatabase _database;
        public DarwinDatabase Database
        {
            get => _database;
            set
            {
                _database = value;
                RaisePropertyChanged("Database");
            }
        }

        private int _maskImageWidth;
        public int MaskImageWidth
        {
            get => _maskImageWidth;
            set
            {
                _maskImageWidth = value;
                RaisePropertyChanged("MaskImageWidth");
            }
        }

        private int _maskImageHeight;
        public int MaskImageHeight
        {
            get => _maskImageHeight;
            set
            {
                _maskImageHeight = value;
                RaisePropertyChanged("MaskImageHeight");
            }
        }

        private int _classificationImageWidth;
        public int ClassificationImageWidth
        {
            get => _classificationImageWidth;
            set
            {
                _classificationImageWidth = value;
                RaisePropertyChanged("ClassificationImageWidth");
            }
        }

        private int _classificationImageHeight;
        public int ClassificationImageHeight
        {
            get => _classificationImageHeight;
            set
            {
                _classificationImageHeight = value;
                RaisePropertyChanged("ClassificationImageHeight");
            }
        }

        private bool _remaskClassification;
        public bool RemaskClassification
        {
            get => _remaskClassification;
            set
            {
                _remaskClassification = value;
                RaisePropertyChanged("RemaskClassification");
            }
        }

        public DeveloperToolsViewModel(DarwinDatabase database)
        {
            WindowTitle = "Developer Tools";
            Database = database;

            MaskImageHeight = AppSettings.DefaultMaskImageHeight;
            MaskImageWidth = AppSettings.DefaultMaskImageWidth;

            ClassificationImageWidth = AppSettings.DefaultClassificationImageWidth;
            ClassificationImageHeight = AppSettings.DefaultClassificationImageHeight;
        }
    }
}
