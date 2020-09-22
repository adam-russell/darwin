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

using Darwin.Database;
using Darwin.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Matching
{
    public static class MatchFactorPresets
    {
        public static List<MatchFactor> CreateFinMatchFactors(RegistrationMethodType registrationMethod, bool useFullFinError,
            UpdateDisplayOutlinesDelegate updateOutlines)
        {
            var matchFactors = new List<MatchFactor>();

            // TODO: Move this elsewhere, change for Fins/Bears?
            var controlPoints = new List<FeaturePointType> { FeaturePointType.LeadingEdgeBegin, FeaturePointType.Tip, FeaturePointType.PointOfInflection };

            switch (registrationMethod)
            {
                case RegistrationMethodType.Original3Point:
                    // use beginning of leading edge, tip and largest trailing notch
                    // to map unknown outline to database outline, and then use
                    // version of meanSqError... that trims leading and trailing
                    // edge points to equalize number of points on each contour,
                    // and finally compute error between "corresponding" pairs of
                    // mapped points
                    matchFactors.Add(MatchFactor.CreateOutlineFactor(
                        1.0f,
                        controlPoints,
                        OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                        OutlineErrorFunctions.FindErrorBetweenFins_Original3Point,
                        updateOutlines));
                    break;
                case RegistrationMethodType.TrimFixedPercent:
                    // use a series of calls to meanSqError..., each with different amounts
                    // of the leading edge of each fin "ignored" in order to find the BEST
                    // choice of "leading edge beginning point" correspondence.  This prevents
                    // "bulging" of outlines due to long or short placement of the
                    // beginning of the trace.  Also, the version of meanSqError... used
                    // is one that walks the unknown fin outline point by point, and 
                    // computes the "closest point" on the database outline by finding
                    // the intersection of a perpendicular from the unknown outline point.
                    // This helps minimize errors due to nonuniform point spacing created
                    // during the mapping process.
                    matchFactors.Add(MatchFactor.CreateOutlineFactor(
                        1.0f,
                        controlPoints,
                        OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                        OutlineErrorFunctions.FindErrorBetweenFins,
                        updateOutlines));
                    break;
                /*removed - JHS
                            case TRIM_OPTIMAL :
                                // use an optimization process (essentially Newton-Raphson) to
                                // shorten the leading edges of each fin to produce a correspondence
                                // that yeilds the BEST match.  A fin Outline walking approach
                                // is used to conpute the meanSqError....
                                result = Match::findErrorBetweenFinsJHS(
                                            thisDBFin, timeTaken, regSegmentsUsed,
                                            useFullFinError);
                                break;
                */
                case RegistrationMethodType.TrimOptimalTotal:
                    // use an optimization process (essentially Newton-Raphson) to
                    // shorten the leading AND trailing edges of each fin to produce a correspondence
                    // that yeilds the BEST match.  A fin Outline walking approach
                    // is used to compute the meanSqError....
                    matchFactors.Add(MatchFactor.CreateOutlineFactor(
                        1.0f,
                        controlPoints,
                        OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                        OutlineErrorFunctions.FindErrorBetweenFinsOptimal,
                        updateOutlines,
                        new OutlineMatchOptions
                        {
                            MoveTip = false,
                            MoveEndsInAndOut = false,
                            UseFullFinError = useFullFinError
                        }));

                    break;
                case RegistrationMethodType.TrimOptimalTip:
                    matchFactors.Add(MatchFactor.CreateOutlineFactor(
                        1.0f,
                        controlPoints,
                        OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                        OutlineErrorFunctions.FindErrorBetweenFinsOptimal,
                        updateOutlines,
                        new OutlineMatchOptions
                        {
                            MoveTip = true,
                            MoveEndsInAndOut = false,
                            UseFullFinError = useFullFinError
                        }));
                    break;
                case RegistrationMethodType.TrimOptimalArea: //***1.85 - new area based metric option
                    matchFactors.Add(MatchFactor.CreateOutlineFactor(
                        1.0f,
                        controlPoints,
                        OutlineErrorFunctions.AreaBasedErrorBetweenOutlineSegments,
                        OutlineErrorFunctions.FindErrorBetweenFinsOptimal,
                        updateOutlines,
                        new OutlineMatchOptions
                        {
                            MoveTip = true,
                            MoveEndsInAndOut = false,
                            UseFullFinError = useFullFinError
                        }));
                    break;
                case RegistrationMethodType.TrimOptimalInOut:
                case RegistrationMethodType.TrimOptimalInOutTip:
                default:
                    throw new NotImplementedException();
            }

            return matchFactors;
        }

        public static List<MatchFactor> CreateBearMatchFactors(DarwinDatabase database)
        {
            var matchFactors = new List<MatchFactor>();

            var controlPoints = new List<FeaturePointType>()
            {
                FeaturePointType.Nasion,
                FeaturePointType.Tip,
                FeaturePointType.PointOfInflection,

                // Our alternate control point to also try a mapping on
                FeaturePointType.BottomLipProtrusion
            };

            matchFactors.Add(MatchFactor.CreateOutlineFactor(
                0.55f,
                controlPoints,
                //OutlineErrorFunctions.MeanSquaredErrorBetweenOutlinesWithControlPoints,
                OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                OutlineErrorFunctions.FindErrorBetweenOutlinesWithControlPointJitter,
                new OutlineMatchOptions
                {
                    MoveTip = true,
                    MoveEndsInAndOut = false,
                    UseFullFinError = true,
                    JumpDistancePercentage = 0.01f,
                    TrimBeginLeadingEdge = true,

                    // Also need to make sure there are 4 control points passed in if this is true
                    TryAlternateControlPoint3 = false
                }));

            matchFactors.Add(MatchFactor.CreateFeatureFactor(
                0.1f,
                FeatureErrorFunctions.ComputeCurvatureError,
                FeatureType.BrowCurvature));

            matchFactors.Add(MatchFactor.CreateFeatureFactor(
                0.05f,
                FeatureErrorFunctions.ComputeMouthDentError,
                FeatureType.HasMouthDent));

            var benchmarkFeatures = new List<FeaturePointType>()
            {
                //FeaturePointType.Nasion,
                //FeaturePointType.Tip
                FeaturePointType.Tip,
                FeaturePointType.Notch
            };

            var landmarkFeatures = new List<FeaturePointType>()
            {
                //FeaturePointType.LeadingEdgeBegin,
                FeaturePointType.Tip,
                FeaturePointType.Nasion,
                FeaturePointType.Notch,
                FeaturePointType.UpperLip,
                FeaturePointType.PointOfInflection
            };

            matchFactors.Add(MatchFactor.CreateFeaturePointFactor(
                0.35f,
                benchmarkFeatures,
                landmarkFeatures,
                
                // Pass in a specific set of ratios.  If not passed, it'll automatically
                // compute the ratio permutations of all the landmarks we pass in.
                //GetVerticalFeaturePointCombinations(),

                5, // Number of desired ratios
                database.AllFins,
                //FeaturePointErrorFunctions.ComputeEigenValueWeightedCosineDistance,
                FeaturePointErrorFunctions.ComputeMahalanobisDistance,

                // The option below will compare after using the lowest error mapping
                // from the outline error function above, assuming there's an outline error
                // function before this.
                new FeatureSetMatchOptions
                {
                    UseRemappedOutline = false
                }));

            return matchFactors;
        }

        public static List<MatchFactor> CreateBearMachineLearningMatchFactors(DarwinDatabase database)
        {
            var matchFactors = new List<MatchFactor>();
            matchFactors.Add(MatchFactor.CreateMachineLearningFactor(MachineLearningErrorFunctions.ComputeEmbeddingL2Distance, 1.0f, null));
            return matchFactors;
        }

        public static List<MatchFactor> CreateBearMatchFactorsOld(DarwinDatabase database)
        {
            var matchFactors = new List<MatchFactor>();

            var controlPoints = new List<FeaturePointType>()
            {
                FeaturePointType.Nasion,
                FeaturePointType.Tip,
                FeaturePointType.PointOfInflection
            };

            var benchmarkFeatures = new List<FeaturePointType>()
            {
                FeaturePointType.Nasion,
                FeaturePointType.Tip
                //FeaturePointType.Tip,
                //FeaturePointType.Notch
            };

            var landmarkFeatures = new List<FeaturePointType>()
            {
                //FeaturePointType.LeadingEdgeBegin,
                FeaturePointType.Tip,
                FeaturePointType.Nasion,
                FeaturePointType.Notch,
                FeaturePointType.UpperLip,
                FeaturePointType.PointOfInflection
            };

            matchFactors.Add(MatchFactor.CreateOutlineFactor(
                0.6f,
                controlPoints,
                //OutlineErrorFunctions.MeanSquaredErrorBetweenOutlinesWithControlPoints,
                OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                OutlineErrorFunctions.FindErrorBetweenOutlinesWithControlPointJitter,
                new OutlineMatchOptions
                {
                    MoveTip = true,
                    MoveEndsInAndOut = false,
                    UseFullFinError = true
                }));

            matchFactors.Add(MatchFactor.CreateFeaturePointFactor(
                0.4f,
                benchmarkFeatures,
                landmarkFeatures,
                5, // Number of desired ratios
                database.AllFins,
                //FeaturePointErrorFunctions.ComputeEigenValueWeightedCosineDistance,
                FeaturePointErrorFunctions.ComputeMahalanobisDistance,
                new FeatureSetMatchOptions
                {
                    UseRemappedOutline = false
                }));

            return matchFactors;
        }

        public static List<MatchFactor> CreateBearMatchFactorsOld2(DarwinDatabase database)
        {
            var matchFactors = new List<MatchFactor>();

            var controlPoints = new List<FeaturePointType>()
            {
                FeaturePointType.Nasion,
                FeaturePointType.Tip,
                FeaturePointType.PointOfInflection,

                // Our alternate control point to also try a mapping on
                FeaturePointType.BottomLipProtrusion
            };

            matchFactors.Add(MatchFactor.CreateOutlineFactor(
                0.55f,
                controlPoints,
                //OutlineErrorFunctions.MeanSquaredErrorBetweenOutlinesWithControlPoints,
                OutlineErrorFunctions.MeanSquaredErrorBetweenOutlineSegments,
                OutlineErrorFunctions.FindErrorBetweenOutlinesWithControlPointJitter,
                new OutlineMatchOptions
                {
                    MoveTip = true,
                    MoveEndsInAndOut = false,
                    UseFullFinError = true,
                    JumpDistancePercentage = 0.01f,
                    TrimBeginLeadingEdge = true,
                    TryAlternateControlPoint3 = true
                }));

            matchFactors.Add(MatchFactor.CreateFeatureFactor(
                0.1f,
                FeatureErrorFunctions.ComputeCurvatureError,
                FeatureType.BrowCurvature));

            matchFactors.Add(MatchFactor.CreateFeatureFactor(
                0.05f,
                FeatureErrorFunctions.ComputeMouthDentError,
                FeatureType.HasMouthDent));

            var benchmarkFeatures = new List<FeaturePointType>()
            {
                FeaturePointType.Nasion,
                FeaturePointType.Tip
                //FeaturePointType.Tip,
                //FeaturePointType.Notch
            };

            var landmarkFeatures = new List<FeaturePointType>()
            {
                //FeaturePointType.LeadingEdgeBegin,
                FeaturePointType.Tip,
                FeaturePointType.Nasion,
                FeaturePointType.Notch,
                FeaturePointType.UpperLip,
                FeaturePointType.PointOfInflection
            };

            matchFactors.Add(MatchFactor.CreateFeaturePointFactor(
                0.35f,
                benchmarkFeatures,
                landmarkFeatures,
                5, // Number of desired ratios
                database.AllFins,
                //FeaturePointErrorFunctions.ComputeEigenValueWeightedCosineDistance,
                FeaturePointErrorFunctions.ComputeMahalanobisDistance,
                new FeatureSetMatchOptions
                {
                    UseRemappedOutline = false
                }));

            return matchFactors;
        }

        private static List<IEnumerable<FeaturePointType>> GetVerticalFeaturePointCombinations()
        {
            var result = new List<IEnumerable<FeaturePointType>>()
            {
                new List<FeaturePointType>() { FeaturePointType.Tip, FeaturePointType.Notch },
                new List<FeaturePointType>() { FeaturePointType.Nasion, FeaturePointType.PointOfInflection },
                new List<FeaturePointType>() { FeaturePointType.Tip, FeaturePointType.UpperLip },
                new List<FeaturePointType>() { FeaturePointType.Notch, FeaturePointType.UpperLip }
            };

            return result;
        }
    }
}
