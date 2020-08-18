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
using Darwin.Database;
using Darwin.Helpers;
using Darwin.Model;
using Darwin.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Darwin.Wpf.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        public new string WindowTitle
        {
            get
            {
                if (DarwinDatabase == null)
                    return "DARWIN";

                return Path.GetFileName(DarwinDatabase.Filename) + " - DARWIN";
            }
        }

        private bool _showHome;
        public bool ShowHome
        {
            get => _showHome;
            set
            {
                _showHome = value;
                RaisePropertyChanged("ShowHome");

                if (_showHome)
                {
                    ShowImports = false;
                    ShowMaps = false;
                }
            }
        }

        private bool _showImports;
        public bool ShowImports
        {
            get => _showImports;
            set
            {
                _showImports = value;
                RaisePropertyChanged("ShowImports");

                if (_showImports)
                {
                    ShowHome = false;
                    ShowMaps = false;
                }
            }
        }

        private bool _showMaps;
        public bool ShowMaps
        {
            get => _showMaps;
            set
            {
                _showMaps = value;
                RaisePropertyChanged("ShowMaps");

                if (_showMaps)
                {
                    ShowImports = false;
                    ShowHome = false;
                }
            }
        }

        private DarwinDatabase _darwinDatabase;
        public DarwinDatabase DarwinDatabase
        {
            get
            {
                return _darwinDatabase;
            }
            set
            {
                _darwinDatabase = value;
                RaisePropertyChanged("FeatureSetTypeDisplay");
                RaisePropertyChanged("FeatureSetTypeVisibility");
                RaisePropertyChanged("DarwinDatabase");
                RaisePropertyChanged("WindowTitle");
            }
        }

        public Visibility FeatureSetTypeVisibility
        {
            get
            {
                if (string.IsNullOrEmpty(FeatureSetTypeDisplay))
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }
        }
        public string FeatureSetTypeDisplay
        {
            get
            {
                if (DarwinDatabase == null || DarwinDatabase.CatalogScheme == null)
                    return string.Empty;

                return DarwinDatabase.CatalogScheme.CollectionTerminology;
            }
        }

        private DatabaseFin _selectedFin;
        public DatabaseFin SelectedFin
        {
            get => _selectedFin;
            set
            {
                var saveOldFin = _selectedFin;

                _selectedFin = value;
                LoadSelectedFin();
                RaisePropertyChanged("SelectedFin");

                if (saveOldFin != null)
                    UnloadFin(saveOldFin);
            }
        }

        private ImageSource _selectedImageSource;
        public ImageSource SelectedImageSource
        {
            get => _selectedImageSource;
            set
            {
                _selectedImageSource = value;
                RaisePropertyChanged("SelectedImageSource");
            }
        }

        private ImageSource _selectedOriginalImageSource;
        public ImageSource SelectedOriginalImageSource
        {
            get => _selectedOriginalImageSource;
            set
            {
                _selectedOriginalImageSource = value;
                RaisePropertyChanged("SelectedOriginalImageSource");
            }
        }

        private Contour _selectedContour;
        public Contour SelectedContour
        {
            get => _selectedContour;
            set
            {
                _selectedContour = value;
                RaisePropertyChanged("SelectedContour");
            }
        }


        private ObservableNotifiableCollection<DatabaseFin> _fins;
        public ObservableNotifiableCollection<DatabaseFin> Fins
        {
            get
            {
                if (_fins == null)
                    _fins = new ObservableNotifiableCollection<DatabaseFin>();

                return _fins;
            }
            set
            {
                _fins = value;
                RaisePropertyChanged("Fins");
            }
        }

        private bool _nextEnabled;
        public bool NextEnabled
        {
            get => _nextEnabled;
            set
            {
                _nextEnabled = value;
                RaisePropertyChanged("NextEnabled");
            }
        }

        private bool _previousEnabled;
        public bool PreviousEnabled
        {
            get => _previousEnabled;
            set
            {
                _previousEnabled = value;
                RaisePropertyChanged("PreviousEnabled");
            }
        }

        public MainWindowViewModel()
        {
            ShowHome = true;
            _darwinDatabase = null;
            _fins = new ObservableNotifiableCollection<DatabaseFin>();
        }

        public void RefreshDatabase()
        {
            // This should probably do more
            RaisePropertyChanged("FeatureSetTypeDisplay");
            RaisePropertyChanged("FeatureSetTypeVisibility");
        }

        public string BackupDatabase()
        {
            return CatalogSupport.BackupDatabase(DarwinDatabase);
        }

        public void SaveSelectedItemAsFinz(string filename)
        {
            if (SelectedFin == null)
                return;

            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            var finCopy = FullyLoadFin();
            CatalogSupport.SaveFinz(DarwinDatabase.CatalogScheme, finCopy, filename, true);
        }

        public DatabaseFin FullyLoadFin()
        {
            DatabaseFin finCopy = null;

            if (SelectedFin != null)
                finCopy = CatalogSupport.FullyLoadFin(SelectedFin);

            return finCopy;
        }

        public void CheckSurveyAreaDatabaseNameFromBackup(string backupFile, out string surveyArea, out string databaseName)
        {
            CatalogSupport.CheckSurveyAreaDatabaseNameFromBackup(backupFile, out surveyArea, out databaseName);
        }

        public void CloseDatabase()
        {
            DarwinDatabase = null;
            SelectedFin = null;
            Fins = null;
            SelectedImageSource = null;
            SelectedOriginalImageSource = null;
            CatalogSupport.CloseDatabase(DarwinDatabase);
        }

        public string RestoreDatabase(string backupFile, string surveyArea, string databaseName)
        {
            return CatalogSupport.RestoreDatabase(backupFile, surveyArea, databaseName);
        }

        private void LoadSelectedFin()
        {
            if (SelectedFin == null)
            {
                SelectedImageSource = null;
                SelectedOriginalImageSource = null;
                SelectedContour = null;
            }
            else
            {
                DatabaseImage.FullyLoadDatabaseImages(SelectedFin.Images);
            }
        }

        private void UnloadFin(DatabaseFin fin)
        {
            if (fin == null)
                return;

            DatabaseImage.UnloadDatabaseImages(fin.Images);
        }
    }
}
