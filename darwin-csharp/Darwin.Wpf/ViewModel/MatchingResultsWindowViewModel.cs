﻿using Darwin.Database;
using Darwin.Helpers;
using Darwin.Matching;
using Darwin.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Darwin.Wpf.ViewModel
{
    public class MatchingResultsWindowViewModel : INotifyPropertyChanged
    {
        private bool _autoScroll;
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                _autoScroll = value;
                RaisePropertyChanged("AutoScroll");
            }
        }

        private DatabaseFin _databaseFin;
        public DatabaseFin DatabaseFin
        {
            get => _databaseFin;
            set
            {
                _databaseFin = value;
                RaisePropertyChanged("DatabaseFin");
            }
        }

        private MatchResults _matchResults;
        public MatchResults MatchResults
        {
            get => _matchResults;
            set
            {
                _matchResults = value;
                RaisePropertyChanged("MatchResults");
            }
        }

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

        private Result _selectedResult;
        public Result SelectedResult
        {
            get => _selectedResult;
            set
            {
                _selectedResult = value;
                RaisePropertyChanged("SelectedResult");

                CheckNextPreviousEnabled();
                LoadSelectedResult();
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

        private ImageSource _unknownImageSource;
        public ImageSource UnknownImageSource
        {
            get => _unknownImageSource;
            set
            {
                _unknownImageSource = value;
                RaisePropertyChanged("UnknownImageSource");
            }
        }

        private bool _showIDColumn;
        public bool ShowIDColumn
        {
            get => _showIDColumn;
            set
            {
                _showIDColumn = value;
                RaisePropertyChanged("ShowIDColumn");
            }
        }

        private bool _showInfoColumns;
        public bool ShowInfoColumns
        {
            get => _showInfoColumns;
            set
            {
                _showInfoColumns = value;
                RaisePropertyChanged("ShowInfoColumns");
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

        public int CurrentSelectedIndex
        {
            get
            {
                if (SelectedResult == null)
                    return -1;

                return MatchResults.Results.IndexOf(SelectedResult);
            }
        }

        private Contour _unknownContour;
        public Contour UnknownContour
        {
            get => _unknownContour;
            set
            {
                _unknownContour = value;
                RaisePropertyChanged("UnknownContour");
            }
        }

        private Contour _dbContour;
        public Contour DBContour
        {
            get => _dbContour;
            set
            {
                _dbContour = value;
                RaisePropertyChanged("DBContour");
            }
        }

        private double _contourXOffset;
        public double ContourXOffset
        {
            get => _contourXOffset;
            set
            {
                _contourXOffset = value;
                RaisePropertyChanged("ContourXOffset");
            }
        }

        private double _contourYOffset;
        public double ContourYOffset
        {
            get => _contourYOffset;
            set
            {
                _contourYOffset = value;
                RaisePropertyChanged("ContourYOffset");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MatchingResultsWindowViewModel(
            DatabaseFin unknownFin,
            MatchResults matchResults,
            DarwinDatabase database)
        {
            if (unknownFin == null)
                throw new ArgumentNullException(nameof(unknownFin));

            if (matchResults == null)
                throw new ArgumentNullException(nameof(matchResults));

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            ShowIDColumn = true;
            ShowInfoColumns = true;
            AutoScroll = false;

            NextEnabled = true;
            PreviousEnabled = true;

            DatabaseFin = unknownFin;

            if (DatabaseFin != null && DatabaseFin.OriginalFinImage != null)
                UnknownImageSource = DatabaseFin.OriginalFinImage.ToImageSource();

            MatchResults = matchResults;
            Database = database;

            if (MatchResults.Results != null && MatchResults.Results.Count > 0)
            {
                SelectedResult = MatchResults.Results[0];
                LoadSelectedResult();
            }
        }

        private void CheckNextPreviousEnabled()
        {
            int curIndex = CurrentSelectedIndex;

            if (curIndex <= 0)
                PreviousEnabled = false;
            else if (MatchResults.Results != null && MatchResults.Results.Count > 0)
                PreviousEnabled = true;

            if (MatchResults.Results != null && MatchResults.Results.Count > 0 && curIndex < MatchResults.Results.Count - 1)
                NextEnabled = true;
            else
                NextEnabled = false;
        }

        private void LoadSelectedResult()
        {
            if (SelectedResult != null)
            {
                UpdateOutlines(SelectedResult.unknownContour, SelectedResult.dbContour);

                // TODO: Cache images?
                if (!string.IsNullOrEmpty(SelectedResult.ImageFilename))
                {
                    //CatalogSupport.UpdateFinFieldsFromImage(Options.CurrentUserOptions.CurrentDataPath, SelectedFin);

                    //SelectedContour = new Contour(SelectedResult.dbContour, SelectedResult..Scale);

                    string fullImageFilename = Path.Combine(Options.CurrentUserOptions.CurrentSurveyAreaPath, SelectedResult.ImageFilename);

                    if (File.Exists(fullImageFilename))
                    {
                        try
                        {
                            DatabaseFin tempFin = new DatabaseFin()
                            {
                                ImageFilename = SelectedResult.ImageFilename
                            };

                            CatalogSupport.UpdateFinFieldsFromImage(Options.CurrentUserOptions.CurrentSurveyAreaPath, tempFin);

                            var realOriginalImageFilename = string.Empty;
                            if (tempFin != null && !string.IsNullOrEmpty(tempFin.ImageFilename))
                            {
                                realOriginalImageFilename = Path.Combine(Options.CurrentUserOptions.CurrentSurveyAreaPath, tempFin.ImageFilename);
                            }

                            System.Drawing.Image img;
                            if (!string.IsNullOrEmpty(realOriginalImageFilename) && File.Exists(realOriginalImageFilename))
                                img = System.Drawing.Image.FromFile(realOriginalImageFilename);
                            else
                                img = System.Drawing.Image.FromFile(fullImageFilename);

                            var bitmap = new Bitmap(img);
                            // TODO: Hack for HiDPI -- this should be more intelligent.
                            bitmap.SetResolution(96, 96);

                            // TODO: Refactor this so we're not doing it every time, which is a little crazy
                            if (tempFin.ImageMods != null && tempFin.ImageMods.Count > 0)
                                bitmap = ModificationHelper.ApplyImageModificationsToOriginal(bitmap, tempFin.ImageMods);

                            // We're directly changing the source, not the bitmap property on DatabaseFin
                            SelectedImageSource = bitmap.ToImageSource();
                        }
                        catch (Exception ex)
                        {
                            // TODO
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            }
        }

        private void UpdateOutlines(FloatContour unknownContour, FloatContour dbContour)
        {
            if (unknownContour == null || dbContour == null)
                return;

            Contour unk, db;
            double xOffset, yOffset;
            FloatContour.FitContoursToSize(unknownContour, dbContour, out unk, out db, out xOffset, out yOffset);

            ContourXOffset = xOffset;
            ContourYOffset = yOffset;
            UnknownContour = unk;
            DBContour = db;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}