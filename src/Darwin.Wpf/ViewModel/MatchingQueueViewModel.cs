﻿// This file is part of DARWIN.
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;
using Darwin.Wpf.Extensions;
using Darwin.Matching;
using System.Diagnostics;
using Darwin.Features;

namespace Darwin.Wpf.ViewModel
{
    public class MatchingQueueViewModel : INotifyPropertyChanged
    {
        private static object selectedFinSync = new object();

        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                RaisePropertyChanged("WindowTitle");
            }
        }

        private DatabaseFin _selectedFin;
        public DatabaseFin SelectedFin
        {
            get => _selectedFin;
            set
            {
                lock (selectedFinSync)
                {
                    try
                    {
                        _selectedFin = value;

                        RaisePropertyChanged("SelectedFin");

                        if (_selectedFin == null)
                            SelectedImageSource = null;
                        else
                            SelectedImageSource = _selectedFin.PrimaryImage.FinImage.ToImageSource();
                    }
                    catch (Exception ex)
                    {
                        // The above can throw exceptions if the bitmaps are large,
                        // since we're running an update on a separate thread and 
                        // possibly modifying the Bitmaps elsewhere.
                        Trace.WriteLine(ex);
                    }
                    MatchingQueue.CheckQueueRunnable();
                }
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

        private int _queueProgressPercent;
        public int QueueProgressPercent
        {
            get => _queueProgressPercent;
            set
            {
                _queueProgressPercent = value;
                RaisePropertyChanged("QueueProgressPercent");
            }
        }

        private int _currentUnknownPercent;
        public int CurrentUnknownPercent
        {
            get => _currentUnknownPercent;
            set
            {
                _currentUnknownPercent = value;
                RaisePropertyChanged("CurrentUnknownPercent");
            }
        }

        private bool _pauseMatching;
        public bool PauseMatching
        {
            get => _pauseMatching;
            set
            {
                _pauseMatching = value;
                RaisePropertyChanged("PauseMatching");
            }
        }

        private bool _cancelMatching;
        public bool CancelMatching
        {
            get => _cancelMatching;
            set
            {
                _cancelMatching = value;
                RaisePropertyChanged("CancelMatching");
            }
        }

        private MatchingQueue _matchingQueue;
        public MatchingQueue MatchingQueue
        {
            get => _matchingQueue;
            set
            {
                _matchingQueue = value;
                RaisePropertyChanged("MatchingQueue");
            }
        }

        public string IndividualTerminology
        {
            get
            {
                if (_database == null)
                    return string.Empty;

                return _database.CatalogScheme.IndividualTerminology;
            }
        }

        public string IndividualTerminologyInitialCaps
        {
            get
            {
                if (_database == null)
                    return string.Empty;

                return _database.CatalogScheme.IndividualTerminologyInitialCaps;
            }
        }

        private DarwinDatabase _database;

        public MatchingQueueViewModel()
        {
            WindowTitle = "Matching Queue";

            _database = CatalogSupport.OpenDatabase(Options.CurrentUserOptions.DatabaseFileName,
                Options.CurrentUserOptions.DefaultCatalogScheme, false);
            MatchingQueue = new MatchingQueue(
                _database,
                RegistrationMethodType.TrimOptimalTip,
                RangeOfPointsType.AllPoints);
        }

        // Pass-through
        public void SaveQueue(string filename)
        {
            MatchingQueue.SaveQueue(filename);

            WindowTitle = "Matching Queue - " + System.IO.Path.GetFileName(filename);
        }

        // Pass-through
        public void LoadQueue(string filename)
        {
            SelectedFin = null;
            MatchingQueue.LoadQueue(filename);
            WindowTitle = "Matching Queue - " + System.IO.Path.GetFileName(filename);

            if (MatchingQueue.Fins?.Count > 0)
                SelectedFin = MatchingQueue.Fins.First();
        }

        // Pass-through
        public MatchResults LoadMatchResults(string filename, out DarwinDatabase database, out DatabaseFin databaseFin)
        {
            return MatchResults.Load(filename, out database, out databaseFin);
        }

        public void SaveMatchResults()
        {
            MatchingQueue.SaveMatchResults(Options.CurrentUserOptions.CurrentMatchQueueResultsPath);
        }

        public string GetMatchSummary()
        {
            return MatchingQueue.GetSummary();
        }

        public bool VerifyMatchSettings()
        {
            if (MatchingQueue.Fins.Count < 1)
                return true;

            Match testMatch = null;
            switch (MatchingQueue.Database.CatalogScheme.FeatureSetType)
            {
                case Features.FeatureSetType.DorsalFin:
                    testMatch = new Match(
                        MatchingQueue.Fins[0],
                        MatchingQueue.Database, null,
                        MatchingQueue.RegistrationMethod,
                        (MatchingQueue.RangeOfPoints == RangeOfPointsType.AllPoints) ? true : false);
                    break;

                case Features.FeatureSetType.Bear:
                    testMatch = new Match(
                        MatchingQueue.Fins[0],
                        MatchingQueue.Database,
                        null,
                        null,
                        true);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return testMatch.VerifyMatchSettings();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
