﻿using Darwin.Model;
using Darwin.Wpf.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Darwin.Wpf.ViewModel
{
    public class TraceWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

		private Bitmap _bitmap;
		public Bitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				RaisePropertyChanged("Bitmap");

				if (_bitmap != null)
				{
					// Copy it to the OriginalBitmap property, as well, if it hasn't been set
					if (OriginalBitmap == null)
						OriginalBitmap = new Bitmap(_bitmap);

					ImageSource = _bitmap.ToImageSource();
				}
				else if (ImageSource != null)
                {
					ImageSource = null;
                }
			}
		}

		private ImageSource _imageSource;
		public ImageSource ImageSource
		{
			get => _imageSource;
			set
			{
				_imageSource = value;
				RaisePropertyChanged("ImageSource");
			}
		}

		public void UpdateImage()
        {
			ImageSource = _bitmap.ToImageSource();
        }

		private Bitmap _originalBitmap;
		public Bitmap OriginalBitmap
		{
			get => _originalBitmap;
			set
			{
				_originalBitmap = value;
				RaisePropertyChanged("OriginalBitmap");
			}
		}

		private Contour _contour;
		public Contour Contour
		{
			get => _contour;
			set
			{
				_contour = value;
				RaisePropertyChanged("Contour");
			}
		}

		private Outline _outline;
		public Outline Outline
		{
			get => _outline;
			set
			{
				_outline = value;
				RaisePropertyChanged("Outline");
			}
		}

		private TraceToolType _traceTool;
		public TraceToolType TraceTool
		{
			get => _traceTool;
			set
			{
				_traceTool = value;
				RaisePropertyChanged("TraceTool");
			}
		}

		private bool _traceLocked;
		public bool TraceLocked
		{
			get => _traceLocked;
			set
			{
				_traceLocked = value;
				RaisePropertyChanged("TraceLocked");
			}
		}

		// TODO: Do we need both finalized & locked?
		private bool _traceFinalized;
		public bool TraceFinalized
		{
			get => _traceFinalized;
			set
			{
				_traceFinalized = value;
				RaisePropertyChanged("TraceFinalized");
			}
		}

		private bool _traceSnapped;
		public bool TraceSnapped
		{
			get => _traceSnapped;
			set
			{
				_traceSnapped = value;
				RaisePropertyChanged("TraceSnapped");
			}
		}

		private bool _imageLocked;
		public bool ImageLocked
		{
			get => _imageLocked;
			set
			{
				_imageLocked = value;
				RaisePropertyChanged("ImageLocked");
			}
		}

		private float _zoomRatio;
		public float ZoomRatio
		{
			get => _zoomRatio;
			set
			{
				_zoomRatio = value;
				RaisePropertyChanged("ZoomRatio");
			}
		}

		private List<double> _zoomValues;
		public List<double> ZoomValues
		{
			get => _zoomValues;
			set
			{
				_zoomValues = value;
				RaisePropertyChanged("ZoomValues");
			}
		}

		public float ZoomPointSize
		{
			get
			{
				if (_zoomRatio > 0.5f)
					return AppSettings.DrawingPointSize * _zoomRatio;

				return AppSettings.DrawingPointSize;
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