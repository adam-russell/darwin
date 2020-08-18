﻿/////////////////////////////////////////////////////////////////////
//   file: CatalogScheme.h
//
// author: J H Stewman
//
//   date: 7/18/2008
//
// new catalog scheme class - begin integrating with all code - JHS
//
/////////////////////////////////////////////////////////////////////

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

using Darwin.Features;
using Darwin.Model;
using Darwin.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Darwin.Database
{
    public class CatalogScheme : INotifyPropertyChanged
    {
        private string _schemeName;
		public string SchemeName
        {
            get => _schemeName;
            set
            {
                _schemeName = value;
                RaisePropertyChanged("SchemeName");
            }
        }

        private ObservableCollection<Category> _categories;
		public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                RaisePropertyChanged("Categories");
            }
        }

        private FeatureSetType _featureSetType;
        public FeatureSetType FeatureSetType
        {
            get => _featureSetType;
            set
            {
                _featureSetType = value;
                RaisePropertyChanged("FeatureSetType");
            }
        }

        [JsonIgnore]
        public string CollectionTerminology
        {
            get
            {
                switch (FeatureSetType)
                {
                    case FeatureSetType.Bear:
                        return "Bears";

                    case FeatureSetType.DorsalFin:
                    default:
                        return "Dolphin Fins";
                }
            }
        }

        [JsonIgnore]
        public string TraceInstructionsTerminology
        {
            get
            {
                // TODO: Do we need instructions?

                //switch (FeatureSetType)
                //{
                //    case FeatureSetType.Bear:
                //        return "Note: Bear MUST be facing your RIGHT!";

                //    case FeatureSetType.DorsalFin:
                //    default:
                //        return "Note: Dolphin MUST swim to your LEFT!";
                //}

                return string.Empty;
            }
        }

        [JsonIgnore]
        public string IndividualTerminologyInitialCaps
        {
            get
            {
                return IndividualTerminology.ToFirstCharacterUpper();
            }
        }

        [JsonIgnore]
        public string IndividualTerminology
        {
            get
            {
                switch (FeatureSetType)
                {
                    case FeatureSetType.Bear:
                        return "bear";

                    case FeatureSetType.DorsalFin:
                    default:
                        return "fin";
                }
            }
        }

        private bool _isDefault;
		public bool IsDefault
        {
            get => _isDefault;
            set
            {
                _isDefault = value;
                RaisePropertyChanged("IsDefault");
            }
        }

        private bool _isBuiltIn;
        public bool IsBuiltIn
        {
            get => _isBuiltIn;
            set
            {
                _isBuiltIn = value;
                RaisePropertyChanged("IsBuiltIn");
            }
        }

        public CatalogScheme()
		{
            FeatureSetType = FeatureSetType.DorsalFin;
			SchemeName = string.Empty;
			Categories = new ObservableCollection<Category>();
            IsDefault = false;
		}

        public CatalogScheme(string name, FeatureSetType featureSetType, List<Category> categories)
        {
            SchemeName = name;
            Categories = new ObservableCollection<Category>(categories);
            FeatureSetType = featureSetType;
            IsDefault = true;
        }

        public CatalogScheme(CatalogScheme scheme)
        {
            SchemeName = scheme.SchemeName;
            Categories = new ObservableCollection<Category>(scheme.Categories);
            FeatureSetType = scheme.FeatureSetType;
            IsDefault = scheme.IsDefault;
            IsBuiltIn = scheme.IsBuiltIn;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
