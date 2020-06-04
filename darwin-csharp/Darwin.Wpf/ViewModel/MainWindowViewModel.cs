﻿using Darwin.Collections;
using Darwin.Database;
using Darwin.Helpers;
using Darwin.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Darwin.Wpf.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
            }
        }

        private DatabaseFin _selectedFin;
        public DatabaseFin SelectedFin
        {
            get => _selectedFin;
            set
            {
                _selectedFin = value;
                RaisePropertyChanged("SelectedFin");

                LoadSelectedFin();
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

        public MainWindowViewModel()
        {
            _darwinDatabase = null;
            _fins = new ObservableNotifiableCollection<DatabaseFin>();
        }

        private void LoadSelectedFin()
        {
            if (SelectedFin != null)
            {
                // TODO: Cache images?
                if (!string.IsNullOrEmpty(SelectedFin.ImageFilename))
                {
                    CatalogSupport.UpdateFinFieldsFromImage(Options.CurrentUserOptions.CurrentDataPath, SelectedFin);

                    SelectedContour = new Contour(SelectedFin.FinOutline.ChainPoints, SelectedFin.Scale);

                    string fullImageFilename = Path.Combine(Options.CurrentUserOptions.CurrentDataPath, SelectedFin.ImageFilename);

                    if (File.Exists(fullImageFilename))
                    {
                        try
                        {
                            var img = System.Drawing.Image.FromFile(fullImageFilename);

                            var bitmap = new Bitmap(img);
                            // TODO: Hack for HiDPI -- this should be more intelligent.
                            bitmap.SetResolution(96, 96);

                            SelectedOriginalImageSource = bitmap.ToImageSource();

                            // TODO: Refactor this so we're not doing it every time, which is a little crazy
                            if (SelectedFin.ImageMods != null && SelectedFin.ImageMods.Count > 0)
                                bitmap = ModificationHelper.ApplyImageModificationsToOriginal(bitmap, SelectedFin.ImageMods);

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

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}