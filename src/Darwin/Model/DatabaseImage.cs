using Darwin.Database;
using Darwin.Helpers;
using Darwin.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;

namespace Darwin.Model
{
    public class DatabaseImage : BaseEntity
    {
        public int Version { get; set; }

        public static int CurrentVersion = 2;

        private const int CropPadding = 10;

        public Bitmap FinImage { get; set; } // Modified image
        public Bitmap OriginalFinImage { get; set; }
        public Bitmap CropImage { get; set; }
        public Contour Contour { get; set; }
        public Contour ClippedContour { get; set; }

        public string CropImageFilename { get; set; }

        private Outline _finOutline;
        public Outline FinOutline //  008OL
        {
            get => _finOutline;
            set
            {
                _finOutline = value;
                RaisePropertyChanged("FinOutline");
            }
        }

        public List<ImageMod> ImageMods;    //  1.8 - for list of image modifications

        private DateTime? _dateOfSighting;
        public DateTime? DateOfSighting
        {
            get => _dateOfSighting;
            set
            {
                _dateOfSighting = value;
                RaisePropertyChanged("DateOfSighting");
                FieldsChanged = true;
            }
        }

        private string _rollAndFrame;
        public string RollAndFrame
        {
            get => _rollAndFrame;
            set
            {
                _rollAndFrame = value;
                RaisePropertyChanged("RollAndFrame");
                FieldsChanged = true;
            }
        }

        private string _locationCode;
        public string LocationCode
        {
            get => _locationCode;
            set
            {
                _locationCode = value;
                RaisePropertyChanged("LocationCode");
                FieldsChanged = true;
            }
        }

        private string _shortDescription;
        public string ShortDescription
        {
            get => _shortDescription;
            set
            {
                _shortDescription = value;
                RaisePropertyChanged("ShortDescription");
                FieldsChanged = true;
            }
        }

        private string _imageFilename; //  001DB
        public string ImageFilename
        {
            get => _imageFilename;
            set
            {
                _imageFilename = value;
                RaisePropertyChanged("ImageFilename");
                RaisePropertyChanged("ImageFilenameUri");
            }
        }

        public string ImageFilenameUri
        {
            get
            {
                if (string.IsNullOrEmpty(ImageFilename))
                    return AppSettings.MissingImageUri;

                return Path.Combine(Options.CurrentUserOptions.CurrentCatalogPath, ImageFilename);
            }
        }

        private string _originalImageFilename;
        public string OriginalImageFilename
        {
            get => _originalImageFilename;
            set
            {
                _originalImageFilename = value;
                RaisePropertyChanged("OriginalImageFilename");
                RaisePropertyChanged("OriginalImageFilenameUri");
            }
        }

        public string OriginalImageFilenameUri
        {
            get
            {
                if (string.IsNullOrEmpty(OriginalImageFilename))
                    return AppSettings.MissingImageUri;

                return Path.Combine(Options.CurrentUserOptions.CurrentCatalogPath, OriginalImageFilename);
            }
        }

        private int _orderId;
        public int OrderId
        {
            get => _orderId;
            set
            {
                _orderId = value;
                RaisePropertyChanged("Order");
            }
        }

        private GeoLocation _geoLocation;
        public GeoLocation GeoLocation
        {
            get => _geoLocation;
            set
            {
                _geoLocation = value;
                RaisePropertyChanged("GeoLocation");
            }
        }

        public long IndividualId { get; set; }

        public DatabaseImage()
        {

        }

        public DatabaseImage(string imageFilename)
        {
            var img = System.Drawing.Image.FromFile(imageFilename);

            FinImage = new Bitmap(img);
            // TODO: Hack for HiDPI -- this should be more intelligent.
            FinImage.SetResolution(96, 96);

            OriginalFinImage = new Bitmap(FinImage);
            ImageFilename = OriginalImageFilename = imageFilename;

            DateOfSighting = ImageDataHelper.GetImageTakenDateTime(imageFilename);
            GeoLocation = ImageDataHelper.GetImageLocation(imageFilename);
        }

