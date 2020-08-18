using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Model
{
    public class DatabaseImport : BaseEntity
    {
        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                RaisePropertyChanged("Date");
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

        public DatabaseImport()
        {

        }
    }
}
