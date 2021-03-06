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

using CsvHelper;
using Darwin.Database;
using Darwin.Extensions;
using Darwin.Helpers;
using Darwin.ImageProcessing;
using Darwin.Matching;
using Darwin.ML;
using Darwin.ML.Services;
using Darwin.Model;
using Darwin.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Helpers
{
    public static class MLSupport
    {
        public const string ImagesDirectoryName = "images";
        public const int FeatureImageWidth = 224;
        public const int FeatureImageHeight = 224;
        public const string FeatureCsvFilename = "darwin_coordinates.csv";

        //public const int MaskImageWidth = 224;
        //public const int MaskImageHeight = 224;
        public const string MaskCsvFilename = "darwin_masks.csv";

        public const string ClassificationCsvFilename = "darwin_classification.csv";

        public static MLFeatureImage ConvertDatabaseFinToMLFeatureImage(Bitmap image, FloatContour contour, double scale)
        {
            int xMin = (int)Math.Floor(contour.MinX() / scale);
            int yMin = (int)Math.Floor(contour.MinY() / scale);
            int xMax = (int)Math.Ceiling(contour.MaxX() / scale);
            int yMax = (int)Math.Ceiling(contour.MaxY() / scale);

            // Figure out the ratio
            var resizeRatioX = (float)FeatureImageWidth / (xMax - xMin);
            var resizeRatioY = (float)FeatureImageHeight / (yMax - yMin);

            if (resizeRatioX > resizeRatioY)
            {
                // We're X constrained, so expand the X
                var extra = ((yMax - yMin) - (xMax - xMin)) * ((float)FeatureImageWidth / FeatureImageHeight);
                xMin -= (int)Math.Round(extra / 2);
                xMax += (int)Math.Round(extra / 2);

                if (xMin < 0)
                {
                    xMax += (0 - xMin);
                    xMin = 0;
                }

                if (xMax > image.Width)
                {
                    xMin -= xMax - image.Width;
                    xMax = image.Width;
                }

                if (xMin < 0)
                    xMin = 0;
                if (xMax > image.Width)
                    xMax = image.Width;
            }
            else
            {
                // We're Y constrained, so expand the Y
                var extra = ((xMax - xMin) - (yMax - yMin)) * ((float)FeatureImageHeight / FeatureImageWidth);
                yMin -= (int)Math.Round(extra / 2);
                yMax += (int)Math.Round(extra / 2);

                if (yMin < 0)
                {
                    yMax += (0 - yMin);
                    yMin = 0;
                }

                if (yMax > image.Height)
                {
                    yMin -= yMax - image.Height;
                    yMax = image.Height;
                }

                if (yMin < 0)
                    yMin = 0;
                if (yMax > image.Height)
                    yMax = image.Height;
            }

            var workingImage = BitmapHelper.CropBitmap(image,
                xMin, yMin,
                xMax, yMax);

            // We've hopefully already corrected for the aspect ratio above
            workingImage = BitmapHelper.ResizeBitmap(workingImage, FeatureImageWidth, FeatureImageHeight);

            float xRatio = (float)FeatureImageWidth / (xMax - xMin);
            float yRatio = (float)FeatureImageHeight / (yMax - yMin);

            return new MLFeatureImage
            {
                Image = workingImage,
                XMin = xMin,
                XMax = xMax,
                YMin = yMin,
                YMax = yMax,
                XRatio = xRatio,
                YRatio = yRatio
            };
        }

        public static void SaveFeatureDatasetImages(string datasetDirectory, DarwinDatabase database)
        {
            if (datasetDirectory == null)
                throw new ArgumentNullException(nameof(datasetDirectory));

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (!Directory.Exists(datasetDirectory))
                throw new ArgumentOutOfRangeException(nameof(datasetDirectory));

            Trace.WriteLine("Starting dataset export...");

            string fullImagesDirectory = Path.Combine(datasetDirectory, ImagesDirectoryName);

            Directory.CreateDirectory(fullImagesDirectory);

            var csvRecords = new List<MLFeatureCsvRecord>();

            int individualNum = 1;
            foreach (var dbFin in database.AllFins)
            { 
                var fin = CatalogSupport.FullyLoadFin(dbFin);

                if (!string.IsNullOrEmpty(fin?.IDCode))
                    Trace.WriteLine("Exporting " + fin.IDCode);

                if (fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints == null ||
                    fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints.Count < 1 ||
                    !fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints.ContainsKey(Features.FeaturePointType.Eye))
                {
                    // If we don't have the features we need, skip to the next one
                    continue;
                }

                var mlImage = ConvertDatabaseFinToMLFeatureImage(fin.PrimaryImage.FinImage, fin.PrimaryImage.FinOutline.ChainPoints, fin.PrimaryImage.FinOutline.Scale);

                string imageFilename = individualNum.ToString().PadLeft(6, '0') + ".jpg";

                mlImage.Image.Save(Path.Combine(fullImagesDirectory, imageFilename), ImageFormat.Jpeg);

                csvRecords.Add(new MLFeatureCsvRecord
                {
                    image = imageFilename,
                    eye_x = mlImage.XRatio * (float)(fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints[Features.FeaturePointType.Eye].Coordinate.X / fin.PrimaryImage.FinOutline.Scale - mlImage.XMin),
                    eye_y = mlImage.YRatio * (float)(fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints[Features.FeaturePointType.Eye].Coordinate.Y / fin.PrimaryImage.FinOutline.Scale - mlImage.YMin),
                    nasalfold_x = mlImage.XRatio * (float)(fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints[Features.FeaturePointType.NasalLateralCommissure].Coordinate.X / fin.PrimaryImage.FinOutline.Scale - mlImage.XMin),
                    nasalfold_y = mlImage.YRatio * (float)(fin.PrimaryImage.FinOutline.FeatureSet.CoordinateFeaturePoints[Features.FeaturePointType.NasalLateralCommissure].Coordinate.Y / fin.PrimaryImage.FinOutline.Scale - mlImage.YMin)
                });

                individualNum += 1;
            }

            using (var writer = new StreamWriter(Path.Combine(datasetDirectory, FeatureCsvFilename), false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(csvRecords);
            }

            Trace.WriteLine("done.");
        }

        public static void SaveSegmentationMaskDatasetImages(string datasetDirectory, DarwinDatabase database, int maskImageWidth = 512, int maskImageHeight = 512)
        {
            if (datasetDirectory == null)
                throw new ArgumentNullException(nameof(datasetDirectory));

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (!Directory.Exists(datasetDirectory))
                throw new ArgumentOutOfRangeException(nameof(datasetDirectory));

            Trace.WriteLine("Starting dataset export...");

            string fullImagesDirectory = Path.Combine(datasetDirectory, ImagesDirectoryName);

            Directory.CreateDirectory(fullImagesDirectory);

            var csvRecords = new List<MLMaskCsvRecord>();

            int imageNum = 1;

            foreach (var dbFin in database.AllFins)
            {
                var fin = CatalogSupport.FullyLoadFin(dbFin);

                if (!string.IsNullOrEmpty(fin?.IDCode))
                    Trace.WriteLine("Exporting " + fin.IDCode);

                foreach (var image in fin.Images)
                {
                    image.Contour.ApplyScale();

                    double ratio;
                    var mlImage = BitmapHelper.ResizeKeepAspectRatio(image.FinImage, maskImageWidth, maskImageHeight, out ratio);

                    //float xRatio = (float)image.FinImage.Width / maskImageWidth;
                    //float yRatio = (float)image.FinImage.Height / maskImageHeight;
                    float contourRatio = (float)(1 / ratio);
                    image.Contour.ApplyNonProportionalScale(contourRatio, contourRatio);

                    var mask = BitmapHelper.CreateMaskImageFromContour(mlImage, image.Contour);
                    mask = BitmapHelper.ResizeKeepAspectRatio(mask, maskImageWidth, maskImageHeight);

                    // Now pad the images so they're the "full" desired size
                    mlImage = BitmapHelper.Pad(mlImage, maskImageWidth, maskImageHeight, Color.Black);
                    mask = BitmapHelper.Pad(mask, maskImageWidth, maskImageHeight, Color.Black);

                    string imageFilename = imageNum.ToString().PadLeft(6, '0') + ".jpg";
                    string maskFilename = imageNum.ToString().PadLeft(6, '0') + "_mask.png";

                    mlImage.SaveAsCompressedJpg(Path.Combine(fullImagesDirectory, imageFilename));

                    // Note we're saving as a PNG to make sure we don't get compression artificacts -- we
                    // want the mask to be strictly white/black
                    mask.SaveAsCompressedPng(Path.Combine(fullImagesDirectory, maskFilename));

                    csvRecords.Add(new MLMaskCsvRecord
                    {
                        image = imageFilename,
                        mask_image = maskFilename,
                        id_code = fin.IDCode.ToLowerInvariant()
                    });

                    imageNum += 1;
                }

                CatalogSupport.UnloadFin(fin);
                fin = null;

                // Wait for the garbage collector after we just dereferenced some objects,
                // otherwise we end up maxing RAM if we have a large database.
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            using (var writer = new StreamWriter(Path.Combine(datasetDirectory, MaskCsvFilename), false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(csvRecords);
            }

            Trace.WriteLine("done.");
        }

        public static (Bitmap, Bitmap) CreateClassificationImageAndMask(DatabaseImage image, int datasetImageWidth = 299, int datasetImageHeight = 299, bool generateMask = false)
        {
            int xMin, xMax, yMin, yMax;
            var cropImage = image.CreateCropImage(out xMin, out yMin, out xMax, out yMax, image.Contour);

            double ratio;
            var mlImage = BitmapHelper.ResizeKeepAspectRatio(cropImage, datasetImageWidth, datasetImageHeight, out ratio);

            image.Contour.Crop(xMin, yMin, xMax, yMax);

            float contourRatio = (float)(1 / ratio);
            image.Contour.ApplyNonProportionalScale(contourRatio, contourRatio);

            Bitmap mask = null;

            if (generateMask)
            {
                mask = BitmapHelper.CreateMaskImageFromContour(mlImage, image.Contour);
                mask = BitmapHelper.Pad(mask, datasetImageWidth, datasetImageHeight, Color.Black);
            }
            //mask = BitmapHelper.ResizeKeepAspectRatio(mask, datasetImageWidth, datasetImageHeight);

            // Now pad the images so they're the "full" desired size
            mlImage = BitmapHelper.Pad(mlImage, datasetImageWidth, datasetImageHeight, Color.Black);

            return (mlImage, mask);
        }

        public static async Task SaveClassificationDatasetImages(string datasetDirectory, DarwinDatabase database, int datasetImageWidth = 299, int datasetImageHeight = 299, bool remask = false)
        {
            if (datasetDirectory == null)
                throw new ArgumentNullException(nameof(datasetDirectory));

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (!Directory.Exists(datasetDirectory))
                throw new ArgumentOutOfRangeException(nameof(datasetDirectory));

            Trace.WriteLine("Starting dataset export...");

            string fullImagesDirectory = Path.Combine(datasetDirectory, ImagesDirectoryName);

            Directory.CreateDirectory(fullImagesDirectory);

            var csvRecords = new List<MLMaskCsvRecord>();

            int imageNum = 1;

            foreach (var dbFin in database.AllFins)
            {
                var fin = CatalogSupport.FullyLoadFin(dbFin);

                if (!string.IsNullOrEmpty(fin?.IDCode))
                    Trace.WriteLine("Exporting " + fin.IDCode);

                foreach (var image in fin.Images)
                {
                    if (remask)
                    {
                        var backupContour = image.Contour;

                        int tryNum = 0;
                        while (tryNum < AppSettings.NumRetries)
                        {
                            try
                            {
                                image.Contour = await DetectContour(image.FinImage, image.ImageFilename);

                                if (image.Contour == null || image.Contour.Length < 1)
                                    throw new Exception("Empty contour!");

                                goto GoodContour;
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(ex);
                                tryNum += 1;
                                Thread.Sleep(AppSettings.RetrySleepMilliseconds);
                            }
                        }

                        Trace.WriteLine("Failed to find contours automatically, falling back to stored contour.");
                        // We didn't find a contour, fall back to the one that's on the already.
                        image.Contour = backupContour;
                        image.Contour.ApplyScale();
                    }
                    else
                    {
                        image.Contour.ApplyScale();
                    }

                    GoodContour:

                    //(var mlImage, var mask) = CreateClassificationImageAndMask(image, datasetImageWidth, datasetImageHeight);

                    string imageFilename = imageNum.ToString().PadLeft(6, '0') + ".jpg";
                    string maskFilename = imageNum.ToString().PadLeft(6, '0') + "_mask.png";

                    //mlImage.SaveAsCompressedJpg(Path.Combine(fullImagesDirectory, imageFilename));

                    // Note we're saving as a PNG to make sure we don't get compression artifacts -- we
                    // want the mask to be strictly white/black
                    //mask.SaveAsCompressedPng(Path.Combine(fullImagesDirectory, maskFilename));

                    csvRecords.Add(new MLMaskCsvRecord
                    {
                        image = imageFilename,
                        mask_image = maskFilename,
                        id_code = fin.IDCode.ToLowerInvariant()
                    });

                    imageNum += 1;
                }

                CatalogSupport.UnloadFin(fin);
                fin = null;

                // Wait for the garbage collector after we just dereferenced some objects,
                // otherwise we end up maxing RAM if we have a large database.
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            using (var writer = new StreamWriter(Path.Combine(datasetDirectory, ClassificationCsvFilename), false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(csvRecords);
            }

            Trace.WriteLine("done.");
        }

        public static float[] PredictCoordinates(Bitmap image, FloatContour chainPoints, double scale)
        {
            Trace.WriteLine("Predicting coordinates with " + AppSettings.MLModelFilename_BearFeatureIdentification + " model using Emgu.TF.Lite / TensorFlow Lite");

            var model = new MLModel(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AppSettings.MLModelFilename_BearFeatureIdentification));

            var mlImage = ConvertDatabaseFinToMLFeatureImage(image, chainPoints, scale);
            var directBmp = new DirectBitmap(mlImage.Image);

            //var floatArray = directBmp.ToScaledRGBFloatArray();

            // This must match what the model expects. E.g., this is what Keras on TF for Resnet uses:
            var floatArray = directBmp.ToScaledTensorFlowRGBPreprocessInput();
            
            var coordinates = model.Run(floatArray);

            Trace.WriteLine("Raw predicted coordinates:");

            // Scale/translate the coordinates back to the original image
            for (int i = 0; i < coordinates.Length; i++)
            {
                Trace.Write(" " + coordinates[i]);
                if (i % 2 == 0)
                    coordinates[i] = (float)((coordinates[i] / mlImage.XRatio + mlImage.XMin) * scale);
                else
                    coordinates[i] = (float)((coordinates[i] / mlImage.YRatio + mlImage.YMin) * scale);
            }

            Trace.WriteLine(" ");

            return coordinates;
        }

        public static string GetImageEmbedding(DatabaseImage image)
        {
            image.Contour.ApplyScale();
            (var imageToEncode, var mask) = CreateClassificationImageAndMask(image,
                AppSettings.MLModel_BearClassification_ImageDim,
                AppSettings.MLModel_BearClassification_ImageDim,
                AppSettings.MLModelFilename_BearClassification_UseMask);

            var model = new MLModel(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AppSettings.MLModelFilename_BearClassification));

            var directImageToEncode = new DirectBitmap(imageToEncode);
            var imageArray = directImageToEncode.ToScaledTorchRGBPreprocessInput();

            if (!AppSettings.MLModelFilename_BearClassification_UseMask)
            {
                var encodingImageOnly = model.Run(imageArray);
                return FloatHelper.ConvertToBase64String(encodingImageOnly);
            }

            var directMask = new DirectBitmap(mask);
            var maskArray = directMask.ToScaledTorchRGBPreprocessInput();

            // Create a 4 channel float array out of the image and the mask
            float[] combinedArray = new float[directImageToEncode.Width * directImageToEncode.Height * 4];

            int idx = 0;
            int imageIdx = 0;
            int maskArrayIdx = 0;
            for (int y = 0; y < directImageToEncode.Height; y++)
            {
                for (int x = 0; x < directImageToEncode.Width; x++)
                {
                    combinedArray[idx++] = imageArray[imageIdx++];
                    combinedArray[idx++] = imageArray[imageIdx++];
                    combinedArray[idx++] = imageArray[imageIdx++];
                    combinedArray[idx++] = maskArray[maskArrayIdx];
                    maskArrayIdx += 3;
                }
            }


            var encodingImageAndMask = model.Run(combinedArray);

            return FloatHelper.ConvertToBase64String(encodingImageAndMask);
        }

        public static async Task<Contour> DetectContour(Bitmap bitmap, string filename)
        {
            double ratio;
            var mlImage = BitmapHelper.ResizeKeepAspectRatio(bitmap, AppSettings.DefaultMaskImageWidth, AppSettings.DefaultMaskImageHeight, out ratio);
            int resizedWidth = mlImage.Width;
            int resizedHeight = mlImage.Height;
            mlImage = BitmapHelper.Pad(mlImage, AppSettings.DefaultMaskImageWidth, AppSettings.DefaultMaskImageHeight, Color.Black);

            var mlService = new MLService();

            Trace.WriteLine("Sending image to service to find contours...");
            var rawContour = await mlService.GetRawContourAsync(mlImage, filename);

            if (rawContour == null)
                return null;

            Trace.WriteLine("Received response with contour.");

            int xOffset = -1 * ((AppSettings.DefaultMaskImageWidth - resizedWidth) / 2);
            int yOffset = -1 * ((AppSettings.DefaultMaskImageHeight - resizedHeight) / 2);

            var contour = new Contour(rawContour, (float)ratio, xOffset, yOffset);

            return contour.EvenlySpaceContourPoints2(Options.CurrentUserOptions.ContourSpacing);
            //return contour;
        }
    }
}
