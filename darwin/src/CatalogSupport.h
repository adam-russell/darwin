// CatalogSupport.h

#ifndef DATABASE_SUPPORT_H
#define DATABASE_SUPPORT_H

#include "interface/ErrorDialog.h"
#include "Database.h"
#include "SQLiteDatabase.h"
#include "OldDatabase.h"
#include "DummyDatabase.h"
#include "interface/MainWindow.h"
#include "interface/DBConvertDialog.h"
#include "utility.h"

typedef enum {
			cannotOpen = 0,
				canOpen,
				convert
	} db_opentype_t;

Database* openDatabase(Options *o, bool create);
Database * openDatabase(MainWindow *mainWin, string filename);
void copyFins(Database* from, Database *to);
db_opentype_t databaseOpenType(string filePath);
Database* convertDatabase(Options* o, string sourceFilename);
Database* duplicateDatabase(Options* o, Database* sourceDatabase, string targetFilename);

void rebuildFolders(std::string home, std::string area, bool force);
void extractCatalogFiles(std::string backupFilename, std::string toFollder);

bool backupCatalog(Database *db);
bool restoreCatalogFrom(std::string filename,
						std::string restorePath, 
						std::string restoreHome, 
						std::string restoreArea);
bool exportCatalogTo(Database *db, Options *o, std::string filename);
bool importCatalogFrom(string backupFilename, 
						string restorePath, 
						string restoreHome, 
						string restoreArea);

bool createArchive (Database *db, string filename); // creates zipped catalog
bool continueOverwrite(string winLabel, string message, string fileName);

DatabaseFin<ColorImage>* openFin(string filename);
bool saveFin(DatabaseFin<ColorImage>* fin, string filename);

DatabaseFin<ColorImage>* openFinz(string filename);
void saveFinz(DatabaseFin<ColorImage>* fin, string filename);

#endif