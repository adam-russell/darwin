//                                            *
//   file: DatabaseFin.h
//
// author: Adam Russell
//
//   mods: J H Stewman (9/27/2005)
//         -- code to determine whether particular databasefin
//            is being used for an UNKNOWN or is being created as it
//            is read from the database
//            ASSUMPTION: if image filename contains any slashes it is
//            presumed to be an UNKNOWN
//

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

//
// 0.3.1-DB: Addendum : New DatabaseFin Data structure
// [Data Position] (4 Bytes)
// [Image Filename] (char[255]) **Delimited by '\n'
// [Number of Contour Points] (4 bytes)
// [Contour Points ...] (Number * (int) bytes)
// [Thumbnail Pixmap] (25*25)
// [Short Description] (char[255]) **Delimited by '\n'
//
// Darwin_0.3.8 - DB version 2: Addendum : New DatabaseFin Data structure
// [Data Position] (4 Bytes) -- or "DFIN" as hex number in saved traced fin files
// [Image Filename] (char[255]) **Delimited by '\n'
// [Number of FloatContour Points] (4 bytes)
// [FloatContour Points ...] (Number*2*sizeof(float) bytes)
// [Feature Point Positions] (5*sizeof(int) bytes)
// [Thumbnail Pixmap] (25*25 bytes)
// [Short Description] (char[255]) **Delimited by '\n'
//
// Darwin_1.4 - DB version 4: Addendum : New DatabaseFin Data structure
// this adds fields for tracking changes to image while tracing fin
// [Data Position] (4 Bytes) -- or "DFIN" as hex number in saved traced fin files
// [Image Filename] (char[255]) **Delimited by '\n'
// [Number of FloatContour Points] (4 bytes)
// [FloatContour Points ...] (Number*2*sizeof(float) bytes)
// [Feature Point Positions] (5*sizeof(int) bytes)
// [Thumbnail Pixmap] (25*25 bytes)
// [Is Left Side] '1' or '0'
// [Is Flipped Image] '1' or '0'
// [Clipping bounds xmin,ymin,xmax,ymax] (4 * sizeof(double))
// [Normalizing Scale] (sizeof(double)
// [Alternate (blind) ID] (5 chars) **Delimited by '\n'
// [Short Description] (char[255]) **Delimited by '\n'
//

using Darwin.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace Darwin.Database
{
    public class DatabaseFin : BaseEntity
    {
        private string _IDCode;
        private string _name;
        private string _damageCategory;
        private string _thumbnailFileName;

        private string _finFilename;
        public string FinFilename  // 1.6 - for name of fin file if fin saved outside DB
        {
            get => _finFilename;
            set
            {
                _finFilename = value;
                RaisePropertyChanged("FinFilename");
            }
        }

        public string FinFilenameOnly
        {
            get
            {
                if (_finFilename == null)
                    return null;

                return Path.GetFileName(_finFilename);
            }
        }

        public string IDCode
        {
            get => _IDCode;
            set
            {
                _IDCode = value;
                RaisePropertyChanged("IDCode");
                FieldsChanged = true;
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
                FieldsChanged = true;
            }
        }

        public string DamageCategory
        {
            get => _damageCategory;
            set
            {
                _damageCategory = value;
                RaisePropertyChanged("DamageCategory");
                FieldsChanged = true;
            }
        }

        public string ThumbnailFilename
        {
            get => _thumbnailFileName;
            set
            {
                _thumbnailFileName = value;
                RaisePropertyChanged("ThumbnailFilename");
                RaisePropertyChanged("ThumbnailFilenameUri");
            }
        }

        private List<DatabaseImage> _images;
        public List<DatabaseImage> Images
        {
            get => _images;
            set
            {
                _images = value;
                RaisePropertyChanged("Images");
            }
        }

        private void CheckImagesCollection()
        {
            if (_images == null)
                _images = new List<DatabaseImage>();

            if (_images.Count < 1)
                _images.Add(new DatabaseImage());
        }
        public DatabaseImage PrimaryImage
        {
            get
            {
                CheckImagesCollection();

                // Relies on this list being sorted by order
                return _images[0];
            }
            //set
            //{
            //    CheckImagesCollection();

            //    _images[0] = value;

            //    RaisePropertyChanged("PrimaryImage");
            //}
        }

        public string ThumbnailFilenameUri
        {
            get
            {
                if (string.IsNullOrEmpty(ThumbnailFilename))
                    return null;

                return Path.Combine(Options.CurrentUserOptions.CurrentCatalogPath, ThumbnailFilename);
            }
        }

        public DatabaseFin()
        {
        }

        //                                                **
        //
        // called from numerous places in ...
        //   TraceWindow.cxx, ModifyDatabaseWindow.cxx, and 
        //   NoMatchWindow.cxx
        //
        public DatabaseFin(
            string filename, //  001DB
            Outline outline, //  008OL
            string idcode,
            string name,
            string dateOfSighting,
            string rollAndFrame,
            string locationCode,
            string damageCategory,
            string shortDescription
        )
        {
            FinFilename = string.Empty; //  1.6

            IDCode = idcode;
            Name = name;
            DamageCategory = damageCategory;

            _images = new List<DatabaseImage>();
            _images.Add(new DatabaseImage(
                filename,
                outline,
                dateOfSighting,
                rollAndFrame,
                locationCode,
                shortDescription));

            //  1.5 - need some way to CATCH error thrown when image file
            //         does not exist or is unsupported type  --
            //         program now crashes when database image is misplaced or misnamed

            //OriginalFinImage = new Bitmap(ImageFilename); //  001DB

            FieldsChanged = false;
        }

        /*
         *             DatabaseFin fin = new DatabaseFin(id,
                individual.idcode,
                individual.name,
                damagecategory.Name,
                images);*/

        //  1.99 - new constructor used by SQLite database code
        // Added. Called in Database::getFin().
        public DatabaseFin(
            long id,
			string idcode,
			string name,
			string damageCategory,
			List<DatabaseImage> images
		)
        {
            ID = id;
            IDCode = idcode;
            Name = name;
            DamageCategory = damageCategory;

            //Scale = 1.0; //  1.4
            //FinImage = null; //  1.5
            FinFilename = string.Empty; //  1.6

            Images = images;

            FieldsChanged = false;
        }

        public DatabaseFin(DatabaseFin fin)
        {
            ID = fin.ID; //  001DB
            IDCode = fin.IDCode;
            Name = fin.Name;

            Images = DatabaseImage.CopyDatabaseImageList(fin.Images);

            DamageCategory = fin.DamageCategory;
            FinFilename = fin.FinFilename;

            FieldsChanged = false;
        }

        public void SaveSightingData(string filename)
        {
            using (StreamWriter writer = File.AppendText(filename))
            {
                writer.WriteLine(
                    IDCode?.StripCRLFTab() + "\t" +
                    Name?.StripCRLFTab() + "\t" +
                    PrimaryImage.DateOfSighting?.StripCRLFTab() + "\t" +
                    PrimaryImage.RollAndFrame?.StripCRLFTab() + "\t" +
                    PrimaryImage.LocationCode?.StripCRLFTab() + "\t" +
                    DamageCategory?.StripCRLFTab() + "\t" +
                    PrimaryImage.ShortDescription?.StripCRLFTab() + "\t" +
                    PrimaryImage.OriginalImageFilename + "\t" +
                    PrimaryImage.ImageFilename);
            }
        }
    }
}
