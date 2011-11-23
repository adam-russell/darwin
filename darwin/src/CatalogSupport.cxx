// DatabaseSupport.cxx

#include "CatalogSupport.h"

#ifdef WIN32
#include <io.h>     //***1.982 - _findFirst()
#include <direct.h> //***1.982 - _mkdir()
#else
#include <sys/stat.h> //***2.22 - mkdir(), stat() Mac/UNIX 
#endif

#include <set>
#include <cctype>

using namespace std;

bool continueOverwrite(string winLabel, string message, string fileName)
{
	GtkWidget *dialog = gtk_dialog_new_with_buttons (
				winLabel.c_str(),
				NULL, // do not have parent
				(GtkDialogFlags)(GTK_DIALOG_MODAL | GTK_DIALOG_DESTROY_WITH_PARENT),
				GTK_STOCK_OK,
				GTK_RESPONSE_ACCEPT,
				GTK_STOCK_CANCEL,
				GTK_RESPONSE_REJECT,
				NULL);

	gtk_window_set_default_size(GTK_WINDOW(dialog), 300, 140);

	GtkWidget *label = gtk_label_new(message.c_str());
	gtk_container_add(GTK_CONTAINER(GTK_DIALOG(dialog)->vbox),label);
	gtk_widget_show(label);

	GtkWidget *entry = gtk_entry_new();
	gtk_entry_set_text(GTK_ENTRY(entry),fileName.c_str());
	gtk_container_add(GTK_CONTAINER(GTK_DIALOG(dialog)->vbox),entry);
	gtk_widget_show(entry);

	label = gtk_label_new("Replace this file?");
	gtk_container_add(GTK_CONTAINER(GTK_DIALOG(dialog)->vbox),label);
	gtk_widget_show(label);

	gint result = gtk_dialog_run (GTK_DIALOG (dialog));

	gtk_widget_destroy (dialog);

	return (result == GTK_RESPONSE_ACCEPT);
}

// The woCaseLesThan::operator() is used to compare strings without 
// distinguishing case differences.  Specifically, it is used in 
// backupDatabaseTo() to build set of uninque image filenames associated 
// with a particular *.db file being archived.

class woCaseLessThan {
public:
	bool operator() (const string &s1, const string &s2) const
	{ 
		string first(s1), second(s2);
		int i;
		for (i=0; i < first.length(); i++)
			first[i] = tolower(first[i]);
		for (i=0; i < second.length(); i++)
			second[i] = tolower(second[i]);
		return (first < second);
	}
};

/*
 *	Tries to instantiate a database object using the filename
 *	in the Options object.  As of right now, all new databases
 *  will be SQLite databases but old databases can be opened.
 *  
 *  In the future, if MySQL support is added, the filename
 *  could also be a URL and this method would instantiate
 *  the database type appropriately.
 *
 */
Database * openDatabase(Options *o, bool create)
{
	Database* db;
	CatalogScheme cat; // empty scheme by default

	if (create)
	{
		// should ONLY end up here with IFF we are NOT converting an old database
		int id = o->mCurrentDefaultCatalogScheme;
		cat.schemeName = o->mDefinedCatalogSchemeName[id];
		cat.categoryNames = o->mDefinedCatalogCategoryName[id]; // this is a vector
		db = new SQLiteDatabase(o, cat, create);
	}
	else if (SQLiteDatabase::isType(o->mDatabaseFileName))
	{
		// the catalog scheme will come out of the existing database

		db = new SQLiteDatabase(o, cat, create);
	}
	else if(OldDatabase::isType(o->mDatabaseFileName))
		db = openDatabase(NULL,o->mDatabaseFileName);
	else
	{
		DummyDatabase* dummy = new DummyDatabase(o, false);
		dummy->setStatus(Database::fileNotFound);
		db = dummy;
	} 

	return db;
}

//
// This version is called to open and return (possibly converting)
// an EXISTING database.
//
Database * openDatabase(MainWindow *mainWin, string filename)
{
	Database *db = NULL;
	
	Options o;	
	o.mDatabaseFileName = filename;
	o.mDarwinHome = gOptions->mDarwinHome;//getenv("DARWINHOME");

	CatalogScheme cat; // empty by default

	db_opentype_t opentype = databaseOpenType(filename);
	
	switch (opentype)
	{
	case cannotOpen:
			// notify user of problem
			g_print("Could not open selected database file!\n");
		break;
	case convert:
		{
			// convert the OldDatabase
			DBConvertDialog *cdlg = new DBConvertDialog(
					mainWin,
					filename,
					&db); // this is set to return converted/opened SQLDatabase
			cdlg->run_and_respond();
		}
		break;
	case canOpen:
			// open the SQLite database
			o.mDatabaseFileName = filename;
			o.mDarwinHome = gOptions->mDarwinHome;//getenv("DARWINHOME");
			db = new SQLiteDatabase(&o, cat, false);
		break;
	default:
		// nothing required, should NEVER end up here
		break;
	}

	if (NULL == db)
	{
		// either open database failed or we ended up in the default case above
		o.mDatabaseFileName = filename;
		o.mDarwinHome = gOptions->mDarwinHome;//getenv("DARWINHOME");
		db = new DummyDatabase(&o, false);
	}

	return db;
}