        public DatabaseImage(DatabaseImage imageToCopy)
        {
            Version = imageToCopy.Version;

            _dateOfSighting = imageToCopy._dateOfSighting;
            _finOutline = new Outline(imageToCopy.FinOutline);
            _imageFilename = imageToCopy._imageFilename;
            _locationCode = imageToCopy._locationCode;
            _rollAndFrame = imageToCopy._rollAndFrame;
            _shortDescription = imageToCopy._shortDescription;
            _orderId = imageToCopy._orderId;

            if (imageToCopy.OriginalFinImage != null)
                OriginalFinImage = new Bitmap(imageToCopy.OriginalFinImage);

            if (imageToCopy.FinImage != null)
                FinImage = new Bitmap(imageToCopy.FinImage);

            OriginalImageFilename = imageToCopy.OriginalImageFilename;
            CropImageFilename = imageToCopy.CropImageFilename;
            
            ImageMods = new List<ImageMod>();

            if (imageToCopy.ImageMods != null)
            {
                foreach (var mod in imageToCopy.ImageMods)
                    ImageMods.Add(new ImageMod(mod));
            }

            if (imageToCopy.GeoLocation != null)
                _geoLocation = new GeoLocation(imageToCopy.GeoLocation.Latitude, imageToCopy.GeoLocation.Longitude);
        }

        public DatabaseImage(
            string imageFilename,
            Outline outline,
            DateTime? dateOfSighting,
            string rollAndFrame,
            string locationCode,
            string shortDescription)
        {
            ImageFilename = imageFilename; //  001DB
            FinOutline = new Outline(outline); //  006DF,008OL
            DateOfSighting = dateOfSighting;
            RollAndFrame = rollAndFrame;
            LocationCode = locationCode;
            ShortDescription = shortDescription;

            FinImage = null; //  1.5
        }

        public void PrepareDisplay()
        {
            CheckVersionAndUpgrade();
            LoadContour();
        }

        public void CheckVersionAndUpgrade()
        {
            if (Version < CurrentVersion)
            {
                UpdateDatabaseImageFromImageFile(Options.CurrentUserOptions.CurrentCatalogPath, this);
                Version = CurrentVersion;
            }
        }

        public void LoadContour()
        {
            if (Contour == null)
            {
                if (FinOutline != null && FinOutline.ChainPoints != null)
                {
                    Contour = new Contour(FinOutline.ChainPoints, FinOutline.Scale);

                    ClippedContour = new Contour(Contour);
                    ClippedContour.ClipToBounds();
                }
            }
        }

        public void GenerateCropImage()
        {
            if (FinImage == null && OriginalFinImage == null)
                throw new Exception("Main image is null, can't create a crop image.");

            if (FinOutline == null || FinOutline.ChainPoints == null)
                throw new Exception("Missing outline/contour, can't create a crop image.");

            Bitmap imageToUse = FinImage ?? OriginalFinImage;

            int xMin = (int)Math.Floor((FinOutline.ChainPoints.MinX() - CropPadding) / FinOutline.Scale);
            int yMin = (int)Math.Floor((FinOutline.ChainPoints.MinY() - CropPadding) / FinOutline.Scale);
            int xMax = (int)Math.Ceiling((FinOutline.ChainPoints.MaxX() + CropPadding) / FinOutline.Scale);
            int yMax = (int)Math.Ceiling((FinOutline.ChainPoints.MaxY() + CropPadding) / FinOutline.Scale);

            ConstraintHelper.ConstrainInt(ref xMin, 0, imageToUse.Width);
            ConstraintHelper.ConstrainInt(ref xMax, 0, imageToUse.Width);
            ConstraintHelper.ConstrainInt(ref yMin, 0, imageToUse.Height);
            ConstraintHelper.ConstrainInt(ref yMax, 0, imageToUse.Height);

            // Figure out the ratio
            var resizeRatioX = (float)AppSettings.FinzThumbnailMaxDim / (xMax - xMin);
            var resizeRatioY = (float)AppSettings.FinzThumbnailMaxDim / (yMax - yMin);

            if (resizeRatioX > resizeRatioY)
            {
                // We're X constrained, so expand the X
                var extra = ((yMax - yMin) - (xMax - xMin)) * ((float)AppSettings.FinzThumbnailMaxDim / AppSettings.FinzThumbnailMaxDim);
                xMin -= (int)Math.Round(extra / 2);
                xMax += (int)Math.Round(extra / 2);

                if (xMin < 0)
                {
                    xMax += (0 - xMin);
                    xMin = 0;
                }

                if (xMax > imageToUse.Width)
                {
                    xMin -= xMax - imageToUse.Width;
                    xMax = imageToUse.Width;
                }

                if (xMin < 0)
                    xMin = 0;
                if (xMax > imageToUse.Width)
                    xMax = imageToUse.Width;
            }
            else
            {
                // We're Y constrained, so expand the Y
                var extra = ((xMax - xMin) - (yMax - yMin)) * ((float)AppSettings.FinzThumbnailMaxDim / AppSettings.FinzThumbnailMaxDim);
                yMin -= (int)Math.Round(extra / 2);
                yMax += (int)Math.Round(extra / 2);

                if (yMin < 0)
                {
                    yMax += (0 - yMin);
                    yMin = 0;
                }

                if (yMax > imageToUse.Height)
                {
                    yMin -= yMax - imageToUse.Height;
                    yMax = imageToUse.Height;
                }

                if (yMin < 0)
                    yMin = 0;
                if (yMax > imageToUse.Height)
                    yMax = imageToUse.Height;
            }

            CropImage = BitmapHelper.CropBitmap(imageToUse,
                xMin, yMin,
                xMax, yMax);
        }

