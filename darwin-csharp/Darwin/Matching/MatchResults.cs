﻿//                                            *
//   file: MatchResults.h
//
// author: Adam Russell
//
//   mods: J H Stewman (2006 & 2007)
//
//                                            *

using Darwin.Database;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Darwin.Matching
{
    public enum MatchResultSortType
    {
        MR_ERROR,
        MR_NAME,
        MR_IDCODE,
        MR_DAMAGE,
        MR_DATE,
        MR_LOCATION
    };

    public class MatchResults
    {
        public List<Result> Results { get; set; }

        public MatchResults()
        {
            Results = new List<Result>();
            mLastSortBy = MatchResultSortType.MR_ERROR;
            mTimeTaken = -1.00f;
            _finID = string.Empty;
            TracedFinFile = string.Empty;
            DatabaseFile = string.Empty;
        }

        public MatchResults(string id)
        {
            Results = new List<Result>();
            mLastSortBy = MatchResultSortType.MR_ERROR;
            mTimeTaken = -1.00f;
            _finID = id;
            TracedFinFile = string.Empty;
            DatabaseFile = string.Empty;
        }

        public MatchResults(string id, string tracedFinFile, string databaseFile)
        {
            Results = new List<Result>();
            mLastSortBy = MatchResultSortType.MR_ERROR;
            mTimeTaken = -1.00f;
            _finID = id;
            TracedFinFile = tracedFinFile;
            DatabaseFile = databaseFile;
        }

        //  008OL -- MatchResultsWindow calls constructor of this type and none existed
        public MatchResults(MatchResults results)
        {
            mLastSortBy = results.mLastSortBy;
            mTimeTaken = results.mTimeTaken;
            _finID = results._finID;
            // TODO: This should probably clone
            Results = results.Results;
            TracedFinFile = results.TracedFinFile;
            DatabaseFile = results.DatabaseFile;
        }

        public void AddResult(Result r)
        {
            if (Results == null)
                Results = new List<Result>();

            Results.Add(r);
        }

        public int Count
        {
            get
            {
                if (Results == null)
                    return 0;

                return Results.Count;
            }
        }

        public string TracedFinFile { get; set; }
        public string DatabaseFile { get; set; }

        public int size()
        {
            if (Results == null)
                return 0;

            return Results.Count;
        }

        // sort assumes sorting by last
        public void Sort()
        {
            //int dummy = 0; Sort(mLastSortBy, ref dummy);
            // Sort by error
            //TODO: This is a little ugly
            Results.Sort((x, y) => x.Error.CompareTo(y.Error));

            SetRankings();
        }

        public void Sort(MatchResultSortType sortBy, ref int active)
        {
            throw new NotImplementedException();
            //TODO
            //try
            //{
            //	//***1.0 - keep track of where active result ends up and 
            //	// reset value of active so redisplay of lists and icons
            //	// has correct active result after sort
            //	int newActive = -1;

            //	mLastSortBy = sortBy;

            //	list<Result> sortedResults;

            //	//***1.0 - need list of indices since mResults list is erased as we go
            //	list<int> actIndex;
            //	for (int i = 0; i < mResults.size(); i++)
            //		actIndex.push_back(i);

            //	while (mResults.size() > 0)
            //	{
            //		list<Result>::iterator it = mResults.begin();
            //		list<Result>::iterator saveIt = it;

            //		list<int>::iterator actIt = actIndex.begin(); //***1.0
            //		list<int>::iterator actItLow = actIt;         //***1.0

            //		string lowest;
            //		float lowestNum;
            //		switch (sortBy)
            //		{
            //			case MR_ERROR:
            //				lowestNum = atof(it->getError().c_str());
            //				break;
            //			case MR_NAME:
            //				lowest = it->getName();
            //				break;
            //			case MR_IDCODE:
            //				lowest = it->getIdCode();
            //				break;
            //			case MR_DAMAGE:
            //				lowest = it->getDamage();
            //				break;
            //			case MR_DATE:
            //				lowest = it->getDate();
            //				break;
            //			case MR_LOCATION:
            //				lowest = it->getLocation();
            //				break;
            //		}

            //		++it;

            //		++actIt; //***1.0

            //		while (it != mResults.end())
            //		{
            //			string compare;
            //			float compareNum;
            //			switch (sortBy)
            //			{
            //				case MR_ERROR:
            //					compareNum = atof(it->getError().c_str());
            //					break;
            //				case MR_NAME:
            //					compare = it->getName();
            //					break;
            //				case MR_IDCODE:
            //					compare = it->getIdCode();
            //					break;
            //				case MR_DAMAGE:
            //					compare = it->getDamage();
            //					break;
            //				case MR_DATE:
            //					compare = it->getDate();
            //					break;
            //				case MR_LOCATION:
            //					compare = it->getLocation();
            //					break;
            //			}

            //			if (sortBy == MR_ERROR && compareNum < lowestNum)
            //			{
            //				lowestNum = compareNum;
            //				saveIt = it;
            //				actItLow = actIt; //***1.0
            //			}
            //			else if (compare < lowest)
            //			{
            //				lowest = compare;
            //				saveIt = it;
            //				actItLow = actIt; //***1.0
            //			}

            //			++it;

            //			++actIt; //***1.0
            //		}

            //		sortedResults.push_back(*saveIt);
            //		mResults.erase(saveIt);

            //		//***1.0 - save new position of old active Result
            //		if (((*actItLow) == active) && (newActive == -1))
            //			newActive = sortedResults.size() - 1;
            //		actIndex.erase(actItLow);
            //	}

            //	mResults = sortedResults;

            //	active = newActive; //***1.0
            //}
            //catch (...) {
            //	throw;
            //}
        }

        //  1.5 - indicates whether sorted order is by error measure, so rank numbering is appropriate
        public bool LastSortedByError() //  1.5
        {
            return mLastSortBy == MatchResultSortType.MR_ERROR;
        }

        public void SetRankings() //  1.5
        {
            if (mLastSortBy != MatchResultSortType.MR_ERROR)
                return;

            int i = 1;
            foreach (var r in Results)
            {
                r.Rank = i;
                i++;
            }
        }

        // doesn't make a copy to save time... so DON'T DELETE
        // THE RESULT WHEN DONE
        public Result GetResultNum(int resultNum)
        {
            if (Results == null || resultNum < 0 || resultNum >= Results.Count)
                throw new ArgumentOutOfRangeException(nameof(resultNum));

            return Results[resultNum];
        }

        public void SetTimeTaken(float timeTaken)
        {
            mTimeTaken = timeTaken;
        }

        // getTimeTaken
        // 	A return of -1.00 indicates that the amount of
        // 	time is undefined.
        public float GetTimeTaken()
        {
            return mTimeTaken;
        }

        /// <summary>
        /// This will overwrite if the filename matches
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
                if (!string.IsNullOrEmpty(_finID))
                {
                    writer.WriteLine("Results for ID: " + (_finID ?? "NONE"));

                    writer.WriteLine("fin FILE: " + TracedFinFile);
                    writer.WriteLine(" db FILE: " + DatabaseFile);

                    int rank = FindRank();

                    if (rank == -1)
                        writer.WriteLine("ID does not match any in the results list.");
                    else
                        writer.WriteLine("The ID is ranked " + rank);
                }


                if (mTimeTaken > 0.0)
                    writer.WriteLine("Match Time: " + mTimeTaken + Environment.NewLine);

                writer.WriteLine(" Rank\tError\tID\tDBPosit\tunkBegin\tunkTip\tunkEnd\tdbBegin\tdbTip\tdbEnd\tDamage");
                writer.WriteLine("_____________________________________________________________________");

                for (int i = 0; i < Results.Count; i++)
                {
                    int
                        uBegin, uTip, uEnd,
                        dbBegin, dbTip, dbEnd;

                    Results[i].GetMappingControlPoints(out uBegin, out uTip, out uEnd, out dbBegin, out dbTip, out dbEnd);

                    writer.WriteLine("  " + (i + 1).ToString()
                        + "\t" + Results[i].Error.ToString("N2")
                        + "\t" + Results[i].IDCode
                        + "\t" + (Results[i].Position + 1) // Important -- match the way old Darwin does this
                        + "\t" + uBegin
                        + "\t" + uTip
                        + "\t" + uEnd
                        + "\t" + dbBegin
                        + "\t" + dbTip
                        + "\t" + dbEnd
                        + "\t" + Results[i].Damage);
                }
            }
        }

        public int FindRank()
        {
            for (int i = 0; i < Results.Count; i++)
            {
                Result r = GetResultNum(i);

                if (r.IDCode.ToLower() == _finID.ToLower())
                    return i + 1;
            }

            return -1;
        }

        //  1.1 - the following functions used in MatchQueue context

        public static MatchResults Load(string fileName, out DarwinDatabase db, out DatabaseFin databaseFin)
        {
            MatchResults result = new MatchResults();
            db = null;
            databaseFin = null;

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (!File.Exists(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            var lines = File.ReadAllLines(fileName);

            if (lines.Length < 6 || !lines[0].StartsWith("Results for ID:"))
                return result;

            result._finID = lines[0].Substring(lines[0].LastIndexOf(":") + 1).Trim();

            result.TracedFinFile = lines[1].Substring(lines[1].IndexOf(":") + 1).Trim();
            result.DatabaseFile = lines[2].Substring(lines[2].IndexOf(":") + 1).Trim();

            db = CatalogSupport.OpenDatabase(result.DatabaseFile, Options.CurrentUserOptions, false);

            //***2.2 - if we can, determine if database path has just changed drive letter
            // or some part of path that is a prefix to the SurveyArea.  If the database
            // is in the same SurveyArea and has the same database name, then we can
            // proceed with the building of the MatchResults
            //int p = mDatabaseFile.find("surveyAreas");
            //string rqdAreaAndDB = mDatabaseFile.substr(p);  // survey area and database name
            //string rqdPreamble = mDatabaseFile.substr(0, p);    // strip it to get preamble

            //string currentDBFile = db->getFilename();
            //p = currentDBFile.find("surveyAreas");
            //string currentAreaAndDB = currentDBFile.substr(p); // survey area and catalog name
            //string currentPreamble = currentDBFile.substr(0, p); // strip it to get preamble

            //if (db.Filename.ToLower() != DatabaseFile.ToLower())
            //{
            //    //cout << mDatabaseFile << endl;
            //    //cout << rqdPreamble << "   " << rqdAreaAndDB << endl;
            //    //cout << currentDBFile << endl;
            //    //cout << currentPreamble << "   " << currentAreaAndDB << endl;

            //    if (currentAreaAndDB == rqdAreaAndDB)
            //    {
            //        string msg = "The Survey Area and Catalog for the match results appear corrrect,\n";
            //        msg += "but the path to DARWIN's home folder seems to have changed.\n";
            //        msg += "Is it OK to open the indicated catalog in the current location\n";
            //        msg += "as shown below?\n\n";
            //        msg += (currentPreamble + rqdAreaAndDB);

            //        Trace.WriteLine(msg);
            //        //ErrorDialog *err = new ErrorDialog(msg);
            //        //err->show();

            //        //***2.2 - path possibly has to be fixed to FIN file as well
            //        if ((currentDBFile != mDatabaseFile) && (currentAreaAndDB == rqdAreaAndDB))
            //        {
            //            //add preamble of current DARWINHOME to FINs relative location
            //            p = mTracedFinFile.find("surveyAreas");
            //            string currentFinAreaPlus = mTracedFinFile.substr(p);
            //            mTracedFinFile = currentPreamble + currentFinAreaPlus;
            //            cout << mTracedFinFile << endl;
            //        }
            //    }
            //    else
            //    {
            //        string msg = "The WRONG database is currently loaded for viewing these results ...\n\n";
            //        msg += "LOADED DB:\n    " + db->getFilename() + "\n\n";
            //        msg += "REQUIRED DB:\n    " + mDatabaseFile + "\n\n";
            //        msg += "Please load the required database from the main window\n";
            //        msg += "and then reload the desired results file.";

            //        //ErrorDialog *err = new ErrorDialog(msg);
            //        //err->show();
            //        //***2.22 - replacing own ErrorDialog with GtkMessageDialogs
            //        GtkWidget* errd = gtk_message_dialog_new(NULL,
            //                                GTK_DIALOG_DESTROY_WITH_PARENT,
            //                                GTK_MESSAGE_ERROR,
            //                                GTK_BUTTONS_CLOSE,
            //                                msg.c_str());
            //        gtk_dialog_run(GTK_DIALOG(errd));
            //        gtk_widget_destroy(errd);
            //        return NULL;
            //    }
            //}

            DatabaseFin unkFin = CatalogSupport.OpenFinz(result.TracedFinFile);

            // get match info on each matched database fin
            // After skipping some of the headers
            for (int i = 6; i < lines.Length; i++)
            {
                string line = lines[i];

                int pos = line.IndexOf("\t");
                //string rank = line.Substring(0, pos);
                line = line.Substring(pos + 1);

                pos = line.IndexOf("\t");
                string error = line.Substring(0, pos);
                line = line.Substring(pos + 1);

                pos = line.IndexOf("\t");
                string dbFinID = line.Substring(0, pos);
                line = line.Substring(pos + 1);

                //cout << "dbFinID[" << dbFinID << "]"; //*** 2.2 - show for now

                string numStr;
                int
                    dbFinPosition,
                    uBegin, uTip, uEnd,
                    dbBegin, dbTip, dbEnd;

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                line = line.Substring(pos + 1);
                dbFinPosition = int.Parse(numStr);
                //cout << "[" << dbFinPosition << "]" << endl; //*** 2.2 - show for now 

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                uBegin = int.Parse(numStr);
                line = line.Substring(pos + 1);
                //cout << "[" << uBegin << "]";

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                uTip = int.Parse(numStr);
                line = line.Substring(pos + 1);
                //cout << "[" << uTip << "]";

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                uEnd = int.Parse(numStr);
                line = line.Substring(pos + 1);
                //cout << "[" << uEnd << "]";

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                dbBegin = int.Parse(numStr);
                line = line.Substring(pos + 1);
                //cout << "[" << dbBegin << "]";

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                dbTip = int.Parse(numStr);
                line = line.Substring(pos + 1);
                //cout << "[" << dbTip << "]";

                pos = line.IndexOf("\t");
                numStr = line.Substring(0, pos);
                dbEnd = int.Parse(numStr);
                line = line.Substring(pos + 1);
                //cout << "[" << dbEnd << "]";

                //string damage = line;
                //cout << "[" << damage << "]" << endl;

                // The position is written starting at 1, but our index is 0 based
                DatabaseFin thisDBFin = db.AllFins[dbFinPosition - 1];

                // TODO: Should this throw an exception instead?
                if (thisDBFin.IDCode != dbFinID)
                    Trace.WriteLine("Disaster " + thisDBFin.IDCode + " " + dbFinID);

                FloatContour mappedUnknownContour = unkFin.FinOutline.ChainPoints.MapContour(
                        unkFin.FinOutline.ChainPoints[uTip],
                        unkFin.FinOutline.ChainPoints[uBegin],
                        unkFin.FinOutline.ChainPoints[uEnd],
                        thisDBFin.FinOutline.ChainPoints[dbTip],
                        thisDBFin.FinOutline.ChainPoints[dbBegin],
                        thisDBFin.FinOutline.ChainPoints[dbEnd]);

                Result r = new Result(
                        mappedUnknownContour,                      //***1.3 - Mem Leak - constructor make copy now
                        thisDBFin.FinOutline.ChainPoints, //***1.3 - Mem Leak - constructor make copy now
                        thisDBFin.ImageFilename,
                        thisDBFin.ThumbnailFilenameUri,
                        dbFinPosition - 1, // position of fin in database
                        double.Parse(error),
                        thisDBFin.IDCode,
                        thisDBFin.Name,
                        thisDBFin.DamageCategory,
                        thisDBFin.DateOfSighting,
                        thisDBFin.LocationCode);

                r.SetMappingControlPoints(
                        uBegin, uTip, uEnd,  // beginning, tip & end of unknown fin
                        dbBegin, dbTip, dbEnd); // beginning, tip & end of database fin

                result.Results.Add(r);
            }

            databaseFin = unkFin;
            result.SetRankings();
            return result;
        }

        private MatchResultSortType mLastSortBy;
        private float mTimeTaken;
        private string _finID; // this is the unknown fin ID
    }
}