/*
 * Used to determine if the given file refers to:
 *     1) A file we can't read
 *     2) An unsupported database type that should
 *        be converted to a supported type
 *     3) A supported database
 */
db_opentype_t databaseOpenType(string filePath)
{
	db_opentype_t type = cannotOpen;

	if(SQLiteDatabase::isType(filePath))
		type = canOpen;

	else if(OldDatabase::isType(filePath))
		type = convert;

	return type;
}

/*
 * Used for converting between databases. Relies on the add()
 * method of the target database, and thus, most likely will
 * not create new damage categories.
 * ONLY WORKS FOR DATABASES WITH THE SAME DAMAGE CATEGORIES!!!
 *
 * The categories in the target database are now set correctly
 * prior to calling this function from duplicateDatabase() - JHS
*/
void copyFins(Database* from, Database *to)
{
	const unsigned size = from->sizeAbsolute(); //***2.01 - absolute

	for(unsigned i = 0; i < size; i++)
	{
		DatabaseFin<ColorImage>* fin = from->getItemAbsolute(i); //***2.01 - absolute
		if(fin != NULL) {
			to->add(fin);
			delete fin;
		}
	}
}

/*
 * Used to convert unsupported types to new SQLite databases.
 * IGNORES THE FILENAMES IN THE OPTIONS! (Decision made
 * as part of a move towards de-coupling the options and
 * the database filename and categories.
 *
 * Returns the new converted database.
 */
Database* convertDatabase(Options* o, string sourceFilename)
{
	/* We're moving the source file and then 
		creating the target with the old name
		*/
	string targetFilename = sourceFilename;

	// "file.db" -> "file.olddb"
	int pos = sourceFilename.rfind(".");

	//***2.01 - change so "old" is prepended to whatever file extension exists
	// ensuring that sourceFilename != newFilenameOfSourceDatabase in ALL cases
	string fileExt = sourceFilename.substr(pos+1);                                          //***2.01
	string newFilenameOfSourceDatabase = sourceFilename.substr(0, pos+1) + "old" + fileExt; //***2.01

	//string newFilenameOfSourceDatabase = sourceFilename.substr(0, pos); //***2.01 - replaced
	//newFilenameOfSourceDatabase += ".olddb";                            //***2.01 - replaced

	// move "file.db" "file.olddb" -- renames source file
#ifdef WIN32
	string mvCmd("move \"");
#else
	string mvCmd("mv \""); //***2.22 - for Mac
#endif
	mvCmd += sourceFilename;
	mvCmd += "\" \"";
	mvCmd += newFilenameOfSourceDatabase;
	mvCmd += "\"";
	system(mvCmd.c_str());

	/*
	 * open the old database -- needs to be opened first
	 * due to the side effects (category names) set in 
	 * the options.
	 */
	o->mDatabaseFileName = newFilenameOfSourceDatabase;
	Database* sourceDatabase = openDatabase(o, false);
	
	Database* targetDatabase = duplicateDatabase(o, sourceDatabase, targetFilename);
	
	sourceDatabase->closeStream();
	delete sourceDatabase;

	return targetDatabase;
}

/*
 * Used to duplicate the contents of a database to a new database.
 * IGNORES THE FILENAMES IN THE OPTIONS! (Decision made
 * as part of a move towards de-coupling the options and
 * the database filename and categories.
 *
 * Returns the new converted database.
 * 
 * This probably shouldn't be used directly until types other
 * than the SQLiteDatabase are supported.  The idea here is that
 * a type (SQLite vs MySQL) would be determined by the targetFilename
 * and thus this function is very generic.
 */
Database* duplicateDatabase(Options* o, Database* sourceDatabase, string targetFilename)
{
	o->mDatabaseFileName = targetFilename;
	//Database* targetDatabase = openDatabase(o, true);

	// we have already gone through openDatabase once at this point so call the 
	// constructor for the target database type directly
	Database* targetDatabase = new SQLiteDatabase(o, sourceDatabase->catalogScheme(), true);

	copyFins(sourceDatabase, targetDatabase);
	
	return targetDatabase;
}

