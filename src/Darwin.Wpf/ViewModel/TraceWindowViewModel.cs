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
using Darwin.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Darwin.Database;
using Darwin.Wpf.Model;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using Darwin.Features;
using Darwin.Helpers;
using System.Drawing.Imaging;
using Darwin.Model;

namespace Darwin.Wpf.ViewModel
{
	public class TraceWindowViewModel : BaseViewModel
	{
		private Bitmap _bitmap;
		public Bitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				RaisePropertyChanged("Bitmap");

				if (_bitmap != null)
				{
					// Copy it to the OriginalBitmap property, as well, if it hasn't been set
					if (OriginalBitmap == null)
						OriginalBitmap = new Bitmap(_bitmap);

					if (BaseBitmap == null)
						BaseBitmap = new Bitmap(_bitmap);

					ImageSource = _bitmap.ToImageSource();
				}
				else if (ImageSource != null)
				{
					ImageSource = null;
				}
			}
		}

		private ImageSource _imageSource;
		public ImageSource ImageSource
		{
			get => _imageSource;
			set
			{
				_imageSource = value;
				RaisePropertyChanged("ImageSource");
			}
		}

		public void UpdateImage()
		{
			ImageSource = _bitmap.ToImageSource();
		}

		private Bitmap _baseBitmap;
		/// <summary>
		/// The "base" bitmap that brightness/contrast operate on.
		/// </summary>
		public Bitmap BaseBitmap
		{
			get => _baseBitmap;
			set
			{
				_baseBitmap = value;
				RaisePropertyChanged("BaseBitmap");
			}
		}

		private Bitmap _originalBitmap;

		/// <summary>
		/// The original bitmap/image prior to any modifications.
		/// </summary>
		public Bitmap OriginalBitmap
		{
			get => _originalBitmap;
			set
			{
				_originalBitmap = value;
				RaisePropertyChanged("OriginalBitmap");
			}
		}

		private Contour _contour;
		public Contour Contour
		{
			get => _contour;
			set
			{
				_contour = value;
				RaisePropertyChanged("Contour");

				if (_contour != null && _contour.Length > 0)
					IdentifyFeaturesEnabled = true;
				else
					IdentifyFeaturesEnabled = false;
			}
		}

		private Contour _backupContour;
		public Contour BackupContour
		{
			get => _backupContour;
			set
			{
				_backupContour = value;
				RaisePropertyChanged("BackupContour");
			}
		}

		private Outline _outline;
		public Outline Outline
		{
			get => _outline;
			set
			{
				_outline = value;
				RaisePropertyChanged("Outline");
			}
		}

		private ObservableNotifiableCollection<CoordinateFeaturePoint> _coordinateFeaturePoints;
		public ObservableNotifiableCollection<CoordinateFeaturePoint> CoordinateFeaturePoints
        {
			get
			{
				if (_coordinateFeaturePoints == null)
					_coordinateFeaturePoints = new ObservableNotifiableCollection<CoordinateFeaturePoint>();

				return _coordinateFeaturePoints;
			}
			set
            {
				_coordinateFeaturePoints = value;
				RaisePropertyChanged("CoordinateFeaturePoints");
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

		private TraceToolType _traceTool;
		public TraceToolType TraceTool
		{
			get => _traceTool;
			set
			{
				_traceTool = value;
				RaisePropertyChanged("TraceTool");
			}
		}

		private TraceStepType _traceStep;
		public TraceStepType TraceStep
		{
			get => _traceStep;
			set
			{
				_traceStep = value;
				RaisePropertyChanged("TraceStep");
			}
		}

		private float _normScale;
		public float NormScale
        {
			get => _normScale;
            set
            {
				_normScale = value;
				RaisePropertyChanged("NormScale");
            }
        }

		private bool _traceLocked;
		public bool TraceLocked
		{
			get => _traceLocked;
			set
			{
				_traceLocked = value;
				RaisePropertyChanged("TraceLocked");
			}
		}

		private bool _identifyFeaturesEnabled;
		public bool IdentifyFeaturesEnabled
		{
			get => _identifyFeaturesEnabled;
			set
			{
				_identifyFeaturesEnabled = value;
				RaisePropertyChanged("IdentifyFeaturesEnabled");
			}
		}

		private Visibility _traceToolsVisibility;
		public Visibility TraceToolsVisibility
        {
			get => _traceToolsVisibility;
			set
            {
				_traceToolsVisibility = value;
				RaisePropertyChanged("TraceToolsVisibility");

				if (_traceToolsVisibility == Visibility.Visible)
					FeatureToolsVisibility = Visibility.Collapsed;
				else
					FeatureToolsVisibility = Visibility.Visible;
            }
        }

		private Visibility _featureToolsVisibility;
		public Visibility FeatureToolsVisibility
		{
			get => _featureToolsVisibility;
			set
			{
				_featureToolsVisibility = value;
				RaisePropertyChanged("FeatureToolsVisibility");
			}
		}

		private Visibility _editVisibility;
		public Visibility EditVisibility
		{
			get => _editVisibility;
			set
			{
				_editVisibility = value;
				RaisePropertyChanged("EditVisibility");
				RaisePropertyChanged("InverseEditVisibility");
			}
		}

		public Visibility InverseEditVisibility
		{
			get
            {
				if (_editVisibility == Visibility.Visible)
					return Visibility.Collapsed;

				return Visibility.Visible;
            }
        }

		private Visibility _matchVisibility;
		public Visibility MatchVisibility
		{
			get => _matchVisibility;
			set
			{
				_matchVisibility = value;
				RaisePropertyChanged("MatchVisibility");
			}
		}

		private Visibility _topToolbarVisibility;
		public Visibility TopToolbarVisibility
		{
			get => _topToolbarVisibility;
			set
			{
				_topToolbarVisibility = value;
				RaisePropertyChanged("TopToolbarVisibility");
			}
		}

		private Visibility _saveVisibility;
		public Visibility SaveVisibility
		{
			get => _saveVisibility;
			set
			{
				_saveVisibility = value;
				RaisePropertyChanged("SaveVisibility");
			}
		}

		private Visibility _updateVisibility;
		public Visibility UpdateVisibility
		{
			get => _updateVisibility;
			set
			{
				_updateVisibility = value;
				RaisePropertyChanged("UpdateVisibility");
			}
		}

		private bool _viewerMode;
		public bool ViewerMode
		{
			get => _viewerMode;
			set
            {
				_viewerMode = value;
				RaisePropertyChanged("ViewerMode");

				if (_viewerMode)
                {
					TraceStep = TraceStepType.IdentifyFeatures;

					TraceTool = TraceToolType.Hand;
					MatchVisibility = Visibility.Collapsed;
					SaveVisibility = Visibility.Collapsed;
					UpdateVisibility = Visibility.Collapsed;
					AddToDatabaseVisibility = Visibility.Collapsed;

					TopToolbarVisibility = Visibility.Collapsed;
					TraceLocked = true;
					TraceFinalized = true;
					FeatureToolsVisibility = Visibility.Collapsed;

					//SetTraceStepIdentifyFeatures();
				}
				else
                {
					TraceTool = TraceToolType.MovePoint;
					TopToolbarVisibility = Visibility.Visible;
					TraceToolsVisibility = Visibility.Visible;
					UpdateVisibility = Visibility.Visible;
					//MatchVisibility = Visibility.Visible;
					//AddToDatabaseVisibility = Visibility.Visible;
					TraceStep = TraceStepType.TraceOutline;
				}

				if (Options.CurrentUserOptions.MatchingScheme == MatchingSchemeType.MachineLearning)
					TopToolbarVisibility = Visibility.Collapsed;
			}
		}

		private Visibility _addToDatabaseVisibility;
		public Visibility AddToDatabaseVisibility
        {
			get => _addToDatabaseVisibility;
			set
			{
				_addToDatabaseVisibility = value;
				RaisePropertyChanged("AddToDatabaseVisibility");
			}
		}

		// TODO: Do we need both finalized & locked?
		private bool _traceFinalized;
		public bool TraceFinalized
		{
			get => _traceFinalized;
			set
			{
				_traceFinalized = value;
				RaisePropertyChanged("TraceFinalized");

				if (TraceFinalized)
					TraceToolsVisibility = Visibility.Collapsed;
				else
					TraceToolsVisibility = Visibility.Visible;
			}
		}

		private bool _traceSnapped;
		public bool TraceSnapped
		{
			get => _traceSnapped;
			set
			{
				_traceSnapped = value;
				RaisePropertyChanged("TraceSnapped");
			}
		}

		private bool _preventPropagation;

		private float _zoomRatio;
		public float ZoomRatio
		{
			get => _zoomRatio;
			set
			{
				_zoomRatio = value;

				RaisePropertyChanged("ZoomRatio");
				RaisePropertyChanged("ZoomPointSize");

				if (_preventPropagation)
				{
					_preventPropagation = false;
				}
				else
				{
					var zoomSlider = (float)Math.Round(Math.Log(_zoomRatio) / Math.Log(2), 4);

					if (zoomSlider != _zoomSlider)
					{
						_preventPropagation = true;
						ZoomSlider = zoomSlider;
					}
				}
			}
		}

		private float _zoomSlider;
		public float ZoomSlider
        {
			get => _zoomSlider;
			set
            {
				_zoomSlider = value;

				RaisePropertyChanged("ZoomSlider");

				if (_preventPropagation)
                {
					_preventPropagation = false;
                }
                else
				{ 
					var newRatio = (float)Math.Round(Math.Pow(2, _zoomSlider), 4);

					if (ZoomRatio != newRatio)
					{
						_preventPropagation = true;
						ZoomRatio = newRatio;
					}
				}
			}
        }

		private List<double> _zoomValues;
		public List<double> ZoomValues
		{
			get => _zoomValues;
			set
			{
				_zoomValues = value;
				RaisePropertyChanged("ZoomValues");
			}
		}

		public float ZoomPointSize
		{
			get
			{
				if (_zoomRatio < 1.0f)
					return Options.CurrentUserOptions.DrawingPointSize / _zoomRatio;

				return Options.CurrentUserOptions.DrawingPointSize;
			}
		}

		public bool UndoEnabled
		{
			get
			{
				if (_undoItems == null)
					return false;

				return _undoItems.Count > 0;
			}
		}
		public ObservableStack<Modification> _undoItems;
		public ObservableStack<Modification> UndoItems
		{
			get => _undoItems;
			set
			{
				_undoItems = value;
				RaisePropertyChanged("UndoItems");
				RaisePropertyChanged("UndoEnabled");
			}
		}

		public bool RedoEnabled
        {
            get
            {
				if (_redoItems == null)
					return false;

				return _redoItems.Count > 0;
            }
        }
		public ObservableStack<Modification> _redoItems;
		public ObservableStack<Modification> RedoItems
		{
			get => _redoItems;
			set
			{
				_redoItems = value;
				RaisePropertyChanged("RedoItems");
				RaisePropertyChanged("RedoEnabled");
			}
		}

		private ObservableCollection<Category> _categories;
		public ObservableCollection<Category> Categories
		{
			get => _categories;
			set
			{
				_categories = value;
				RaisePropertyChanged("Categories");
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

		private string _statusBarMessage;
		public string StatusBarMessage
		{
			get => _statusBarMessage;
			set
			{
				_statusBarMessage = value;
				RaisePropertyChanged("StatusBarMessage");
			}
		}

		public string IndividualTerminology
		{
			get
			{
				return Database?.CatalogScheme?.IndividualTerminology;
			}
		}

		public string IndividualTerminologyInitialCaps
		{
			get
			{
				return Database?.CatalogScheme?.IndividualTerminologyInitialCaps;
			}
		}

		private MatchingResultsWindow _matchingResultsWindow;

		private TraceWindowViewModel()
        {
			if (Options.CurrentUserOptions.MatchingScheme == MatchingSchemeType.MachineLearning)
				TopToolbarVisibility = Visibility.Collapsed;
			else
				TopToolbarVisibility = Visibility.Visible;

			TraceToolsVisibility = Visibility.Visible;
			SaveVisibility = Visibility.Visible;
			MatchVisibility = Visibility.Visible;
			AddToDatabaseVisibility = Visibility.Visible;
			EditVisibility = Visibility.Collapsed;
			UpdateVisibility = Visibility.Collapsed;

			//DatabaseFin = new DatabaseFin();
			TraceStep = TraceStepType.TraceOutline;
			Categories = new ObservableCollection<Category>();
			AttachEvents();
		}

		public TraceWindowViewModel(DatabaseFin fin, bool readOnly = false)
			: this()
        {
			LoadFin(fin);

			if (readOnly)
			{
				TraceLocked = true;
				TraceFinalized = true;
				ViewerMode = true;
			}
		}

		public TraceWindowViewModel(DatabaseFin fin, DarwinDatabase db)
			: this(fin)
        {
			Database = db;
			Categories = db.Categories;

			StatusBarMessage = Database?.CatalogScheme?.TraceInstructionsTerminology;
		}

		// Little hacky, keeping a reference to the MatchResultsWindow, so we can close it when adding to the DB
		public TraceWindowViewModel(DatabaseFin fin, DarwinDatabase db, string windowTitle, MatchingResultsWindow matchingResultsWindow)
			: this(fin, db)
        {
			WindowTitle = windowTitle;

			if (matchingResultsWindow != null)
			{
				_matchingResultsWindow = matchingResultsWindow;
				MatchVisibility = Visibility.Collapsed;
				SaveVisibility = Visibility.Collapsed;

				TopToolbarVisibility = Visibility.Collapsed;
				TraceLocked = true;
				TraceFinalized = true;
				FeatureToolsVisibility = Visibility.Collapsed;
			}
        }

		public TraceWindowViewModel(DatabaseFin fin, DarwinDatabase db, string windowTitle, MainWindow mainWindow, bool readOnly = false)
			: this(fin, db)
		{
			WindowTitle = windowTitle;

			if (mainWindow != null || readOnly)
			{
				TraceLocked = true;
				TraceFinalized = true;
				ViewerMode = true;

				if (!readOnly)
					EditVisibility = Visibility.Visible;
			}
		}

		public TraceWindowViewModel(string imageFilename, DarwinDatabase db)
			: this()
		{
			var image = OpenImage(imageFilename);

			DatabaseFin = new DatabaseFin(image);

			if (db.Categories != null && db.Categories.Count > 0)
				DatabaseFin.DamageCategory = db.Categories[0].Name;

			Categories = db.Categories;

			Database = db;

			//ImageLocked = false;
			TraceLocked = false;

			NormScale = 1.0f;

			TraceStep = TraceStepType.TraceOutline;
			TraceTool = TraceToolType.Hand;
			ZoomRatio = 1.0f;
			ZoomValues = new List<double>();

			WindowTitle = Path.GetFileName(imageFilename);

			StatusBarMessage = Database?.CatalogScheme?.TraceInstructionsTerminology;
		}

		public DatabaseImage OpenImage(string filename)
        {
			var image = new DatabaseImage(filename);
			Bitmap = image.FinImage;
			return image;
		}

		public void SaveFinz(string filename)
        {
			UpdateDatabaseFin();
			CatalogSupport.SaveFinz(Database.CatalogScheme, DatabaseFin, filename);
        }

		public void SaveToDatabase()
        {
			UpdateDatabaseFin();
			CatalogSupport.SaveToDatabase(Database, DatabaseFin);

			// Check whether we have a reference to the MatchingResultsWindow.  If so,
			// we got this fin passed back as a match/no match.  When we add to the database,
			// let's close the matching results window.
			if (_matchingResultsWindow != null)
				_matchingResultsWindow.Close();
		}

		public void SaveSightingData()
        {
			if (DatabaseFin != null)
            {
				// TODO: Filename logic should probably be elsewhere
				var filename = Path.Combine(Options.CurrentUserOptions.CurrentSightingsPath, "SightingDataLogForArea_" + Options.CurrentUserOptions.CurrentSurveyArea + ".txt");
				DatabaseFin.SaveSightingData(filename);
			}
        }

		public void LoadCoordinateFeaturePoints()
        {
			// We're directly bound to _vm.CoordinateFeaturePoints, so we want to add to it rather than
			// replace it entirely, if possible.
			if (CoordinateFeaturePoints == null)
				CoordinateFeaturePoints = new ObservableNotifiableCollection<CoordinateFeaturePoint>();

			CoordinateFeaturePoints.Clear();

			if (Outline?.FeatureSet?.CoordinateFeaturePointList != null)
			{
				foreach (var coordFeature in Outline.FeatureSet.CoordinateFeaturePointList)
					CoordinateFeaturePoints.Add(coordFeature);
			}
		}

		public void UpdateDatabaseFin()
        {
			DatabaseFin.PrimaryImage.FinOutline = Outline;
			DatabaseFin.PrimaryImage.FinOutline.Scale = NormScale;
			DatabaseFin.PrimaryImage.FinImage = new Bitmap(Bitmap);

			if (DatabaseFin.PrimaryImage.ImageMods == null)
				DatabaseFin.PrimaryImage.ImageMods = new List<ImageMod>();

			var addedMods = new List<ImageMod>();

			if (UndoItems != null)
			{
				addedMods = UndoItems
					.Where(u => u.ImageMod != null && (u.ModificationType == ModificationType.Image || u.ModificationType == ModificationType.Both))
					.Select(u => u.ImageMod)
					.Reverse() // Very important -- these are stored backwards in UndoItems!
					.ToList();

				UndoItems.Clear();
				RedoItems.Clear();
            }

			// We're adding mods in case we opened up an old Finz file/etc.  So we don't blow away previous
			// mods done to the image.
			DatabaseFin.PrimaryImage.ImageMods = DatabaseFin.PrimaryImage.ImageMods.Concat(addedMods).ToList();
		}

		public async Task<Contour> DetectContour()
        {
			return await MLSupport.DetectContour(DatabaseFin.PrimaryImage.FinImage, DatabaseFin.PrimaryImage.ImageFilename);
        }

		/// <summary>
		/// Locks in trace after user cleanup, but before user is allowed to move feature points (tip, notch, etc)
		/// </summary>
		public void TraceFinalize()
		{
			if (Contour == null || !TraceLocked) //***006FC
				return;

			BackupContour = new Contour(Contour);

			// After even spacing and normalization fin height will be approx 600 units
			NormScale = Contour.NormalizeContour(); //***006CN

			if (Options.CurrentUserOptions.ContoursAreClosedLoop)
			{
				Contour = Contour.EvenlySpaceContourPoints2(3.0); //***006CN
			}
			else
			{
				Contour.RemoveKnots(3.0);
				Contour = Contour.EvenlySpaceContourPoints(3.0); //***006CN
			}

			Outline = new Outline(Contour, Database.CatalogScheme.FeatureSetType, Bitmap, NormScale); //***008OL

			LoadCoordinateFeaturePoints();

			TraceFinalized = true; // ***006PD moved from beginning of function

			if (Options.CurrentUserOptions.MatchingScheme == MatchingSchemeType.MachineLearning)
			{
				UpdateDatabaseFin();
				DatabaseFin.PrimaryImage.Embedding = MLSupport.GetImageEmbedding(DatabaseFin.PrimaryImage);
			}

			TraceTool = TraceToolType.MoveFeature;
		}

		public void SetTraceStepOutline(double spaceBetweenPoints)
        {
			if (TraceFinalized)
			{
				if (BackupContour != null)
				{
					Contour = BackupContour;
				}
				else
				{
					var tempContour = new Contour(Contour, true);

					double spacing = tempContour.GetTotalDistanceAlongContour() / 200.0;
					if (spacing < spaceBetweenPoints)
						spacing = spaceBetweenPoints;

					if (Options.CurrentUserOptions.ContoursAreClosedLoop)
						Contour = tempContour.EvenlySpaceContourPoints2(spacing);
					else
						Contour = tempContour.EvenlySpaceContourPoints(spacing);
				}

				Outline = null;

				TraceLocked = false;
				TraceFinalized = false;
				TraceTool = TraceToolType.MovePoint;
			}
		}

		public void SetTraceStepIdentifyFeatures()
        {
			if (!TraceFinalized)
			{
				TraceLocked = true;
				TraceFinalize();
				TraceTool = TraceToolType.MoveFeature;
			}
		}

		private void LoadFin(DatabaseFin fin)
		{
			WindowTitle = fin.IDCode;

			if (!string.IsNullOrEmpty(fin.FinFilename))
				WindowTitle += " - " + Path.GetFileName(fin.FinFilename);

			// TODO: Hack for HiDPI
			fin.PrimaryImage.FinImage.SetResolution(96, 96);

			DatabaseFin = fin;

			Bitmap = fin.PrimaryImage.FinImage ?? fin.PrimaryImage.OriginalFinImage;

			if (fin.PrimaryImage.FinOutline == null || fin.PrimaryImage.FinOutline.ChainPoints == null)
				Contour = null;
			else
				Contour = new Contour(fin.PrimaryImage.FinOutline, fin.PrimaryImage.FinOutline.Scale);

			Outline = fin.PrimaryImage.FinOutline;

			LoadCoordinateFeaturePoints();

			if (Categories == null)
				Categories = new ObservableCollection<Category>();

			if (!Categories.Any(c => c.Name == fin?.DamageCategory))
			{
				Categories.Add(new Category
				{
					Name = fin?.DamageCategory
				});
			}

			// ImageLocked = true;
			TraceLocked = true;
			TraceFinalized = true;

			NormScale = (float)fin.PrimaryImage.FinOutline.Scale;

			TraceTool = TraceToolType.Hand;
			ZoomRatio = 1.0f;
			ZoomValues = new List<double>();
		}

		private void AttachEvents()
        {
			UndoItems = new ObservableStack<Modification>();
			UndoItems.CollectionChanged += UndoItemsCollectionChanged;
			RedoItems = new ObservableStack<Modification>();
			RedoItems.CollectionChanged += RedoItemsCollectionChanged;
		}

		private void UndoItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
			RaisePropertyChanged("UndoEnabled");
		}

		private void RedoItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RaisePropertyChanged("RedoEnabled");
		}
	}
}
