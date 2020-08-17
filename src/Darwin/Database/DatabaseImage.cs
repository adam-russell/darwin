using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;

namespace Darwin.Database
{
    public class DatabaseImage : BaseEntity
    {
        public Bitmap FinImage { get; set; } // Modified image
        public Bitmap OriginalFinImage { get; set; }
        public Contour Contour { get; set; }

        public string CropImageFilename { get; set; }
        public string OriginalImageFilename { get; set; } //  1.8 - filename of original unmodified image

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

        private string _dateOfSighting;
        public string DateOfSighting
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

        public long IndividualId { get; set; }

        public DatabaseImage()
        {

        }

        public DatabaseImage(DatabaseImage imageToCopy)
        {
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
        }

        public DatabaseImage(
            string imageFilename,
            Outline outline,
            string dateOfSighting,
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

        public static ObservableCollection<DatabaseImage> CopyDatabaseImageList(ObservableCollection<DatabaseImage> imagesToCopy)
        {
            if (imagesToCopy == null)
                return null;

            var result = new ObservableCollection<DatabaseImage>();

            foreach (var image in imagesToCopy)
                result.Add(new DatabaseImage(image));

            return result;
        }

        public static void FullyLoadDatabaseImages(ObservableCollection<DatabaseImage> images)
        {
            if (images == null)
                return;

            foreach (var image in images)
                CatalogSupport.FullyLoadDatabaseImage(image);
        }

        public static void UnloadDatabaseImages(ObservableCollection<DatabaseImage> images)
        {
            if (images == null)
                return;

            foreach (var image in images)
                CatalogSupport.UnloadDatabaseImage(image);
        }
    }
}