/*
 * This makes a backup in the "DARWINHOME/backups" folder with
 * a name coprised of the SurveyArea name, catalog Database
 * filename, the current date, and a sequence number, if necessary.
 *
 * It assumes ONLY that the database (db) is open and complete.
 *
 */

bool backupCatalog(Database *db)
{
	cout << "\nCreating BACKUP of Database ...\n  " << db->getFilename() << endl;

	// create backup filename .. should we allow user to choose this?

	string shortName = db->getFilename();
	shortName = shortName.substr(1+shortName.rfind(PATH_SLASH));
	shortName = shortName.substr(0,shortName.rfind(".db")); // just root name of DB file

	string shortArea = db->getFilename();
	shortArea = shortArea.substr(0,shortArea.rfind(PATH_SLASH));
	shortArea = shortArea.substr(0,shortArea.rfind(PATH_SLASH));
	shortArea = shortArea.substr(1+shortArea.rfind(PATH_SLASH)); // just the area name

	shortName = shortArea + "_" + shortName; // merge two name parts

	// now append the date & time
	shortName = shortName.substr(0,shortName.rfind(".db"));
	time_t ltime;
	time( &ltime );
	tm *today = localtime( &ltime );
	char buffer[128];
	strftime(buffer,128,"_%b_%d_%Y",today); // month name, day & year
	shortName += buffer;
	// and append ".zip"
	shortName += ".zip";
	
	string backupPath, backupFilename, fileList, command;

	//backupPath = gOptions->mDarwinHome;//getenv("DARWINHOME");
	backupPath = gOptions->mCurrentDataPath; //***2.22
	backupPath = backupPath 
		+ PATH_SLASH 
		+ "backups" 
		+ PATH_SLASH;
	backupFilename = backupPath + shortName;

	// find out if archive already exists, and if so, append a suffix to backup
	ifstream testFile(backupFilename.c_str());
	if (! testFile.fail())
	{
		testFile.close();
		backupFilename = backupFilename.substr(0,backupFilename.rfind(".zip")); // strip ".zip"
		char suffix[16];
		int i=2;
		bool done=false;
		while (! done)
		{
			sprintf(suffix,"[%d]",i);
			testFile.open((backupFilename + suffix + ".zip").c_str());
			if (testFile.fail())
				done = true;
			else
			{
				testFile.close();
				i++;
			}
		}
		backupFilename = backupFilename + suffix + ".zip";
	}

	return createArchive(db, backupFilename);
}


