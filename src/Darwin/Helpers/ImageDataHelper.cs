using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Darwin.Helpers
{
    public static class ImageDataHelper
    {
        /// <summary>
        /// Tries to get the time an image was taken.  Tries reading Exif data,
        /// but will fall back to the file creation time if that's not found.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>DateTime the image was taken</returns>
        public static DateTime? GetImageTakenDateTime(string filename)
        {
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                return null;

            var directories = ImageMetadataReader.ReadMetadata(filename);

            foreach (var directory in directories)
                foreach (var tag in directory.Tags)
                    Trace.WriteLine($"{directory.Name} - {tag.Name} = {tag.Description}");

            var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

            var tagDateTimeOriginal = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

            if (!string.IsNullOrEmpty(tagDateTimeOriginal))
            {
                DateTime parsedDate;
                if (DateTime.TryParseExact(tagDateTimeOriginal, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                    return parsedDate;
            }

            var tagDateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTime);

            if (!string.IsNullOrEmpty(tagDateTime))
            {
                DateTime parsedDate;
                if (DateTime.TryParseExact(tagDateTime, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                    return parsedDate;
            }

            return File.GetCreationTime(filename);
        }

        public static Model.GeoLocation GetImageLocation(string filename)
        {
            var gps = ImageMetadataReader.ReadMetadata(filename)
                             .OfType<GpsDirectory>()
                             .FirstOrDefault();

            if (gps == null)
                return null;

            var location = gps.GetGeoLocation();

            if (location == null)
                return null;

            return new Model.GeoLocation(location.Latitude, location.Longitude);
        }
    }
}
