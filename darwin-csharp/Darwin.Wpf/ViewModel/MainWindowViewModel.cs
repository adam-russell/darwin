﻿using Darwin.Collections;
using Darwin.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Darwin.Wpf.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableNotifiableCollection<DatabaseFin> _fins;
        public ObservableNotifiableCollection<DatabaseFin> Fins
        {
            get
            {
                if (_fins == null)
                    _fins = new ObservableNotifiableCollection<DatabaseFin>();

                return _fins;
            }
            set
            {
                _fins = value;
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
