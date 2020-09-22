using Darwin.Database;
using Darwin.Model;
using Darwin.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Matching
{
    public static class MachineLearningErrorFunctions
    {
        public static MatchError ComputeEmbeddingL2Distance(
           DatabaseFin unknownFin,
           DatabaseFin databaseFin,
           MatchOptions options)
        {
            if (unknownFin == null)
                throw new ArgumentNullException(nameof(unknownFin));

            if (databaseFin == null)
                throw new ArgumentNullException(nameof(databaseFin));

            //var fin = CatalogSupport.FullyLoadFin(databaseFin);

            float[] unknownVec = FloatHelper.ConvertFromBase64String(unknownFin.PrimaryImage.Embedding);

            double? minError = null;
            FloatContour minErrorContour = null;
            foreach (var image in databaseFin.Images)
            {
                float[] curVec = FloatHelper.ConvertFromBase64String(image.Embedding);

                var currentDistance = MathHelper.GetDistance(unknownVec, curVec);

                if (minError == null || currentDistance < minError)
                {
                    minError = currentDistance;
                    minErrorContour = image.FinOutline.ChainPoints;
                }
            }

            //CatalogSupport.UnloadFin(fin);

            // Wait for the garbage collector after we just dereferenced some objects,
            // otherwise we end up maxing RAM if we have a large database.
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            return new MatchError
            {
                Error = minError.Value,
                Contour1 = unknownFin.PrimaryImage.FinOutline.ChainPoints,
                Contour2 = minErrorContour
            };
        }
    }
}
