/*
 * RJ wrote this -- replacing with proper header.
 */

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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Darwin.Database;
using Darwin.Features;
using Darwin.Matching;
using Darwin.Model;
using Darwin.Utilities;

namespace Darwin.Database
{
    // TODO: There's some copy/pasted code in here that could be refactored a little to
    // eliminate some duplication.
    // TODO: Maybe move some of this to an ORM like Sqlite PCL or something.
    public class SQLiteDatabase : DarwinDatabase
    {
        private const string SettingsCatalogSchemeName = "CatalogSchemeName";
        private const string SettingsFeatureSetType = "FeatureSetType";

        public const int LatestDBVersion = 7;

        private CatalogScheme _catalogScheme;
        public override CatalogScheme CatalogScheme
        {
            get
            {
                if (_catalogScheme == null)
                    _catalogScheme = SelectCatalogScheme();

                return _catalogScheme;
            }
        }

        public override ObservableCollection<Category> Categories
        {
            get
            {
                if (CatalogScheme == null)
                    return new ObservableCollection<Category>();

                return CatalogScheme.Categories;
            }
        }

        private List<DatabaseFin> _allFins;
        public override List<DatabaseFin> AllFins
        {
            get
            {
                // TODO: The thumbnail part should be temporary
                if (_allFins == null)
                    _allFins = GetAllFins();

                return _allFins;
            }
        }

        private string _connectionString;

        public SQLiteDatabase(string filename, CatalogScheme cat = null, bool createEmptyDB = false)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            Filename = filename;

            // We're using ConnectionStringBuilder to avoid injection attacks
            var builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = filename;
            builder.Version = 3;
            _connectionString = builder.ConnectionString;

