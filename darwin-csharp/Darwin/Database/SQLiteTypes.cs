﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Database
{
    public class DBIndividual
    {
        public int id;
        public string idcode;
        public string name; int fkdamagecategoryid;
    }

    public class DBDamageCategory
    {
        public int id;
        public int orderid;
        public string name;
    }
    public class DBImage
    {
        public int id;
        public int fkindividualid;
        public string imagefilename;
        public string dateofsighting;
        public string rollandframe;
        public string locationcode;
        public string shortdescription;
    }

    public class DBThumbnail
    {
        public int id;
        public int rows;
        public string pixmap;
        public int fkimageid;
    }
    public class DBOutline
    {
        public int id;
        public int tipposition;
        public int beginle;
        public int endle;
        public int notchposition;
        public int endte;
        public int fkindividualid;
    }

    public class DBPoint
    {
        public int id;
        public float xcoordinate;
        public float ycoordinate;
        public int fkoutlineid;
        public int orderid;
    }
    public class DBInfo
    {
        public string key;
        public string value;
    }
    public class DBImageModification
    {
        public int id;
        public int operation;
        public int value1;
        public int value2;
        public int value3;
        public int value4;
        public int orderid;
        public int fkimageid;
    }
}