        public static ObservableCollection<DatabaseImage> CopyDatabaseImageList(ObservableCollection<DatabaseImage> imagesToCopy)
        {
            if (imagesToCopy == null)
                return null;

            var result = new ObservableCollection<DatabaseImage>();

            foreach (var image in imagesToCopy)
                result.Add(new DatabaseImage(image));

            return result;
        }

        public static void PrepareDatabaseImagesDisplay(ObservableCollection<DatabaseImage> images)
        {
            if (images == null)
                return;

            foreach (var image in images)
                image.PrepareDisplay();
        }

        public static void FullyLoadDatabaseImages(ObservableCollection<DatabaseImage> images)
        {
            if (images == null)
                return;

            foreach (var image in images)
                FullyLoadDatabaseImage(image);
        }

        public static void UnloadDatabaseImages(ObservableCollection<DatabaseImage> images)
        {
            if (images == null)
                return;

            foreach (var image in images)
                UnloadDatabaseImage(image);
        }

        public static void UpdateDatabaseImageFromImageFile(string basePath, DatabaseImage image)
        {
            // TODO:
            // Probably not the best "flag" to look at to figure out if this an old image.
            // We're saving OriginalImageFilename in the database for newer fins
            if (string.IsNullOrEmpty(image.OriginalImageFilename))
            {
                List<ImageMod> imageMods;
                bool thumbOnly;
                string originalFilename;
                float normScale;

                string fullFilename = Path.Combine(basePath, image.ImageFilename);

                PngHelper.ParsePngText(fullFilename, out normScale, out imageMods, out thumbOnly, out originalFilename);

                image.ImageMods = imageMods;
                image.FinOutline.Scale = normScale;

                if (!string.IsNullOrEmpty(originalFilename))
                {
                    image.OriginalImageFilename = originalFilename;
                }
                // TODO: Save these changes back to the database?
            }
        }

        /// <summary>
        /// We're modifying the image itself here to fully load the images into bitmaps.
        /// </summary>
        /// <param name="image"></param>
        public static void FullyLoadDatabaseImage(DatabaseImage image)
        {
            if (image == null)
                return;

            if (!string.IsNullOrEmpty(image.ImageFilename))
            {
                UpdateDatabaseImageFromImageFile(Options.CurrentUserOptions.CurrentCatalogPath, image);

                string fullImageFilename = Path.Combine(Options.CurrentUserOptions.CurrentCatalogPath,
                    (string.IsNullOrEmpty(image.OriginalImageFilename)) ? image.ImageFilename : image.OriginalImageFilename);

                if (File.Exists(fullImageFilename))
                {
                    var img = Image.FromFile(fullImageFilename);

                    var bitmap = new Bitmap(img);

                    // TODO: Hack for HiDPI -- this should probably be more intelligent.
                    bitmap.SetResolution(96, 96);

                    image.OriginalFinImage = new Bitmap(bitmap);

                    // TODO: Maybe refactor this so we're not doing it every time?
                    if (image.ImageMods != null && image.ImageMods.Count > 0)
                    {
                        bitmap = ModificationHelper.ApplyImageModificationsToOriginal(bitmap, image.ImageMods);

                        // TODO: HiDPI hack
                        bitmap.SetResolution(96, 96);
                    }

                    image.FinImage = bitmap;
                }
            }

            image.LoadContour();
        }

        public static void UnloadDatabaseImage(DatabaseImage image)
        {
            if (image != null)
            {
                image.FinImage = null;
                image.OriginalFinImage = null;
                image.Contour = null;
            }
        }
    }
}