            if (!File.Exists(builder.DataSource))
            {
                if (!createEmptyDB)
                    throw new Exception("Database file does not exist, and not trying to create it.");

                SQLiteConnection.CreateFile(builder.DataSource);

                CreateEmptyDatabase(cat);
            }
            else
            {
                // Let's make sure we can open it, and also check the version number and upgrade it if necessary
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    CheckVersionAndUpgrade(conn);

                    conn.Close();
                }
            }
        }

        // *****************************************************************************
        //
        // Returns complete DatabaseFin<ColorImage>. mDataPos field will be used to map to id in 
        // db for individuals
        //
        public override DatabaseFin GetFin(long id)
        {
            DBIndividual individual;
            //DBThumbnail thumbnail;
            Category damagecategory;
            FloatContour fc = new FloatContour();

            individual = SelectIndividualByID(id);
            damagecategory = SelectDamageCategoryByID(individual.fkdamagecategoryid);

            // TODO?
            // finOutline.SetLEAngle(0.0, true);

            var images = SelectImagesByFkIndividualID(id);

            if (images != null)
            {
                foreach (var image in images)
                {
                    var outline = SelectOutlineByFkImageID(image.ID);
                    //thumbnail = SelectThumbnailByFkImageID(image.id);
                    List<DBPoint> points = SelectPointsByFkOutlineID(outline.id);

                    // Although having both of these blocks of code seems uesless, this ensures that
                    // the given path contains only the image filename.  If the given path contains
                    // more, then the first code block will strip it down.

                    //// Strip path info
                    //image.imagefilename = Path.GetFileName(image.imagefilename);
                    //// Add current path info
                    //image.imagefilename = Path.Combine(new string[] { Options.CatalogFolderName, image.imagefilename });

                    //if (!string.IsNullOrEmpty(image.original_imagefilename))
                    //{
                    //    image.original_imagefilename = Path.Combine(new string[] { Options.CatalogFolderName, Path.GetFileName(image.original_imagefilename) });
                    //}

                    // assumes list is returned as FIFO (queue)... should be due to use of ORDER BY OrderID
                    foreach (var p in points)
                    {
                        fc.AddPoint(p.xcoordinate, p.ycoordinate);
                    }

                    var featurePoints = SelectFeaturePointsByFkOutlineID(outline.id);

                    Outline finOutline;

                    if (featurePoints != null && featurePoints.Count > 0)
                    {
                        var coordinateFeaturePoints = SelectCoordinateFeaturePointsByFkOutlineID(outline.id);
                        var features = SelectFeaturesByFkOutlineID(outline.id);

                        var featureSet = FeatureSet.Load(CatalogScheme.FeatureSetType, featurePoints, coordinateFeaturePoints, features);
                        finOutline = new Outline(fc, CatalogScheme.FeatureSetType, featureSet, outline.scale);
                    }
                    else
                    {
                        finOutline = new Outline(fc, CatalogScheme.FeatureSetType, outline.scale);
                        finOutline.SetFeaturePoint(FeaturePointType.LeadingEdgeBegin, outline.beginle);
                        finOutline.SetFeaturePoint(FeaturePointType.LeadingEdgeEnd, outline.endle);
                        finOutline.SetFeaturePoint(FeaturePointType.Notch, outline.notchposition);
                        finOutline.SetFeaturePoint(FeaturePointType.Tip, outline.tipposition);
                        finOutline.SetFeaturePoint(FeaturePointType.PointOfInflection, outline.endte);
                    }

                    image.FinOutline = finOutline;
                }
            }

            DatabaseFin fin = new DatabaseFin(id,
                individual.idcode,
                individual.name,
                individual.ThumbnailFilename,
                damagecategory.Name,
                new ObservableCollection<DatabaseImage>(images));

            return fin;
        }

        // *****************************************************************************
        //
        // Returns all fins from database.
        //
        public override List<DatabaseFin> GetAllFins()
        {
            List<DatabaseFin> fins = new List<DatabaseFin>();

            List<DBIndividual> individuals = SelectAllIndividuals();

            if (individuals == null)
                return fins;

            foreach (var ind in individuals)
            {
                fins.Add(GetFin(ind.id));
            }

            return fins;
        }

        public override long Add(DatabaseFin fin)
        {
            InvalidateAllFins();
            long individualId = 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    DBIndividual existingIndividual = null;

                    if (!string.IsNullOrEmpty(fin.IDCode))
                        existingIndividual = SelectIndividualByIDCode(fin.IDCode);

                    var dmgCat = SelectDamageCategoryByName(fin.DamageCategory);

                    if (dmgCat == null || dmgCat.ID == -1)
                        dmgCat = SelectDamageCategoryByName("NONE");

                    if (existingIndividual == null)
                    {
                        var individual = new DBIndividual();
                        individual.idcode = fin.IDCode;
                        individual.name = fin.Name;
                        individual.fkdamagecategoryid = dmgCat.ID;
                        individual.ThumbnailFilename = fin.ThumbnailFilename;
                        InsertIndividual(conn, ref individual);
                        individualId = individual.id;
                    }
                    else
                    {
                        individualId = existingIndividual.id;

                        if (dmgCat.ID != existingIndividual.fkdamagecategoryid || existingIndividual.ThumbnailFilename != fin.ThumbnailFilename)
                        {
                            existingIndividual.fkdamagecategoryid = dmgCat.ID;
                            existingIndividual.ThumbnailFilename = fin.ThumbnailFilename;

                            UpdateDBIndividual(conn, existingIndividual);
                        }
                    }

                    if (fin.Images != null)
                    {
                        foreach (var image in fin.Images)
                        {
                            var imageCopy = image;
                            image.ID = InsertImage(conn, individualId, ref imageCopy);

                            InsertImageModifications(conn, imageCopy.ID, image.ImageMods);

                            var outline = new DBOutline();
                            outline.scale = image.FinOutline.Scale;
                            outline.beginle = image.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeBegin);
                            outline.endle = image.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeEnd);
                            outline.notchposition = image.FinOutline.GetFeaturePoint(FeaturePointType.Notch);
                            outline.tipposition = image.FinOutline.GetFeaturePoint(FeaturePointType.Tip);
                            outline.endte = image.FinOutline.GetFeaturePoint(FeaturePointType.PointOfInflection);
                            outline.fkimageid = image.ID;
                            InsertOutline(conn, ref outline);

                            List<DBPoint> points = new List<DBPoint>();

                            var numPoints = image.FinOutline.Length;
                            var fc = image.FinOutline.ChainPoints;
                            for (int i = 0; i < numPoints; i++)
                            {
                                points.Add(new DBPoint
                                {
                                    xcoordinate = fc[i].X,
                                    ycoordinate = fc[i].Y,
                                    orderid = i,
                                    fkoutlineid = outline.id
                                });
                            }
                            InsertPoints(conn, points);

                            InsertFeatures(conn, outline.id, image.FinOutline.FeatureSet.FeatureList);
                            InsertFeaturePoints(conn, outline.id, image.FinOutline.FeatureSet.FeaturePointList);
                            InsertCoordinateFeaturePoints(conn, outline.id, image.FinOutline.FeatureSet.CoordinateFeaturePointList);
                        }
                    }

                    transaction.Commit();
                }
                conn.Close();
            }

            return individualId;
        }

        public override long Add(long individualId, DatabaseImage data)
        {
            if (individualId < 1)
                throw new ArgumentOutOfRangeException(nameof(individualId));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    data.ID = InsertImage(conn, individualId, ref data);

                    InsertImageModifications(conn, data.ID, data.ImageMods);

                    var outline = new DBOutline();
                    outline.scale = data.FinOutline.Scale;
                    outline.beginle = data.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeBegin);
                    outline.endle = data.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeEnd);
                    outline.notchposition = data.FinOutline.GetFeaturePoint(FeaturePointType.Notch);
                    outline.tipposition = data.FinOutline.GetFeaturePoint(FeaturePointType.Tip);
                    outline.endte = data.FinOutline.GetFeaturePoint(FeaturePointType.PointOfInflection);
                    outline.fkimageid = data.ID;
                    InsertOutline(conn, ref outline);

                    List<DBPoint> points = new List<DBPoint>();

                    var numPoints = data.FinOutline.Length;
                    var fc = data.FinOutline.ChainPoints;
                    for (int i = 0; i < numPoints; i++)
                    {
                        points.Add(new DBPoint
                        {
                            xcoordinate = fc[i].X,
                            ycoordinate = fc[i].Y,
                            orderid = i,
                            fkoutlineid = outline.id
                        });
                    }
                    InsertPoints(conn, points);

                    InsertFeatures(conn, outline.id, data.FinOutline.FeatureSet.FeatureList);
                    InsertFeaturePoints(conn, outline.id, data.FinOutline.FeatureSet.FeaturePointList);
                    InsertCoordinateFeaturePoints(conn, outline.id, data.FinOutline.FeatureSet.CoordinateFeaturePointList);

                    transaction.Commit();
                }
                conn.Close();
            }

            return data.ID;
        }

        //
        // Updates DatabaseFin<ColorImage>
        //
        public override void Update(DatabaseFin fin)
        {
            InvalidateAllFins();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    var dmgCat = SelectDamageCategoryByName(fin.DamageCategory);

                    DBIndividual individual = new DBIndividual();
                    individual.id = fin.ID; // mapping Individuals id to mDataPos
                    individual.idcode = fin.IDCode;
                    individual.name = fin.Name;
                    individual.fkdamagecategoryid = dmgCat.ID;
                    individual.ThumbnailFilename = fin.ThumbnailFilename;

                    UpdateDBIndividual(conn, individual);

                    // TODO: Any removals?
                    if (fin.Images != null)
                    {
                        foreach (var image in fin.Images)
                        {
                            image.IndividualId = individual.id;
                            if (image.ID == 0)
                            {
                                var imageCopy = image;
                                image.ID = InsertImage(conn, individual.id, ref imageCopy);
                            }
                            else
                            {
                                UpdateImage(conn, image);
                            }


                            // we do this as we don't know what the outline id is
                            var outline = SelectOutlineByFkImageID(image.ID);
                            outline.scale = image.FinOutline.Scale;
                            outline.beginle = image.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeBegin);
                            outline.endle = image.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeEnd);
                            outline.notchposition = image.FinOutline.GetFeaturePoint(FeaturePointType.Notch);
                            outline.tipposition = image.FinOutline.GetFeaturePoint(FeaturePointType.Tip);
                            outline.endte = image.FinOutline.GetFeaturePoint(FeaturePointType.PointOfInflection);
                            outline.fkimageid = image.ID;
                            UpdateOutline(conn, outline);

                            List<DBPoint> points = new List<DBPoint>();
                            var numPoints = image.FinOutline.Length;
                            var fc = image.FinOutline.ChainPoints;

                            for (var i = 0; i < numPoints; i++)
                            {
                                points.Add(new DBPoint
                                {
                                    xcoordinate = fc[i].X,
                                    ycoordinate = fc[i].Y,
                                    orderid = i,
                                    fkoutlineid = outline.id
                                });
                            }
                            DeletePoints(conn, outline.id);

                            InsertPoints(conn, points);

                            DeleteFeaturesIndividualID(conn, outline.id);
                            InsertFeatures(conn, outline.id, image.FinOutline.FeatureSet.FeatureList);

                            DeleteOutlineFeaturePointsByOutlineID(conn, outline.id);
                            InsertFeaturePoints(conn, outline.id, image.FinOutline.FeatureSet.FeaturePointList);

                            DeleteCoordinateFeaturePointsByIndividualID(conn, outline.id);
                            InsertCoordinateFeaturePoints(conn, outline.id, image.FinOutline.FeatureSet.CoordinateFeaturePointList);
                        }
                    }

                    transaction.Commit();
                }
                conn.Close();
            }
        }

        public override void Update(DatabaseImage data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.ID < 1)
                throw new ArgumentOutOfRangeException(nameof(data));

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    UpdateImage(conn, data);

                    // we do this as we don't know what the outline id is
                    var outline = SelectOutlineByFkImageID(data.ID);
                    outline.scale = data.FinOutline.Scale;
                    outline.beginle = data.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeBegin);
                    outline.endle = data.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeEnd);
                    outline.notchposition = data.FinOutline.GetFeaturePoint(FeaturePointType.Notch);
                    outline.tipposition = data.FinOutline.GetFeaturePoint(FeaturePointType.Tip);
                    outline.endte = data.FinOutline.GetFeaturePoint(FeaturePointType.PointOfInflection);
                    outline.fkimageid = data.ID;
                    UpdateOutline(conn, outline);

                    List<DBPoint> points = new List<DBPoint>();
                    var numPoints = data.FinOutline.Length;
                    var fc = data.FinOutline.ChainPoints;

                    for (var i = 0; i < numPoints; i++)
                    {
                        points.Add(new DBPoint
                        {
                            xcoordinate = fc[i].X,
                            ycoordinate = fc[i].Y,
                            orderid = i,
                            fkoutlineid = outline.id
                        });
                    }
                    DeletePoints(conn, outline.id);

                    InsertPoints(conn, points);

                    DeleteFeaturesIndividualID(conn, outline.id);
                    InsertFeatures(conn, outline.id, data.FinOutline.FeatureSet.FeatureList);

                    DeleteOutlineFeaturePointsByOutlineID(conn, outline.id);
                    InsertFeaturePoints(conn, outline.id, data.FinOutline.FeatureSet.FeaturePointList);

                    DeleteCoordinateFeaturePointsByIndividualID(conn, outline.id);
                    InsertCoordinateFeaturePoints(conn, outline.id, data.FinOutline.FeatureSet.CoordinateFeaturePointList);

                    transaction.Commit();
                }
                conn.Close();
            }
        }

        public override void InvalidateCache()
        {
            InvalidateAllFins();
        }

        public override void UpdateOutline(DatabaseImage databaseImage, bool preventInvalidate = false)
        {
            if (!preventInvalidate)
                InvalidateAllFins();

            if (databaseImage == null)
                throw new ArgumentNullException(nameof(databaseImage));

            // Invalid ID
            if (databaseImage.ID < 1)
                throw new ArgumentOutOfRangeException(nameof(databaseImage));

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    // we do this as we don't know what the outline id is
                    var outline = SelectOutlineByFkImageID(databaseImage.ID);
                    outline.scale = databaseImage.FinOutline.Scale;
                    outline.beginle = databaseImage.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeBegin);
                    outline.endle = databaseImage.FinOutline.GetFeaturePoint(FeaturePointType.LeadingEdgeEnd);
                    outline.notchposition = databaseImage.FinOutline.GetFeaturePoint(FeaturePointType.Notch);
                    outline.tipposition = databaseImage.FinOutline.GetFeaturePoint(FeaturePointType.Tip);
                    outline.endte = databaseImage.FinOutline.GetFeaturePoint(FeaturePointType.PointOfInflection);
                    outline.fkimageid = databaseImage.ID;
                    UpdateOutline(conn, outline);

                    List<DBPoint> points = new List<DBPoint>();
                    var numPoints = databaseImage.FinOutline.Length;
                    var fc = databaseImage.FinOutline.ChainPoints;

                    for (var i = 0; i < numPoints; i++)
                    {
                        points.Add(new DBPoint
                        {
                            xcoordinate = fc[i].X,
                            ycoordinate = fc[i].Y,
                            orderid = i,
                            fkoutlineid = outline.id
                        });
                    }
                    DeletePoints(conn, outline.id);

                    InsertPoints(conn, points);

                    DeleteFeaturesIndividualID(conn, outline.id);
                    InsertFeatures(conn, outline.id, databaseImage.FinOutline.FeatureSet.FeatureList);

                    DeleteOutlineFeaturePointsByOutlineID(conn, outline.id);
                    InsertFeaturePoints(conn, outline.id, databaseImage.FinOutline.FeatureSet.FeaturePointList);

                    DeleteCoordinateFeaturePointsByIndividualID(conn, outline.id);
                    InsertCoordinateFeaturePoints(conn, outline.id, databaseImage.FinOutline.FeatureSet.CoordinateFeaturePointList);

                    transaction.Commit();
                }
                conn.Close();
            }
        }

        public override void UpdateIndividual(DatabaseFin data)
        {
            InvalidateAllFins();

            var dmgCat = SelectDamageCategoryByName(data.DamageCategory);

            DBIndividual individual = new DBIndividual();
            individual.id = data.ID; // mapping Individuals id to mDataPos
            individual.idcode = data.IDCode;
            individual.name = data.Name;
            individual.fkdamagecategoryid = dmgCat.ID;
            individual.ThumbnailFilename = data.ThumbnailFilename;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                UpdateDBIndividual(conn, individual);
                conn.Close();
            }
        }
        public override bool ContainsAllFeatureTypes(List<FeatureType> featureTypes)
        {
            if (featureTypes == null || featureTypes.Count < 1)
                return true;

            if (AllFins == null || AllFins.Count < 1)
                return true;

            foreach (var individual in AllFins)
            {
                if (individual.PrimaryImage.FinOutline == null || individual.PrimaryImage.FinOutline.FeatureSet.Features == null)
                    return false;

                foreach (var type in featureTypes)
                {
                    if (!individual.PrimaryImage.FinOutline.FeatureSet.Features.ContainsKey(type)
                        || individual.PrimaryImage.FinOutline.FeatureSet.Features[type].IsEmpty)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool ContainsAllFeaturePointTypes(List<FeaturePointType> featurePointTypes)
        {
            if (featurePointTypes == null || featurePointTypes.Count < 1)
                return true;

            if (AllFins == null || AllFins.Count < 1)
                return true;

            foreach (var individual in AllFins)
            {
                if (individual.PrimaryImage.FinOutline == null || individual.PrimaryImage.FinOutline.FeatureSet.FeaturePoints == null)
                    return false;

                foreach (var type in featurePointTypes)
                {
                    if (!individual.PrimaryImage.FinOutline.FeatureSet.FeaturePoints.ContainsKey(type)
                        || individual.PrimaryImage.FinOutline.FeatureSet.FeaturePoints[type].IsEmpty)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // *****************************************************************************
        //
        // Delete fin from database
        //
        public override void Delete(DatabaseFin fin)
        {
            InvalidateAllFins();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    var images = SelectImagesByFkIndividualID(fin.ID);
                    
                    foreach (var image in images)
                    {
                        DeleteImageModifications(conn, image.ID);
                        DeleteImage(conn, image.ID);
                        DeleteThumbnailByFkImageID(conn, image.ID);
                        var outline = SelectOutlineByFkImageID(image.ID);

                        DeletePoints(conn, outline.id);
                        DeleteOutlineByFkImageID(conn, image.ID);
                    }

                    DeleteIndividual(conn, fin.ID);

                    transaction.Commit();
                }

                conn.Close();
            }
        }

        public override void Delete(DatabaseImage data)
        {
            InvalidateAllFins();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    DeleteImageModifications(conn, data.ID);
                    DeleteImage(conn, data.ID);

                    DeleteThumbnailByFkImageID(conn, data.ID);
                    var outline = SelectOutlineByFkImageID(data.ID);

                    DeletePoints(conn, outline.id);
                    DeleteOutlineByFkImageID(conn, data.ID);

                    transaction.Commit();
                }

                conn.Close();
            }
        }

        private List<DBIndividual> SelectAllIndividuals()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Individuals;";

                    var rdr = cmd.ExecuteReader();

                    List<DBIndividual> individuals = new List<DBIndividual>();
                    while (rdr.Read())
                    {
                        var individual = new DBIndividual
                        {
                            id = rdr.SafeGetInt("ID"),
                            idcode = rdr.SafeGetString("IDCode"),
                            name = rdr.SafeGetStringStripNone("Name"),
                            fkdamagecategoryid = rdr.SafeGetInt("fkDamageCategoryID"),
                            ThumbnailFilename = rdr.SafeGetString("ThumbnailFilename")
                        };

                        individuals.Add(individual);
                    }

                    conn.Close();

                    return individuals;
                }
            }
        }

        private DBIndividual SelectIndividualByIDCode(string idCode)
        {
            if (string.IsNullOrEmpty(idCode))
                throw new ArgumentNullException(nameof(idCode));

            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Individuals WHERE UPPER(IDCode) = @IDCode;";
                    cmd.Parameters.AddWithValue("@IDCode", idCode.Trim().ToUpperInvariant());

                    DBIndividual individual = null;
                    using (var rdr = cmd.ExecuteReader())
                    {

                        if (rdr.Read())
                        {
                            individual = new DBIndividual
                            {
                                id = rdr.SafeGetInt("ID"),
                                idcode = rdr.SafeGetString("IDCode"),
                                name = rdr.SafeGetStringStripNone("Name"),
                                fkdamagecategoryid = rdr.SafeGetInt("fkDamageCategoryID"),
                                ThumbnailFilename = rdr.SafeGetString("ThumbnailFilename")
                            };
                        }
                    }

                    conn.Close();

                    return individual;
                }
            }
        }

        private DBIndividual SelectIndividualByID(long id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Individuals WHERE ID = @ID;";
                    cmd.Parameters.AddWithValue("@ID", id);

                    DBIndividual individual = null;
                    using (var rdr = cmd.ExecuteReader())
                    {

                        if (rdr.Read())
                        {
                            individual = new DBIndividual
                            {
                                id = rdr.SafeGetInt("ID"),
                                idcode = rdr.SafeGetString("IDCode"),
                                name = rdr.SafeGetStringStripNone("Name"),
                                fkdamagecategoryid = rdr.SafeGetInt("fkDamageCategoryID"),
                                ThumbnailFilename = rdr.SafeGetString("ThumbnailFilename")
                            };
                        }
                    }

                    conn.Close();

                    return individual;
                }
            }
        }

        private Category SelectDamageCategoryByName(string name)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM DamageCategories WHERE Name = @Name;";
                    cmd.Parameters.AddWithValue("@Name", name);

                    Category category = null;

                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            category = new Category
                            {
                                ID = rdr.SafeGetInt("ID"),
                                Name = rdr.SafeGetString("Name"),
                                Order = rdr.SafeGetInt("OrderID")
                            };
                        }
                    }
                    conn.Close();

                    return category;
                }
            }
        }

        private Category SelectDamageCategoryByID(long id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM DamageCategories WHERE ID = @ID;";
                    cmd.Parameters.AddWithValue("@ID", id);

                    Category category = null;

                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            category = new Category
                            {
                                ID = rdr.SafeGetInt("ID"),
                                Name = rdr.SafeGetString("Name"),
                                Order = rdr.SafeGetInt("OrderID")
                            };
                        }
                    }

                    conn.Close();

                    return category;
                }
            }
        }

        public override void SetCatalogScheme(CatalogScheme catalogScheme)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    SaveCatalogScheme(conn, catalogScheme);

                    transaction.Commit();

                    _catalogScheme = new CatalogScheme(catalogScheme);
                }
                conn.Close();
            }
        }

        private void SaveCatalogScheme(SQLiteConnection conn, CatalogScheme catalogScheme)
        {
            var currentSettings = SelectAllSettings();

            if (currentSettings == null || !currentSettings.ContainsKey(SettingsCatalogSchemeName))
                InsertSetting(conn, SettingsCatalogSchemeName, catalogScheme.SchemeName);
            else
                UpdateSetting(conn, SettingsCatalogSchemeName, catalogScheme.SchemeName);

            if (currentSettings == null || !currentSettings.ContainsKey(SettingsFeatureSetType))
                InsertSetting(conn, SettingsFeatureSetType, ((int)catalogScheme.FeatureSetType).ToString());
            else
                UpdateSetting(conn, SettingsFeatureSetType, ((int)catalogScheme.FeatureSetType).ToString());

            for (int i = 0; i < catalogScheme.Categories.Count; i++)
            {
                catalogScheme.Categories[i].Order = i;

                if (catalogScheme.Categories[i].ID > 0 && SelectDamageCategoryByID(catalogScheme.Categories[i].ID) != null)
                {
                    UpdateDamageCategory(conn, catalogScheme.Categories[i]);
                }
                else
                {
                    Category tempCat = new Category(catalogScheme.Categories[i]);
                    InsertDamageCategory(conn, ref tempCat);
                    catalogScheme.Categories[i].ID = tempCat.ID;
                }
            }
        }

        private CatalogScheme SelectCatalogScheme()
        {
            var settings = SelectAllSettings();

            string schemeName = "Eckerd College";
            FeatureSetType featureSetType = FeatureSetType.DorsalFin;

            if (settings != null && settings.ContainsKey(SettingsCatalogSchemeName))
                schemeName = settings[SettingsCatalogSchemeName];

            if (settings != null && settings.ContainsKey(SettingsFeatureSetType))
                featureSetType = (FeatureSetType)Convert.ToInt32(settings[SettingsFeatureSetType]);

            return new CatalogScheme(schemeName, featureSetType, SelectAllDamageCategories());
        }

        private List<Category> SelectAllDamageCategories()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM DamageCategories ORDER BY OrderID;";

                    var categories = new List<Category>();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var cat = new Category
                            {
                                ID = rdr.SafeGetInt("ID"),
                                Name = rdr.SafeGetString("Name"),
                                Order = rdr.SafeGetInt("OrderID")
                            };

                            categories.Add(cat);
                        }

                        conn.Close();
                    }

                    return categories;
                }
            }
        }

        // *****************************************************************************
        //
        // This returns all the DBInfo rows as a list of DBInfo structs.
        //
        public Dictionary<string,string> SelectAllSettings()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Settings;";

                    var settings = new Dictionary<string, string>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            settings[rdr.SafeGetString("Key")] = rdr.SafeGetString("Value");
                        }
                    }

                    conn.Close();

                    return settings;
                }
            }
        }

        // *****************************************************************************
        //
        // Populates given list<DBImageModification> with all rows from 
        // ImageModifications table.
        //
        private List<DBImageModification> SelectAllImageModifications()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM ImageModifications;";

                    var modifications = new List<DBImageModification>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var mod = new DBImageModification
                            {
                                id = rdr.SafeGetInt("ID"),
                                operation = rdr.SafeGetInt("Operation"),
                                value1 = rdr.SafeGetInt("Value1"),
                                value2 = rdr.SafeGetInt("Value2"),
                                value3 = rdr.SafeGetInt("Value3"),
                                value4 = rdr.SafeGetInt("Value4"),
                                orderid = rdr.SafeGetInt("OrderID"),
                                fkimageid = rdr.SafeGetInt("fkImageID")
                            };

                            modifications.Add(mod);
                        }
                    }

                    conn.Close();

                    return modifications;
                }
            }
        }

        // *****************************************************************************
        //
        // Populates given list<DBImageModification> with all rows from 
        // ImageModifications table where fkImageID equals the given int.
        //
        private List<DBImageModification> SelectImageModificationsByFkImageID(long fkimageid)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM ImageModifications WHERE fkImageID = @fkImageID ORDER BY OrderID;";
                    cmd.Parameters.AddWithValue("@fkImageID", fkimageid);

                    var modifications = new List<DBImageModification>();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var mod = new DBImageModification
                            {
                                id = rdr.SafeGetInt("ID"),
                                operation = rdr.SafeGetInt("Operation"),
                                value1 = rdr.SafeGetInt("Value1"),
                                value2 = rdr.SafeGetInt("Value2"),
                                value3 = rdr.SafeGetInt("Value3"),
                                value4 = rdr.SafeGetInt("Value4"),
                                orderid = rdr.SafeGetInt("OrderID"),
                                fkimageid = rdr.SafeGetInt("fkImageID")
                            };

                            modifications.Add(mod);
                        }
                    }

                    conn.Close();

                    return modifications;
                }
            }
        }

        // *****************************************************************************
        //
        // Populates given list<DBImage> with all rows from Images table.
        //
        //private List<DatabaseImage> SelectAllImages()
        //{
        //    using (var conn = new SQLiteConnection(_connectionString))
        //    {
        //        using (var cmd = new SQLiteCommand(conn))
        //        {
        //            conn.Open();

        //            cmd.CommandText = "SELECT * FROM Images;";

        //            var images = new List<DatabaseImage>();

        //            using (var rdr = cmd.ExecuteReader())
        //            {
        //                while (rdr.Read())
        //                {
        //                    var img = new DatabaseImage
        //                    {
        //                        ID = rdr.SafeGetInt("ID"),
        //                        ImageFilename = rdr.SafeGetString("ImageFilename"),
        //                        OriginalImageFilename = rdr.SafeGetString("OriginalImageFilename"),
        //                        DateOfSighting = rdr.SafeGetStringStripNone("DateOfSighting"),
        //                        RollAndFrame = rdr.SafeGetStringStripNone("RollAndFrame"),
        //                        LocationCode = rdr.SafeGetStringStripNone("LocationCode"),
        //                        ShortDescription = rdr.SafeGetStringStripNone("ShortDescription"),
        //                        IndividualId = rdr.SafeGetInt("fkIndividualID")
        //                    };

        //                    images.Add(img);
        //                }
        //            }

        //            conn.Close();

        //            return images;
        //        }
        //    }
        //}

        // *****************************************************************************
        //
        // Populates given list<DBImage> with all rows from Images table where
        // the fkIndividualID equals the given int.
        //
        private List<DatabaseImage> SelectImagesByFkIndividualID(long fkindividualid)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Images WHERE fkIndividualID = @fkIndividualID;";
                    cmd.Parameters.AddWithValue("@fkIndividualID", fkindividualid);

                    var images = new List<DatabaseImage>();

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var img = new DatabaseImage
                            {
                                ID = rdr.SafeGetInt("ID"),
                                ImageFilename = rdr.SafeGetString("ImageFilename"),
                                OriginalImageFilename = rdr.SafeGetString("OriginalImageFilename"),
                                DateOfSighting = rdr.SafeGetDateTimeStripNone("DateOfSighting"),
                                RollAndFrame = rdr.SafeGetStringStripNone("RollAndFrame"),
                                LocationCode = rdr.SafeGetStringStripNone("LocationCode"),
                                ShortDescription = rdr.SafeGetStringStripNone("ShortDescription"),
                                IndividualId = rdr.SafeGetInt("fkIndividualID"),
                                CropImageFilename = rdr.SafeGetString("CropImageFilename"),
                                OrderId = rdr.SafeGetInt("OrderId")
                            };

                            var imageMods = SelectImageModificationsByFkImageID(img.ID);

                            var mods = new List<ImageMod>();

                            if (imageMods != null)
                            {
                                foreach (var mod in imageMods)
                                {
                                    mods.Add(new ImageMod((ImageModType)mod.operation, mod.value1, mod.value2, mod.value3, mod.value4));
                                }
                            }

                            img.ImageMods = mods;

                            images.Add(img);
                        }
                    }

                    conn.Close();

                    return images;
                }
            }
        }

        // *****************************************************************************
        //
        // Returns DBImage of row with given fkIndividualID
        //
        //private DBImage SelectImageByFkIndividualID(long fkindividualid)
        //{
        //    var images = SelectImagesByFkIndividualID(fkindividualid);

        //    return images?.FirstOrDefault();
        //}

        // *****************************************************************************
        //
        // This returns all the Outlines rows as a list of DBOutline structs.
        //
        //private List<DBOutline> selectAllOutlines()
        //{
        //    using (var conn = new SQLiteConnection(_connectionString))
        //    {
        //        using (var cmd = new SQLiteCommand(conn))
        //        {
        //            conn.Open();

        //            cmd.CommandText = "SELECT * FROM Outlines;";

        //            var outlines = new List<DBOutline>();
        //            using (var rdr = cmd.ExecuteReader())
        //            {
        //                while (rdr.Read())
        //                {
        //                    var outline = new DBOutline
        //                    {
        //                        id = rdr.SafeGetInt("ID"),
        //                        scale = rdr.SafeGetDouble("Scale", 1.0),
        //                        tipposition = rdr.SafeGetInt("TipPosition"),
        //                        beginle = rdr.SafeGetInt("BeginLE"),
        //                        endle = rdr.SafeGetInt("EndLE"),
        //                        notchposition = rdr.SafeGetInt("NotchPosition"),
        //                        endte = rdr.SafeGetInt("EndTE"),
        //                        fkimageid = rdr.SafeGetInt("fkImageID")
        //                    };

        //                    outlines.Add(outline);
        //                }
        //            }

        //            conn.Close();

        //            return outlines;
        //        }
        //    }
        //}

        // *****************************************************************************
        //
        // Returns DBOutline from Outlines table where the fkIndividualID equals
        // the given int.
        //
        private DBOutline SelectOutlineByFkImageID(long fkImageID)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Outlines WHERE fkImageID = @fkImageID;";
                    cmd.Parameters.AddWithValue("@fkImageID", fkImageID);

                    DBOutline outline = null;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            outline = new DBOutline
                            {
                                id = rdr.SafeGetInt("ID"),
                                scale = rdr.SafeGetDouble("Scale", 1.0),
                                tipposition = rdr.SafeGetInt("TipPosition"),
                                beginle = rdr.SafeGetInt("BeginLE"),
                                endle = rdr.SafeGetInt("EndLE"),
                                notchposition = rdr.SafeGetInt("NotchPosition"),
                                endte = rdr.SafeGetInt("EndTE"),
                                fkimageid = rdr.SafeGetInt("fkImageID")
                            };
                        }
                    }

                    conn.Close();

                    return outline;
                }
            }
        }

        private List<OutlineFeaturePoint> SelectFeaturePointsByFkOutlineID(long fkoutlineid)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM OutlineFeaturePoints WHERE fkOutlineID = @fkOutlineID;";
                    cmd.Parameters.AddWithValue("@fkOutlineID", fkoutlineid);

                    List<OutlineFeaturePoint> points = new List<OutlineFeaturePoint>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var featurePoint = new OutlineFeaturePoint
                            {
                                ID = rdr.SafeGetInt("ID"),
                                Type = (FeaturePointType)rdr.SafeGetInt("Type"),
                                Position = rdr.SafeGetInt("Position"),
                                UserSetPosition = rdr.SafeGetInt("UserSetPosition") != 0,
                                Ignore = rdr.SafeGetInt("Ignore") != 0
                            };

                            points.Add(featurePoint);
                        }
                    }

                    conn.Close();

                    return points;
                }
            }
        }

        private List<CoordinateFeaturePoint> SelectCoordinateFeaturePointsByFkOutlineID(long fkOutlineID)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM CoordinateFeaturePoints WHERE fkOutlineID = @fkOutlineID;";
                    cmd.Parameters.AddWithValue("@fkOutlineID", fkOutlineID);

                    List<CoordinateFeaturePoint> points = new List<CoordinateFeaturePoint>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var featurePoint = new CoordinateFeaturePoint
                            {
                                ID = rdr.SafeGetInt("ID"),
                                Type = (FeaturePointType)rdr.SafeGetInt("Type"),
                                Coordinate = new Point(rdr.SafeGetInt("X"), rdr.SafeGetInt("Y")),
                                UserSetPosition = rdr.SafeGetInt("UserSetPosition") != 0,
                                Ignore = rdr.SafeGetInt("Ignore") != 0
                            };

                            points.Add(featurePoint);
                        }
                    }

                    conn.Close();

                    return points;
                }
            }
        }

        private List<Feature> SelectFeaturesByFkOutlineID(long fkOutlineID)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Features WHERE fkOutlineID = @fkOutlineID;";
                    cmd.Parameters.AddWithValue("@fkOutlineID", fkOutlineID);

                    List<Feature> features = new List<Feature>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var feature = new Feature
                            {
                                ID = rdr.SafeGetInt("ID"),
                                Type = (FeatureType)rdr.SafeGetInt("Type"),
                                Value = rdr.SafeGetNullableDouble("Value")
                            };

                            features.Add(feature);
                        }
                    }

                    conn.Close();

                    return features;
                }
            }
        }

        // *****************************************************************************
        //
        // Populates given list<DBPoint> with all rows from Points table where
        // the fkOutlineID equals the given int.
        //
        private List<DBPoint> SelectPointsByFkOutlineID(long fkoutlineid)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Points WHERE fkOutlineID = @fkOutlineID ORDER BY OrderID;";
                    cmd.Parameters.AddWithValue("@fkOutlineID", fkoutlineid);

                    List<DBPoint> points = new List<DBPoint>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var point = new DBPoint
                            {
                                id = rdr.SafeGetInt("ID"),
                                xcoordinate = (float)rdr.SafeGetDouble("XCoordinate"),
                                ycoordinate = (float)rdr.SafeGetDouble("YCoordinate"),
                                fkoutlineid = rdr.SafeGetInt("fkOutlineID"),
                                orderid = rdr.SafeGetInt("OrderID")
                            };

                            points.Add(point);
                        }
                    }

                    conn.Close();

                    return points;
                }
            }
        }

        // *****************************************************************************
        //
        // This returns all the Thumbnails rows as a list of DBThumbnail structs.
        //
        private List<DBThumbnail> SelectAllThumbnails()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Thumbnails;";

                    List<DBThumbnail> thumbnails = new List<DBThumbnail>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var thumb = new DBThumbnail
                            {
                                id = rdr.SafeGetInt("ID"),
                                rows = rdr.SafeGetInt("Rows"),
                                pixmap = rdr.SafeGetString("Pixmap"),
                                fkimageid = rdr.SafeGetInt("fkImageID")
                            };

                            thumbnails.Add(thumb);
                        }
                    }

                    conn.Close();

                    return thumbnails;
                }
            }
        }

        // *****************************************************************************
        //
        // Selects a single Thumbnail.
        //
        private DBThumbnail SelectThumbnailByFkImageID(long fkimageid)
        {
            var thumbnails = SelectThumbnailsByFkImageID(fkimageid);

            if (thumbnails == null)
                return null;

            return thumbnails.FirstOrDefault();
        }

        // *****************************************************************************
        //
        // This returns all the Thumbnails rows as a list of DBThumbnail structs.
        //
        private List<DBThumbnail> SelectThumbnailsByFkImageID(long fkimageid)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    conn.Open();

                    cmd.CommandText = "SELECT * FROM Thumbnails WHERE fkImageID = @fkImageID;";
                    cmd.Parameters.AddWithValue("@fkImageID", fkimageid);

                    List<DBThumbnail> thumbnails = new List<DBThumbnail>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var thumb = new DBThumbnail
                            {
                                id = rdr.SafeGetInt("ID"),
                                rows = rdr.SafeGetInt("Rows"),
                                pixmap = rdr.SafeGetString("Pixmap"),
                                fkimageid = rdr.SafeGetInt("fkImageID")
                            };

                            thumbnails.Add(thumb);
                        }
                    }
                    conn.Close();

                    return thumbnails;
                }
            }
        }

        // *****************************************************************************
        //
        // Inserts Individual into Individuals table.  id needs to be unique.
        //
        private long InsertIndividual(SQLiteConnection conn, ref DBIndividual individual)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Individuals (ID, IDCode, Name, fkDamageCategoryID) " +
                    "VALUES (NULL, @IDCode, @Name, @fkDamageCategoryID);";
                cmd.Parameters.AddWithValue("@IDCode", individual.idcode);
                cmd.Parameters.AddWithValue("@Name", individual.name);
                cmd.Parameters.AddWithValue("@fkDamageCategoryID", individual.fkdamagecategoryid);

                cmd.ExecuteNonQuery();

                individual.id = conn.LastInsertRowId;

                return individual.id;
            }
        }

        // *****************************************************************************
        //
        // Inserts DamageCategory into DamageCategories table.  Ignores id as
        // this is autoincremented in the database.
        //
        private long InsertDamageCategory(SQLiteConnection conn, ref Category damagecategory)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO DamageCategories (ID, Name, OrderID)  " +
                    "VALUES (NULL, @Name, @OrderID);";
                cmd.Parameters.AddWithValue("@Name", damagecategory.Name);
                cmd.Parameters.AddWithValue("@OrderID", damagecategory.Order);

                cmd.ExecuteNonQuery();

                damagecategory.ID = conn.LastInsertRowId;

                return damagecategory.ID;
            }
        }

        private long InsertFeaturePoint(SQLiteConnection conn, long fkOutlineID, ref OutlineFeaturePoint point)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO OutlineFeaturePoints (ID, Type, Position, UserSetPosition, Ignore, fkOutlineID) " +
                    "VALUES (NULL, @Type, @Position, @UserSetPosition, @Ignore, @fkOutlineID);";
                cmd.Parameters.AddWithValue("@Type", point.Type);
                cmd.Parameters.AddWithValue("@Position", point.Position);
                cmd.Parameters.AddWithValue("@UserSetPosition", (point.UserSetPosition) ? 1 : 0);
                cmd.Parameters.AddWithValue("@Ignore", (point.Ignore) ? 1 : 0);
                cmd.Parameters.AddWithValue("@fkOutlineID", fkOutlineID);

                cmd.ExecuteNonQuery();

                point.ID = conn.LastInsertRowId;

                return point.ID;
            }
        }

        private long InsertCoordinateFeaturePoint(SQLiteConnection conn, long fkOutlineID, ref CoordinateFeaturePoint point)
        {
            if (point.IsEmpty)
                return 0;

            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO CoordinateFeaturePoints (ID, Type, X, Y, UserSetPosition, Ignore, fkOutlineID) " +
                    "VALUES (NULL, @Type, @X, @Y, @UserSetPosition, @Ignore, @fkOutlineID);";
                cmd.Parameters.AddWithValue("@Type", point.Type);
                cmd.Parameters.AddWithValue("@X", point.Coordinate?.X);
                cmd.Parameters.AddWithValue("@Y", point.Coordinate?.Y);
                cmd.Parameters.AddWithValue("@UserSetPosition", (point.UserSetPosition) ? 1 : 0);
                cmd.Parameters.AddWithValue("@Ignore", (point.Ignore) ? 1 : 0);
                cmd.Parameters.AddWithValue("@fkOutlineID", fkOutlineID);

                cmd.ExecuteNonQuery();

                point.ID = conn.LastInsertRowId;

                return point.ID;
            }
        }

        private long InsertFeature(SQLiteConnection conn, long fkOutlineID, ref Feature feature)
        {
            if (feature.IsEmpty)
                return 0;

            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Features (ID, Type, Value, fkOutlineID) " +
                    "VALUES (NULL, @Type, @Value, @fkOutlineID);";
                cmd.Parameters.AddWithValue("@Type", feature.Type);
                cmd.Parameters.AddWithValue("@Value", feature?.Value);
                cmd.Parameters.AddWithValue("@fkOutlineID", fkOutlineID);

                cmd.ExecuteNonQuery();

                feature.ID = conn.LastInsertRowId;

                return feature.ID;
            }
        }

        // *****************************************************************************
        //
        // Inserts DBPoint into Points table
        //
        private long InsertPoint(SQLiteConnection conn, ref DBPoint point)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Points (ID, XCoordinate, YCoordinate, fkOutlineID, OrderID) " +
                    "VALUES (NULL, @XCoordinate, @YCoordinate, @fkOutlineID, @OrderID);";
                cmd.Parameters.AddWithValue("@XCoordinate", point.xcoordinate);
                cmd.Parameters.AddWithValue("@YCoordinate", point.ycoordinate);
                cmd.Parameters.AddWithValue("@fkOutlineID", point.fkoutlineid);
                cmd.Parameters.AddWithValue("@OrderID", point.orderid);

                cmd.ExecuteNonQuery();

                point.id = conn.LastInsertRowId;

                return point.id;
            }
        }

        private void InsertSetting(SQLiteConnection conn, string key, string value)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Settings (Key, Value) " +
                    "VALUES (@Key, @Value);";
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.Parameters.AddWithValue("@Value", value);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Inserts DBOutline into Outlines table
        //
        private long InsertOutline(SQLiteConnection conn, ref DBOutline outline)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Outlines (ID, Scale, TipPosition, BeginLE, EndLE, NotchPosition, EndTE, fkImageID) " +
                    "VALUES (NULL, @Scale, @TipPosition, @BeginLE, @EndLE, @NotchPosition, @EndTE, @fkImageID);";

                cmd.Parameters.AddWithValue("@Scale", outline.scale);
                cmd.Parameters.AddWithValue("@TipPosition", outline.tipposition);
                cmd.Parameters.AddWithValue("@BeginLE", outline.beginle);
                cmd.Parameters.AddWithValue("@EndLE", outline.endle);
                cmd.Parameters.AddWithValue("@NotchPosition", outline.notchposition);
                cmd.Parameters.AddWithValue("@EndTE", outline.endte);
                cmd.Parameters.AddWithValue("@fkImageID", outline.fkimageid);

                cmd.ExecuteNonQuery();

                outline.id = conn.LastInsertRowId;

                return outline.id;
            }
        }

        // *****************************************************************************
        //
        // Inserts DBImage into Images table
        //
        private long InsertImage(SQLiteConnection conn, long individualId, ref DatabaseImage image)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Images(ID, ImageFilename, OriginalImageFilename, DateOfSighting, RollAndFrame, LocationCode, ShortDescription, CropImageFilename, OrderId, fkIndividualID) " +
                    "VALUES (NULL, @ImageFilename, @OriginalImageFilename, @DateOfSighting, @RollAndFrame, @LocationCode, @ShortDescription, @CropImageFilename, @OrderId, @fkIndividualID);";
                cmd.Parameters.AddWithValue("@ImageFilename", image.ImageFilename);
                cmd.Parameters.AddWithValue("@OriginalImageFilename", image.OriginalImageFilename);
                cmd.Parameters.AddWithValue("@DateOfSighting", image.DateOfSighting);
                cmd.Parameters.AddWithValue("@RollAndFrame", image.RollAndFrame);
                cmd.Parameters.AddWithValue("@LocationCode", image.LocationCode);
                cmd.Parameters.AddWithValue("@ShortDescription", image.ShortDescription);
                cmd.Parameters.AddWithValue("@CropImageFilename", image.CropImageFilename);
                cmd.Parameters.AddWithValue("@OrderId", image.OrderId);
                cmd.Parameters.AddWithValue("@fkIndividualID", individualId);

                cmd.ExecuteNonQuery();

                image.ID = conn.LastInsertRowId;

                return image.ID;
            }
        }

        private void InsertImageModifications(SQLiteConnection conn, long imageId, List<ImageMod> mods)
        {
            if (mods != null)
            {
                List<DBImageModification> modifications = new List<DBImageModification>();

                for (var j = 0; j < mods.Count; j++)
                {
                    ImageModType modType;
                    int val1, val2, val3, val4;

                    mods[j].Get(out modType, out val1, out val2, out val3, out val4);

                    modifications.Add(new DBImageModification
                    {
                        fkimageid = imageId,
                        operation = (int)modType,
                        value1 = val1,
                        value2 = val2,
                        value3 = val3,
                        value4 = val4,
                        orderid = j + 1
                    });
                }

                InsertImageModifications(conn, modifications);
            }
        }

        // *****************************************************************************
        //
        // Inserts DBImageModification into ImageModifications table
        //
        private long InsertImageModification(SQLiteConnection conn, ref DBImageModification imagemod)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO ImageModifications(ID, Operation, Value1, Value2, Value3, Value4, OrderID, fkImageID) " +
                    "VALUES (NULL, @Operation, @Value1, @Value2, @Value3, @Value4, @OrderID, @fkImageID);";
                cmd.Parameters.AddWithValue("@Operation", imagemod.operation);
                cmd.Parameters.AddWithValue("@Value1", imagemod.value1);
                cmd.Parameters.AddWithValue("@Value2", imagemod.value2);
                cmd.Parameters.AddWithValue("@Value3", imagemod.value3);
                cmd.Parameters.AddWithValue("@Value4", imagemod.value4);
                cmd.Parameters.AddWithValue("@OrderID", imagemod.orderid);
                cmd.Parameters.AddWithValue("@fkImageID", imagemod.fkimageid);

                cmd.ExecuteNonQuery();

                imagemod.id = conn.LastInsertRowId;

                return imagemod.id;
            }
        }

        // *****************************************************************************
        //
        // Inserts DBThumbnail into Thumbnails table
        //
        private long InsertThumbnail(SQLiteConnection conn, ref DBThumbnail thumbnail)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "INSERT INTO Thumbnails (ID, Rows, Pixmap, fkImageID) " +
                    "VALUES (NULL, @Rows, @Pixmap, @fkImageID);";
                cmd.Parameters.AddWithValue("@Rows", thumbnail.rows);
                cmd.Parameters.AddWithValue("@Pixmap", thumbnail.pixmap);
                cmd.Parameters.AddWithValue("@fkImageID", thumbnail.fkimageid);

                cmd.ExecuteNonQuery();

                thumbnail.id = conn.LastInsertRowId;

                return thumbnail.id;
            }
        }

        private void InsertFeaturePoints(SQLiteConnection conn, long fkOutlineID, List<OutlineFeaturePoint> points)
        {
            if (points != null)
            {
                foreach (var p in points)
                {
                    var pointCopy = p;
                    InsertFeaturePoint(conn, fkOutlineID, ref pointCopy);
                }
            }
        }

        private void InsertCoordinateFeaturePoints(SQLiteConnection conn, long fkOutlineID, List<CoordinateFeaturePoint> points)
        {
            if (points != null)
            {
                foreach (var p in points)
                {
                    var pointCopy = p;
                    InsertCoordinateFeaturePoint(conn, fkOutlineID, ref pointCopy);
                }
            }
        }

        private void InsertFeatures(SQLiteConnection conn, long fkOutlineID, List<Feature> features)
        {
            if (features != null)
            {
                foreach (var f in features)
                {
                    var featureCopy = f;
                    InsertFeature(conn, fkOutlineID, ref featureCopy);
                }
            }
        }

        // *****************************************************************************
        //
        // Inserts list of DBPoint's into Points table
        //
        private void InsertPoints(SQLiteConnection conn, List<DBPoint> points)
        {
            foreach (var p in points)
            {
                var pointCopy = p;
                InsertPoint(conn, ref pointCopy);
            }
        }

        // *****************************************************************************
        //
        // Inserts list of DBImageModification's into ImageModifications table
        //
        private void InsertImageModifications(SQLiteConnection conn, List<DBImageModification> imagemods)
        {
            foreach (var i in imagemods)
            {
                var modCopy = i;
                InsertImageModification(conn, ref modCopy);
            }
        }

        // *****************************************************************************
        //
        // Updates outline in Outlines table  
        //
        private void UpdateOutline(SQLiteConnection conn, DBOutline outline)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE Outlines SET " +
                    "Scale = @Scale, " +
                    "TipPosition = @TipPosition, " +
                    "BeginLE = @BeginLE, " +
                    "EndLE = @EndLE, " +
                    "NotchPosition = @NotchPosition, " +
                    "fkIndividualID = @fkIndividualID " +
                    "WHERE ID = @ID";

                cmd.Parameters.AddWithValue("@Scale", outline.scale);
                cmd.Parameters.AddWithValue("@TipPosition", outline.tipposition);
                cmd.Parameters.AddWithValue("@BeginLE", outline.beginle);
                cmd.Parameters.AddWithValue("@EndLE", outline.endle);
                cmd.Parameters.AddWithValue("@NotchPosition", outline.notchposition);
                cmd.Parameters.AddWithValue("@fkImageID", outline.fkimageid);
                cmd.Parameters.AddWithValue("@ID", outline.id);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Updates row in DamageCategories table using given DBDamageCategory struct.
        // Uses ID field for identifying row.
        //
        private void UpdateDamageCategory(SQLiteConnection conn, Category damagecategory)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE DamageCategories SET " +
                    "Name = @Name, " +
                    "OrderID = @OrderID " +
                    "WHERE ID = @ID";

                cmd.Parameters.AddWithValue("@Name", damagecategory.Name);
                cmd.Parameters.AddWithValue("@OrderID", damagecategory.Order);
                cmd.Parameters.AddWithValue("@ID", damagecategory.ID);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Updates row in Individuals table using given DBIndividual struct.  Uses ID
        // field for identifying row.
        //
        private void UpdateDBIndividual(SQLiteConnection conn, DBIndividual individual)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE Individuals SET " +
                    "IDCode = @IDCode, " +
                    "Name = @Name, " +
                    "fkDamageCategoryID = @fkDamageCategoryID, " +
                    "ThumbnailFilename = @ThumbnailFilename " +
                    "WHERE ID = @ID";

                cmd.Parameters.AddWithValue("@IDCode", individual.idcode);
                cmd.Parameters.AddWithValue("@Name", individual.name);
                cmd.Parameters.AddWithValue("@fkDamageCategoryID", individual.fkdamagecategoryid);
                cmd.Parameters.AddWithValue("@ThumbnailFilename", individual.ThumbnailFilename);

                cmd.Parameters.AddWithValue("@ID", individual.id);

                cmd.ExecuteNonQuery();
            }
        }
        // *****************************************************************************
        //
        // Updates row in Images table using given DBImage struct.  Uses ID
        // field for identifying row.
        //
        private void UpdateImage(SQLiteConnection conn, DatabaseImage image)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE Images SET " +
                    "ImageFilename = @ImageFilename, " +
                    "OriginalImageFilename = @OriginalImageFilename, " +
                    "DateOfSighting = @DateOfSighting, " +
                    "RollAndFrame = @RollAndFrame, " +
                    "LocationCode = @LocationCode, " +
                    "ShortDescription = @ShortDescription, " +
                    "fkIndividualID = @fkIndividualID, " +
                    "CropImageFilename = @CropImageFilename, " +
                    "OrderId = @OrderId " +
                    "WHERE ID = @ID";

                cmd.Parameters.AddWithValue("@ImageFilename", image.ImageFilename);
                cmd.Parameters.AddWithValue("@OriginalImageFilename", image.OriginalImageFilename);
                cmd.Parameters.AddWithValue("@DateOfSighting", image.DateOfSighting);
                cmd.Parameters.AddWithValue("@RollAndFrame", image.RollAndFrame);
                cmd.Parameters.AddWithValue("@LocationCode", image.LocationCode);
                cmd.Parameters.AddWithValue("@ShortDescription", image.ShortDescription);
                cmd.Parameters.AddWithValue("@CropImageFilename", image.CropImageFilename);
                cmd.Parameters.AddWithValue("@Order", image.OrderId);
                cmd.Parameters.AddWithValue("@fkIndividualID", image.IndividualId);
                cmd.Parameters.AddWithValue("@ID", image.ID);

                cmd.ExecuteNonQuery();
            }

            DeleteImageModifications(conn, image.ID);
            InsertImageModifications(conn, image.ID, image.ImageMods);
        }

        // *****************************************************************************
        //
        // Updates row in ImageModifications table using given DBImageModification
        // struct.  Uses ID field for identifying row.
        //
        private void UpdateImageModification(SQLiteConnection conn, DBImageModification imagemod)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE ImageModifications SET " +
                    "Operation = @Operation, " +
                    "Value1 = @Value1, " +
                    "Value2 = @Value2, " +
                    "Value3 = @Value3, " +
                    "Value4 = @Value4, " +
                    "OrderID = @OrderID, " +
                    "fkImageID = @fkImageID " +
                    "WHERE ID = @ID";

                cmd.Parameters.AddWithValue("@Operation", imagemod.operation);
                cmd.Parameters.AddWithValue("@Value1", imagemod.value1);
                cmd.Parameters.AddWithValue("@Value2", imagemod.value2);
                cmd.Parameters.AddWithValue("@Value3", imagemod.value3);
                cmd.Parameters.AddWithValue("@Value4", imagemod.value4);
                cmd.Parameters.AddWithValue("@OrderID", imagemod.orderid);
                cmd.Parameters.AddWithValue("@fkImageID", imagemod.fkimageid);
                cmd.Parameters.AddWithValue("@ID", imagemod.id);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Updates row in Thumbnails table using given DBThumbnail
        // struct.  Uses ID field for identifying row.
        //
        private void UpdateThumbnail(SQLiteConnection conn, DBThumbnail thumbnail)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE Thumbnails SET " +
                    "Rows = @Rows, " +
                    "Pixmap = @Pixmap, " +
                    "fkImageID = @fkImageID " +
                    "WHERE ID = @ID";

                cmd.Parameters.AddWithValue("@Rows", thumbnail.rows);
                cmd.Parameters.AddWithValue("@Pixmap", thumbnail.pixmap);
                cmd.Parameters.AddWithValue("@fkImageID", thumbnail.fkimageid);
                cmd.Parameters.AddWithValue("@ID", thumbnail.id);

                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateSetting(SQLiteConnection conn, string key, string value)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "UPDATE Settings SET " +
                    "Value = @Value " +
                    "WHERE Key = @Key";

                cmd.Parameters.AddWithValue("@Value", value);
                cmd.Parameters.AddWithValue("@Key", key);

                cmd.ExecuteNonQuery();
            }
        }

        private void DeleteOutlineFeaturePointsByOutlineID(SQLiteConnection conn, long fkOutlineID)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM OutlineFeaturePoints WHERE fkOutlineID = @ID";
                cmd.Parameters.AddWithValue("@ID", fkOutlineID);

                cmd.ExecuteNonQuery();
            }
        }

        private void DeleteCoordinateFeaturePointsByIndividualID(SQLiteConnection conn, long fkOutlineID)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM CoordinateFeaturePoints WHERE fkOutlineID = @ID";
                cmd.Parameters.AddWithValue("@ID", fkOutlineID);

                cmd.ExecuteNonQuery();
            }
        }

        private void DeleteFeaturesIndividualID(SQLiteConnection conn, long fkOutlineID)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Features WHERE fkOutlineID = @ID";
                cmd.Parameters.AddWithValue("@ID", fkOutlineID);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Deletes set of points from Points table using fkOutlineID  
        //
        private void DeletePoints(SQLiteConnection conn, long fkOutlineID)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Points WHERE fkOutlineID = @ID";
                cmd.Parameters.AddWithValue("@ID", fkOutlineID);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Delete outline from Outlines table using fkIndividualID  
        //
        private void DeleteOutlineByFkImageID(SQLiteConnection conn, long fkImageID)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Outlines WHERE fkImageID = @ID";
                cmd.Parameters.AddWithValue("@ID", fkImageID);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Delete outline from Outlines table using id  
        //
        private void DeleteOutlineByID(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "DELETE FROM Outlines WHERE ID = @ID";
                    cmd.Parameters.AddWithValue("@ID", id);

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        // *****************************************************************************
        //
        // Delete individual from Individuals table using id  
        //
        private void DeleteIndividual(SQLiteConnection conn, long id)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Individuals WHERE ID = @ID";
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Delete damagecategory from DamageCategories table using id  
        //
        private void DeleteDamageCategory(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "DELETE FROM DamageCategories WHERE ID = @ID";
                    cmd.Parameters.AddWithValue("@ID", id);

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        // *****************************************************************************
        //
        // Delete image from Images table using id  
        //
        private void DeleteImage(SQLiteConnection conn, long id)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Images WHERE ID = @ID";
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Delete imagemod from ImageModifications table using id  
        //
        private void DeleteImageModification(SQLiteConnection conn, int id)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM ImageModifications WHERE ID = @ID";
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }

        private void DeleteImageModifications(SQLiteConnection conn, long fkImageID)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM ImageModifications WHERE fkImageID = @fkImageID";
                cmd.Parameters.AddWithValue("@fkImageID", fkImageID);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Delete thumbnail from Thumbnails table using id  
        //
        private void DeleteThumbnail(SQLiteConnection conn, int id)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Thumbnails WHERE ID = @ID";
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }

        // *****************************************************************************
        //
        // Delete thumbnail from Thumbnails table using fkImageID  
        //
        private void DeleteThumbnailByFkImageID(SQLiteConnection conn, long id)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "DELETE FROM Thumbnails WHERE fkImageID = @ID";
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }

        private void InvalidateAllFins()
        {
            _allFins = null;
        }

        public override void CreateEmptyDatabase(CatalogScheme catalogScheme)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                // SQL CREATE TABLE statements... might be better off defined in the header as a constant..
                string tableCreate = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT,
                        Value TEXT
                    );

                    CREATE TABLE IF NOT EXISTS DamageCategories (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                        OrderID INTEGER, 
                        Name TEXT);

                    CREATE TABLE IF NOT EXISTS Individuals (
                        ID INTEGER PRIMARY KEY,
                        IDCode TEXT,
                        Name TEXT,
                        ThumbnailFilename TEXT DEFAULT NULL,
                        fkDamageCategoryID INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS Images (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        fkIndividualID INTEGER,
                        ImageFilename TEXT,
                        OriginalImageFilename TEXT DEFAULT NULL,
                        CropImageFilename TEXT DEFAULT NULL,
                        DateOfSighting TEXT,
                        RollAndFrame TEXT,
                        LocationCode TEXT,
                        ShortDescription TEXT,
                        OrderId INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS ImageModifications ( 
                        ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                        Operation INTEGER, 
                        Value1 INTEGER, 
                        Value2 INTEGER, 
                        Value3 INTEGER, 
                        Value4 INTEGER, 
                        OrderID INTEGER, 
                        fkImageID INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS Thumbnails (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        fkImageID INTEGER,
                        Rows INTEGER, 
                        Pixmap TEXT 
                    );

                    CREATE TABLE IF NOT EXISTS Outlines (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Scale REAL DEFAULT NULL,
                        TipPosition INTEGER,
                        BeginLE INTEGER,
                        EndLE INTEGER,
                        NotchPosition INTEGER,
                        EndTE INTEGER,
                        fkImageID INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS OutlineFeaturePoints (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type INTEGER,
                        Position INTEGER,
                        UserSetPosition INTEGER,
                        Ignore INTEGER,
                        fkOutlineID INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS CoordinateFeaturePoints (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type INTEGER,
                        X INTEGER,
                        Y INTEGER,
                        UserSetPosition INTEGER,
                        Ignore INTEGER,
                        fkOutlineID INTEGER
                    );
                    CREATE INDEX IF NOT EXISTS IX_CoordinateFeaturePoints_fkOutlineID ON CoordinateFeaturePoints (fkOutlineID);

                    CREATE TABLE IF NOT EXISTS Features (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type INTEGER,
                        Value REAL,
                        Ignore INTEGER,
                        fkOutlineID INTEGER
                    );
                    CREATE INDEX IF NOT EXISTS IX_Features_fkOutlineID ON Features (fkOutlineID);

                    CREATE TABLE IF NOT EXISTS Points (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        XCoordinate REAL,
                        YCoordinate REAL,
                        fkOutlineID INTEGER,
                        OrderID INTEGER
                    );

                    CREATE INDEX IF NOT EXISTS IX_Settings_Key ON Settings (Key);

                    CREATE INDEX IF NOT EXISTS dmgcat_orderid ON DamageCategories (OrderID);

                    CREATE INDEX IF NOT EXISTS dmgcat_name ON DamageCategories (Name);

                    CREATE INDEX IF NOT EXISTS imgmod_img ON  ImageModifications (fkImageID);

                    CREATE INDEX IF NOT EXISTS img_indiv ON Images (fkIndividualID);

                    CREATE INDEX IF NOT EXISTS IX_Outline_fkImageID ON Outlines (fkImageID);

                    CREATE INDEX IF NOT EXISTS IX_OutlineFeaturePoints_fkOutlineID ON OutlineFeaturePoints (fkOutlineID);

                    CREATE INDEX IF NOT EXISTS pts_outln ON Points (fkOutlineID);

                    CREATE INDEX IF NOT EXISTS pts_order ON Points (OrderID);

                    CREATE INDEX IF NOT EXISTS pts_outln_order ON Points (fkOutlineID, OrderID);

                    CREATE INDEX IF NOT EXISTS thmbnl_img ON Thumbnails (fkImageID);";

                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    var transaction = conn.BeginTransaction();

                    cmd.CommandText = tableCreate;
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                }

                // Set the DB versioning to the latest
                SetVersion(conn, LatestDBVersion);

                // At this point, the Database class already contains the catalog scheme 
                // specification.  It was set in the Database(...) constructor from 
                // a CatalogScheme passed into the SQLiteDatabase constructor - JHS

                SaveCatalogScheme(conn, catalogScheme);

                conn.Close();
            }
        }

        #region Versioning and Updates
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        private void SetVersion(SQLiteConnection conn, long version)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "PRAGMA user_version = " + version.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        private void CheckVersionAndUpgrade(SQLiteConnection conn)
        {
            long version = 0;
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "PRAGMA user_version;";
                var rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    version = (long)rdr[0];
                    rdr.Close();
                }
            }

            // Maybe this should be a little more generic, but just hardcoding version upgrades right now
            if (version < LatestDBVersion)
            {

                if (version < 2)
                    UpgradeToVersion2(conn);

                if (version < 3)
                    UpgradeToVersion3(conn);

                if (version < 4)
                    UpgradeToVersion4(conn);

                if (version < 5)
                    UpgradeToVersion5(conn);

                if (version < 6)
                    UpgradeToVersion6(conn);

                UpgradeToVersion7(conn);
            }
        }

        private void UpgradeToVersion2(SQLiteConnection conn)
        {
            try
            {
                const string AddScaleToOutlines = "ALTER TABLE Outlines ADD COLUMN Scale REAL DEFAULT NULL";

                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = AddScaleToOutlines;
                    cmd.ExecuteNonQuery();
                }

                const string AddOriginalImageFilename = "ALTER TABLE Images ADD COLUMN OriginalImageFilename TEXT DEFAULT NULL";
                using (var cmd2 = new SQLiteCommand(conn))
                {
                    cmd2.CommandText = AddOriginalImageFilename;
                    cmd2.ExecuteNonQuery();
                }

                SetVersion(conn, 2);
            }
            catch { }
        }

        private void UpgradeToVersion3(SQLiteConnection conn)
        {
            try
            {
                const string CreateFeaturePointsTable = @"CREATE TABLE IF NOT EXISTS OutlineFeaturePoints(
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type INTEGER,
                        Position INTEGER,
                        UserSetPosition INTEGER,
                        fkOutlineID INTEGER
                    );
                    CREATE INDEX IF NOT EXISTS IX_OutlineFeaturePoints_fkOutlineID ON OutlineFeaturePoints (fkOutlineID);";

                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = CreateFeaturePointsTable;
                    cmd.ExecuteNonQuery();
                }

                SetVersion(conn, 3);
            }
            catch { }
        }

        private void UpgradeToVersion4(SQLiteConnection conn)
        {
            try
            {
                const string CreateSettingsTable = @"CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT,
                        Value TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_Settings_Key ON Settings (Key);";

                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = CreateSettingsTable;
                    cmd.ExecuteNonQuery();
                }

                SetVersion(conn, 4);
            }
            catch { }
        }

        private void UpgradeToVersion5(SQLiteConnection conn)
        {
            try
            {
                const string AlterOutlineFeaturePoints = @"ALTER TABLE OutlineFeaturePoints ADD COLUMN Ignore INTEGER DEFAULT 0";

                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = AlterOutlineFeaturePoints;
                    cmd.ExecuteNonQuery();
                }

                SetVersion(conn, 5);
            }
            catch { }
        }

        private void UpgradeToVersion6(SQLiteConnection conn)
        {
            try
            {
                const string AddFeatureTables = @"CREATE TABLE IF NOT EXISTS CoordinateFeaturePoints (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type INTEGER,
                        X INTEGER,
                        Y INTEGER,
                        UserSetPosition INTEGER,
                        Ignore INTEGER,
                        fkOutlineID INTEGER
                    );
                    CREATE INDEX IF NOT EXISTS IX_CoordinateFeaturePoints_fkOutlineID ON CoordinateFeaturePoints (fkOutlineID);

                    CREATE TABLE IF NOT EXISTS Features (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type INTEGER,
                        Value REAL,
                        Ignore INTEGER,
                        fkOutlineID INTEGER
                    );
                    CREATE INDEX IF NOT EXISTS IX_Features_fkOutlineID ON Features (fkOutlineID);";

                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = AddFeatureTables;
                    cmd.ExecuteNonQuery();
                }

                SetVersion(conn, 6);
            }
            catch { }
        }

        private void UpgradeToVersion7(SQLiteConnection conn)
        {
            try
            {
                using (var transaction = conn.BeginTransaction())
                {
                    const string Version7Updates =
                    @"ALTER TABLE Individuals ADD COLUMN ThumbnailFilename TEXT DEFAULT NULL;

                    ALTER TABLE Images ADD COLUMN CropImageFilename TEXT DEFAULT NULL;
                    ALTER TABLE Images ADD COLUMN OrderId INTEGER DEFAULT 100;

                    ALTER TABLE Outlines ADD COLUMN fkImageID INTEGER DEFAULT 0;";

                    using (var cmd = new SQLiteCommand(conn))
                    {
                        cmd.CommandText = Version7Updates;
                        cmd.ExecuteNonQuery();
                    }

                    // Now, update fkImageID in Outlines so it's the right ID, because we're going to drop fkIndividualID next
                    const string UpdateOutlineImageIds = "UPDATE Outlines SET fkImageID = (SELECT i.ID FROM Images i WHERE i.fkIndividualID = Outlines.fkIndividualID LIMIT 1)";
                    using (var cmdImageIds = new SQLiteCommand(conn))
                    {
                        cmdImageIds.CommandText = UpdateOutlineImageIds;
                        cmdImageIds.ExecuteNonQuery();
                    }

                    // And update the thumbnails
                    const string UpdateIndividualThumbnails = "UPDATE Individuals SET ThumbnailFilename = (SELECT i.ImageFilename FROM Images i WHERE i.fkIndividualID = Individuals.ID LIMIT 1)";
                    using (var cmdThumbnails = new SQLiteCommand(conn))
                    {
                        cmdThumbnails.CommandText = UpdateIndividualThumbnails;
                        cmdThumbnails.ExecuteNonQuery();
                    }


                    const string createCopy = @"CREATE TABLE IF NOT EXISTS OutlinesCopy (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Scale REAL DEFAULT NULL,
                        TipPosition INTEGER,
                        BeginLE INTEGER,
                        EndLE INTEGER,
                        NotchPosition INTEGER,
                        EndTE INTEGER,
                        fkImageID INTEGER
                    );

                    INSERT INTO OutlinesCopy(ID, Scale, TipPosition, BeginLE, EndLE, NotchPosition, EndTE, fkImageID)
                        SELECT ID, Scale, TipPosition, BeginLE, EndLE, NotchPosition, EndTE, fkImageID
                        FROM Outlines;";
                    using (var cmdOutlines1 = new SQLiteCommand(conn))
                    {
                        cmdOutlines1.CommandText = createCopy;
                        cmdOutlines1.ExecuteNonQuery();
                    }

                    const string CleanupOutlinesTable2 = @"CREATE INDEX IF NOT EXISTS IX_Outline_fkImageID ON Outlines (fkImageID);";

                    using (var cmdOutlines2 = new SQLiteCommand(conn))
                    {
                        cmdOutlines2.CommandText = CleanupOutlinesTable2;
                        cmdOutlines2.ExecuteNonQuery();
                    }

                    const string dropAndRename = @"
                        DROP TABLE Outlines;

                        ALTER TABLE OutlinesCopy RENAME TO Outlines;";

                    using (var cmdOutlines3 = new SQLiteCommand(conn))
                    {
                        cmdOutlines3.CommandText = dropAndRename;
                        cmdOutlines3.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }

                SetVersion(conn, 7);
            }
            catch { }
        }

        #endregion
    }
}
 
 