//***2.22 - new function to support multiple data areas OUTSIDE DARWINHOME
//          dataPath should terminate in "darwinData" and may be anywhere
//
bool dataPathExists (string dataPath, bool forceCreate)
{
	bool exists = true; // assume true -- dataPath does exist

	string 
		areas = dataPath + PATH_SLASH + "surveyAreas",
		backups = dataPath + PATH_SLASH + "backups";

#ifdef WIN32

	struct _finddata_t c_file;
	long hFile;

	if (forceCreate)
		printf( "Creating Data Path folder(s) ...\n" );

	// Find or create dataPath
	if (hFile = _findfirst(dataPath.c_str(), &c_file ) == -1L)
	{
		if (forceCreate)
			_mkdir(dataPath.c_str());
		else
			exists = false;
	}
	_findclose( hFile );
		
	// Find or create dataPath/surveyAreas
	if (exists)
	{
		if (hFile = _findfirst(areas.c_str(), &c_file ) == -1L)
		{
			if (forceCreate)
				_mkdir(areas.c_str());
			else
				exists = false;
		}
		_findclose( hFile );
	}
	
	// Find or create dataPath/backups
	if (exists)
	{
		if (hFile = _findfirst(backups.c_str(), &c_file ) == -1L)
		{
			if (forceCreate)
				_mkdir(backups.c_str());
			else
				exists = false;
		}
		_findclose( hFile );
	}

#else // Mac & Linux

	struct stat buffer;
	int         status;

	// status = stat("/home/cnd/mod1", &buffer); -- this is the example

	// Find dataPath
	if ( (status = stat(dataPath.c_str(), &buffer)) == -1L )
	{
		if (forceCreate)
			mkdir(dataPath.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		else
			exists = false;
	}

	if (exists)
	{
		if ( (status = stat(areas.c_str(), &buffer)) == -1L )
		{
			if (forceCreate)
				mkdir(areas.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
			else
				exists = false;
		}
	}

	if (exists)
	{
		if ( (status = stat(backups.c_str(), &buffer)) == -1L )
		{
			if (forceCreate)
				mkdir(backups.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
			else
				exists = false;
		}
	}

#endif

	return exists;
}


//***2.22 - new function to see if file or folder exists without opening file
//          as done in utility.h (fileExists(fname)
//
bool filespecFound(string path)
{
	bool found(true);

#ifdef WIN32
	struct _finddata_t c_file;
	long hFile;

	if( (hFile = _findfirst(path.c_str(), &c_file )) == -1L )
		found = false;
	_findclose( hFile );
#else
	struct stat buffer;
	int         status;

	if( (status = stat(path.c_str(), &buffer)) == -1L )
		found = false;
#endif
	return found;
}

//***2.22 - new data move function for first time run of DARWIN
//
// This is called the FIRST time DARWIN runs.  It moves the surveyAreas
// and backups folders from indside the Application (DARWINHOME) to the
// gOptions->mCurrentDataPath folder.  It checks to make sure nothing
// that exists is overwritten, just in case this is an upgrade and the
// normal installer was used.  The parameter force indicates whether or
// not to blindly overwrite the destination.
//
void moveAreasAndBackups(bool force)
{
	string 
		home = gOptions->mDarwinHome,
		pathTo = gOptions->mCurrentDataPath,
		areas = home + PATH_SLASH + "surveyAreas",
		backups = home + PATH_SLASH + "backups",
		copy,
		command;

	if (force) 
	{				
		// create the new data path folder and move the entire surveyAreas
		// folder from the Application (DARWINHOME) to the data path folder

#ifdef WIN32

		_mkdir(pathTo.c_str()); // create the data path folder
		copy = "move /Y \"";

#else //***2.22 - new section for Mac / UNIX ( with permissions 777)

		mkdir(pathTo.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		copy = "mv -f \"";

#endif

		command = copy + areas + "\" \"";
		command += pathTo + "\"";
		system(command.c_str()); // copy survey areas out of application

		command = copy + backups + "\" \"";
		command += pathTo + "\"";
		system(command.c_str()); // copy backups out of application

	}
	else // do not force OVERWRITE
	{
		// unsure what to do here -- data path probably exists, 
		// so assume files are there and should not be replaced?

		// should surveyAreas and backups be REMOVED from application?
	}
}


//
// This builds/rebuilds the folder structure of a Survey Area as needed
// during restore and import catalog operations.  The force parameter
// indicates whether overwriting of folders is forced.
//
void rebuildFolders(string home, string area, bool force)
{
	string path;

	if (force) // IMPORT and NEW SURVEY AREA
	{				
		// create the entire folder structure for a new SurveyArea

		cout << "Creating folders ..." << endl;

		path = home + PATH_SLASH + "surveyAreas" + PATH_SLASH + area;
#ifdef WIN32
		_mkdir(path.c_str());
		path += PATH_SLASH;
		_mkdir(path.c_str());
		_mkdir((path+"catalog").c_str());
		_mkdir((path+"tracedFins").c_str());
		_mkdir((path+"matchQueues").c_str());
		_mkdir((path+"matchQResults").c_str());
		_mkdir((path+"sightings").c_str());
#else //***2.22 - new section for Mac / UNIX ( with permissions 777)
		mkdir(path.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		path += PATH_SLASH;
		mkdir(path.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		mkdir((path+"catalog").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		mkdir((path+"tracedFins").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		mkdir((path+"matchQueues").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		mkdir((path+"matchQResults").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		mkdir((path+"sightings").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
#endif
	}
	else // RESTORE
	{
		// determine whether the file folder structure is intact for this
		// database location ... ensure existance of surveyAreas\<restoreSurveyArea> and
		// its subfolders ...
		//    catalog, tracedFins, matchQueues, matchQResults, sightings
#ifdef WIN32
		struct _finddata_t c_file;
		long hFile;
		string path;

		// Find surveyArea\<restoreSurveyArea>
		path = home + PATH_SLASH + "surveyAreas";
		if( (hFile = _findfirst(path.c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"SurveyAreas\" folder ...\n" );
			_mkdir(path.c_str());
		}
		_findclose( hFile );
		path += PATH_SLASH + area;
		if( (hFile = _findfirst(path.c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"%s\" SurveyArea subfolder...\n", 
				area.c_str());
			_mkdir(path.c_str());
		}
		_findclose( hFile );
		// find subfolders and, if missing, fix them ...
		path += PATH_SLASH;
		if( (hFile = _findfirst((path+"catalog").c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"catalog\" folder ...\n" );
			_mkdir((path+"catalog").c_str());
		}
		_findclose( hFile );
		if( (hFile = _findfirst((path+"tracedFins").c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"tracedFins\" folder ...\n" );
			_mkdir((path+"tracedFins").c_str());
		}
		_findclose( hFile );
		if( (hFile = _findfirst((path+"matchQueues").c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"matchQueues\" folder ...\n" );
			_mkdir((path+"matchQueues").c_str());
		}
		_findclose( hFile );
		if( (hFile = _findfirst((path+"matchQResults").c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"matchQResults\" folder ...\n" );
			_mkdir((path+"matchQResults").c_str());
		}
		_findclose( hFile );
		if( (hFile = _findfirst((path+"sightings").c_str(), &c_file )) == -1L )
		{
			printf( "Creating missing \"sightings\" folder ...\n" );
			_mkdir((path+"sightings").c_str());
		}
#else //***2.22 - Mac / UNIX
		struct stat buffer;
		int         status;

		// status = stat("/home/cnd/mod1", &buffer); -- this is the example

		string path;

		// Find surveyArea\<restoreSurveyArea>
		path = home + PATH_SLASH + "surveyAreas";
		if( (status = stat(path.c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"SurveyAreas\" folder ...\n" );
			mkdir(path.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}

		path += PATH_SLASH + area;
		if( (status = stat(path.c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"%s\" SurveyArea subfolder...\n", 
				area.c_str());
			mkdir(path.c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}

		// find subfolders and, if missing, fix them ...
		path += PATH_SLASH;
		if( (status = stat((path+"catalog").c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"catalog\" folder ...\n" );
			mkdir((path+"catalog").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}
		if( (status = stat((path+"tracedFins").c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"tracedFins\" folder ...\n" );
			mkdir((path+"tracedFins").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}
		if( (status = stat((path+"matchQueues").c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"matchQueues\" folder ...\n" );
			mkdir((path+"matchQueues").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}
		if( (status = stat((path+"matchQResults").c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"matchQResults\" folder ...\n" );
			mkdir((path+"matchQResults").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}
		if( (status = stat((path+"sightings").c_str(), &buffer)) == -1L )
		{
			printf( "Creating missing \"sightings\" folder ...\n" );
			mkdir((path+"sightings").c_str(),(S_IRWXU | S_IRWXG | S_IRWXO));
		}
#endif
	}
}


//
// This performs file extraction from a zipped archive for both the 
// restoreDatabase() and importDatabaseFrom()
//
void extractCatalogFiles(string backupFilename, string toFolder)
{
	// extract database file from the archive

	// note: restorePath and backupFilename are NOT QUOTED!
	
	string command;

	command = "7z x -aoa -o"; // put in proper folder and force overwriting
	command += quoted(toFolder) + " ";
	command += quoted(backupFilename) + " *.db";

	system(command.c_str()); // start extraction process

	// extract images referenced from database file

	//***1.981a - extra quote removed
	command = "7z x -aos -o"; // extract into proper folder without overwriting
	command += quoted(toFolder) + " -x!filesToArchive.txt "; // but don't extract file list
	command += " -x!*.db "; // and don't extract database again
	command += quoted(backupFilename);

	system(command.c_str()); // start extraction process

}

//
// This handles restoration of a damaged Catalog from previous backup.
//
bool restoreCatalogFrom(string backupFilename, 
						string restorePath, 
						string restoreHome, 
						string restoreArea)
{
	// rebuild folder structure if it is compromised
	rebuildFolders(restoreHome, restoreArea, false); // DO NOT force overwite of folders

	// folder structure is OK, so now proceed with extraction ...
	extractCatalogFiles(backupFilename, restorePath);

	return true;
}


//
// This will be eventually simplified to use a common archiver
// since it is essentially the same process as a backup - JHS
//
bool exportCatalogTo(Database *db, Options *o, std::string filename)
{
	cout << "\nEXPORTING Database ...\n  " << db->getFilename() << endl;

	string exportFilename = filename;
				
	if (exportFilename.find(".zip") != (exportFilename.length() - 4))
		exportFilename += ".zip"; // archive MUST end in .zip - force it

	// find out if archive already exists, and if so, append a suffix to backup
	ifstream testFile(exportFilename.c_str());
	if (! testFile.fail())
	{
		testFile.close();

		bool doTheOverwrite = continueOverwrite(
				"REPLACE existing file?",
				"Selected EXPORT file already exists!",
				exportFilename);
		
		if (! doTheOverwrite)
		{
			cout << "EXPORT aborted - User refused REPLACEMENT of existing archive!" << endl;
			return false;
		}

		// delete the exising archive file
		string command = "DEL /Q \"" + exportFilename + "\" >nul";
		system(command.c_str());
	}
	
	return createArchive(db, exportFilename);
}

bool testFileExistsAndPrompt(string filename)
{
	ifstream testFile(filename.c_str());
	if (! testFile.fail())
	{
		testFile.close();

		bool doTheOverwrite = continueOverwrite(
				"REPLACE existing file?",
				"Selected EXPORT file already exists!",
				filename);
		
		if (! doTheOverwrite)
		{
			cout << "EXPORT aborted - User refused REPLACEMENT of existing archive!" << endl;
			return false;
		}

		// delete the exising archive file
#ifdef WIN32
		string command = "DEL /Q \"" + filename + "\" >nul";
#else
		string command = "rm -f \"" + filename + "\" >nul"; //***2.22 - for Mac
#endif
		system(command.c_str());
	}

	return true;
}

//
// This handles import of catalog into DARWIN from a zipped archive
//
bool importCatalogFrom(string backupFilename, 
						string importPath, 
						string importHome, 
						string importArea)
{
	// at this level this is the almost the same as a restore 

	// build folder structure (assumes this is a NEW Survey Area)
	rebuildFolders(importHome, importArea, true); // force building of folders
	
	// folder structure is OK, so now proceed with extraction ...
	extractCatalogFiles(backupFilename, importPath);

	return false;
}

//
// Contains the common code for creating a zipped archive of a catalog.
// This is used by both export and backup processes.
//
bool createArchive (Database *db, string filename)
{

	cout << "\nCollecting list of files comprising database ... \n\n  Please Wait." << endl;

	// build list of image names referenced from within database

	set<string,woCaseLessThan> imageNames;
	set<string,woCaseLessThan>::iterator it, oit;
	DatabaseFin<ColorImage> *fin;
	ImageFile<ColorImage> img;
	string catalogPath = db->getFilename();
	catalogPath = catalogPath.substr(0,catalogPath.rfind(PATH_SLASH)+1);
	int i;

	int limit = db->sizeAbsolute();

	for (i = 0; i < limit; i++)
	{
		fin = db->getItemAbsolute(i);

		if (NULL == fin)
			continue; // found a hole (previously deleted fin) in the database
		
		// if modified image filename not already in set, then add it

		it = imageNames.find(fin->mImageFilename);
		if (it == imageNames.end())
		{
			imageNames.insert(fin->mImageFilename);

			if (img.loadPNGcommentsOnly(fin->mImageFilename))
			{
				// if original image filename is not in set, then add it

				string origImageName = catalogPath + img.mOriginalImageFilename;
				oit = imageNames.find(origImageName);
				if (oit == imageNames.end())
					imageNames.insert(origImageName);

				// make sure fileds are empty for next image file read

				img.mImageMods.clear();
				img.mOriginalImageFilename = "";
			}
		}

		delete fin; // make sure to return storage
	}
	// end of code moved from above

	// put quotes around name
	string archiveFilename = "\"" + filename + "\"";
		
	string 
		command, 
		//fileList = gOptions->mDarwinHome;//getenv("DARWINHOME");
		fileList = gOptions->mTempDirectory; //***2.22 - move location to TEMP

	fileList = fileList 
		//+ PATH_SLASH 
		//+ "backups" 
		+ PATH_SLASH
		+ "filesToArchive.txt";

	string fileListQuoted = "\"" + fileList + "\"";

	cout << "\nARCHIVE filename is ...\n\n  " << filename << endl;

	// create the archive file using 7z compression program (Windows)

	command += "7z a -tzip ";
	command += archiveFilename + " @" + fileListQuoted;
			
	ofstream archiveListFile;
	archiveListFile.open(fileList.c_str());
	if (! archiveListFile.fail())
	{	
		archiveListFile <<  fileListQuoted << endl;
		archiveListFile << "\"" << db->getFilename() << "\"" << endl;  // use db here so it cannot be closed
		for (it = imageNames.begin(); it != imageNames.end(); ++it)
			archiveListFile << "\"" << (*it) << "\"" << endl;

		archiveListFile.close();
		db->closeStream();

		int error = system(command.c_str()); // start the archive process using 7-zip

		db->openStream();

		//***1.982 - remove "filesToArchive.txt"
#ifdef WIN32
		command = "del " + fileListQuoted;
#else
		command = "rm " + fileListQuoted;
#endif
		system(command.c_str());

		return (0 == error || 1 == error); //***2.01 - non-fatal (locked files not compressed is OK, too)
	}

	return false; // archive list could not be built
}


DatabaseFin<ColorImage>* openFin(string filename)
{
	return NULL;
}

//
// save old fin trace in old (multi-file) format
//

bool saveFin(DatabaseFin<ColorImage>* fin, string fileName)
{
	cout << "\nSAVING Fin ...\n  " << fileName << endl;
						
	//***1.4 - enforce ".fin" extension
		int posit = fileName.rfind(".fin");
		int shouldBe = (fileName.length() - 4);
		if (posit != shouldBe)
			fileName += ".fin";

	// find out if file already exists
	if(!testFileExistsAndPrompt(fileName))
		return false;

	// now actually save the file and images (modified and original)

	// copy unknown image to same folder as *.fin file will go

	string saveFolder = fileName.substr(0,fileName.rfind(PATH_SLASH));
	
	string copyfilename = saveImages(fin, saveFolder, fileName);	

	// DatabaseFin::save()  shortens the mImageFilename, so this call must
	// precede the setting of the mImagefilename to the path+filename
	// needed in the TraceWindow code if Add to Database is done after a
	// save of the fin trace
	try {
		fin->save(fileName);
	} catch (Error e) {
		showError(e.errorString());
	}

	fin->mImageFilename = copyfilename; //***1.8 - save this filename now

	return true;
}

string saveImages(DatabaseFin<ColorImage>* fin, string saveFolder, string fileName)
{
	string shortFilename = extractBasename(fin->mImageFilename);

	string copyFilename = saveFolder + PATH_SLASH + shortFilename;

	// copy image over into save folder

#ifdef WIN32
	string command = "copy \"";
#else
	string command = "cp \""; //***2.22 - for Mac
#endif
	command += fin->mImageFilename;
	command += "\" \"";
	command += copyFilename;
	command += "\"";

#ifdef DEBUG
	printf("copy command: \"%s\"",command.c_str());
#endif

	if (copyFilename != fin->mImageFilename) //***1.8 - prevent copy onto self
	{
		printf("copying \"%s\" to %s\n",shortFilename.c_str(),copyFilename.c_str());
		system(command.c_str());
				
		// ***1.8 - save path & name of copy of original image file
		fin->mOriginalImageFilename = copyFilename; // ***1.8
	}

	//***1.5 - save modified image alongside original
	if (NULL == fin->mModifiedFinImage)
		throw Error("Attempt to save Trace without modified image");

	// create filename - base this on FIN filename now
	int pos = fileName.find_last_of(PATH_SLASH);
	copyFilename = saveFolder + fileName.substr(pos); // includes slash
	pos = copyFilename.rfind(".fin");
	copyFilename = copyFilename.substr(0,pos);
	copyFilename += "_wDarwinMods.png"; //***1.8 - new file format
		
	fin->mModifiedFinImage->save_wMods( //***1.8 - new save modified image call
		copyFilename,    // the filename of the modified image
		shortFilename,   // the filename of the original image
		fin->mImageMods); // the list of image modifications
	
	// set image filename to path & filename of modified image so that name is 
	// saved as part of the DatabaseFin record in the file
	fin->mImageFilename = copyFilename; //***1.8 - save this filename now

	return copyFilename;
}

/*
 * Open Finz file for viewing
 */
DatabaseFin<ColorImage>* openFinz(string archive)
{

	string baseimgfilename;
	string tempdir("");
	tempdir += gOptions->mTempDirectory;//getenv("TEMP");
	tempdir += PATH_SLASH;
	tempdir += extractBasename(archive);

	systemUnzip(archive, tempdir);

	Options o = Options();
	o.mDatabaseFileName = tempdir + PATH_SLASH + "database.db";
	
	if(! SQLiteDatabase::isType(o.mDatabaseFileName) )
		return NULL;

	Database *db = openDatabase(&o, false);

	if(db->status() != Database::loaded)
	{
		delete db;
		return NULL;
	}

	DatabaseFin<ColorImage>* fin = db->getItem(0); // first and only fin
	

	// construct absolute file paths and open images
	baseimgfilename = extractBasename(fin->mImageFilename);
	fin->mImageFilename = tempdir + PATH_SLASH + baseimgfilename;
	fin->mModifiedFinImage = new ColorImage(fin->mImageFilename);
	fin->mOriginalImageFilename = tempdir + PATH_SLASH + extractBasename(fin->mModifiedFinImage->mOriginalImageFilename);
	
	if ("" != fin->mOriginalImageFilename)
	{
		fin->mFinImage = new ColorImage(fin->mOriginalImageFilename);
	}
	
	fin->mImageMods = fin->mModifiedFinImage->mImageMods;

	// fixes an issue with the MatchResults trying to re-save the fin
	fin->mFinFilename = archive;

	delete db;

	return fin;
}

/**
 * This method is designed to import a fin previously returned
 * by openFinz into the database.
 *
 * I make no assumptions about other contexts.
 *
 */
void importFin(Database* db, DatabaseFin<ColorImage>* fin)
{
	string dest, imgDest, origImgDest;

	dest = gOptions->mCurrentSurveyArea;
	dest += PATH_SLASH;
	dest += "catalog";
	dest += PATH_SLASH;

	imgDest = dest + extractBasename(fin->mImageFilename);

	systemCopy(fin->mImageFilename, imgDest);

	origImgDest = dest + extractBasename(fin->mOriginalImageFilename);

	systemCopy(fin->mOriginalImageFilename, origImgDest);

	db->add(fin);
}

/*
 * Saves a fin into a finz file
 */
bool saveFinz(DatabaseFin<ColorImage>* fin, string &archivePath) // return save status JHS
{
	DatabaseFin<ColorImage>* modFin;
	Options o;	
	CatalogScheme cat;
	string tempdir, cmd, baseFilename, src, dest;
	int pos;

	//***2.0 - appending .finz will also affect archivePath in caller - JHS
	//force extention .finz -- SAH
	if (archivePath.find(".finz")==string::npos)
		archivePath+=".finz";

	if(!testFileExistsAndPrompt(archivePath))
		return false;

	modFin = new DatabaseFin<ColorImage>(fin);


	baseFilename = extractBasename(archivePath);

	tempdir = gOptions->mTempDirectory;
	tempdir += PATH_SLASH;
	tempdir += baseFilename;

	// delete and re-make dir
	systemRmdir(tempdir);
	systemMkdir(tempdir);
	

	/*
	 * It seems we have two situations for the image mod list.
	 * If there is no modified image instantiated, then the mod list
	 * should come from the instantion of the modified image. If there
	 * is a modified image, it means that the mod list comes from
	 * fin->mImageMods
	 */
	if (modFin->mModifiedFinImage == NULL) {
		modFin->mModifiedFinImage = new ColorImage(modFin->mImageFilename);
		modFin->mImageMods = modFin->mModifiedFinImage->mImageMods;
	} else {
		modFin->mModifiedFinImage->mImageMods = modFin->mImageMods;
	}

	// Original Image should be in same folder as modified image
	if(modFin->mOriginalImageFilename == "")
		modFin->mOriginalImageFilename = extractPath(modFin->mImageFilename) + PATH_SLASH + extractBasename(modFin->mModifiedFinImage->mOriginalImageFilename);

	// copy over original image
	src = modFin->mOriginalImageFilename;
	dest = tempdir + PATH_SLASH + extractBasename(modFin->mOriginalImageFilename);
	systemCopy(src, dest);
	
	// set img path name as relative
	modFin->mOriginalImageFilename = extractBasename(modFin->mOriginalImageFilename);
	
	// replace ".finz" with "_wDarwinMods.png" for modified image filename
	pos = baseFilename.rfind(".");
	modFin->mImageFilename = tempdir + PATH_SLASH + baseFilename.substr(0,pos) + "_wDarwinMods.png";
	
	// save copy of modified image
	modFin->mModifiedFinImage->save_wMods(modFin->mImageFilename,
		modFin->mOriginalImageFilename,
		modFin->mImageMods);
	
	// set mod img path name as relative
	modFin->mImageFilename = extractBasename(modFin->mImageFilename);
	// also set the modified image filename of the fin passed into this function - JHS
	//fin->mImageFilename = modFin->mImageFilename;
	
	// create new database
	o.mDatabaseFileName = tempdir + PATH_SLASH + "database.db";
	cat.schemeName = "FinzSimple";
	if(fin->getDamage() != "NONE")
		cat.categoryNames.push_back("NONE");
	cat.categoryNames.push_back(fin->getDamage());
	
	SQLiteDatabase db(&o, cat, true);

	db.add(modFin);

	db.closeStream();

	// compress contents
	src = tempdir + PATH_SLASH + "*.*";

	systemZip(src, archivePath);

	delete modFin;

	return true;
}


// this returns true if file is OLD style fin file adn first 4 bytes
// are DFIN or NIFD (the magic number)
//
bool isTracedFinFile(string fileName)
{
	ifstream infile;
	char test[sizeof(unsigned long)+1];

	infile.open(fileName.c_str());

	if (!infile)
		return false;
	
	infile.read((char*)&test, sizeof(unsigned long));					
	infile.close();

	test[5] = '\0';

	return ((strncmp(test,"DFIN",4) == 0) || (strncmp(test,"NIFD",4) == 0));
}



