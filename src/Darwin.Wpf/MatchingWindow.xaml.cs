﻿// This file is part of DARWIN.
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

using Darwin.Matching;
using Darwin.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Darwin.Wpf
{
    /// <summary>
    /// Interaction logic for MatchingWindow.xaml
    /// </summary>
    public partial class MatchingWindow : Window
    {
        private BackgroundWorker _matchingWorker = new BackgroundWorker();

        private MatchingWindowViewModel _vm;
        public MatchingWindow(MatchingWindowViewModel vm)
        {
            InitializeComponent();

            _matchingWorker.WorkerReportsProgress = true;
            _matchingWorker.WorkerSupportsCancellation = true;
            //_matchingWorker.ProgressChanged += ProgressChanged;
            _matchingWorker.DoWork += MatchWork;
            _matchingWorker.RunWorkerCompleted += MatchWorker_RunWorkerCompleted;

            _vm = vm;
            this.DataContext = _vm;
            // Binding this does weird things with the startup location
            this.Height = _vm.WindowHeight;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            bool matchSettingsGood = false;
            try
            {
                this.IsHitTestVisible = false;
                Mouse.OverrideCursor = Cursors.Wait;

                // TODO -- shouldn't be hardcoded
                if (_vm.Database.CatalogScheme.FeatureSetType != Features.FeatureSetType.Bear)
                {
                    _vm.Match.SetMatchOptions(_vm.RegistrationMethod,
                        (_vm.RangeOfPoints == RangeOfPointsType.AllPoints) ? true : false);
                }

                matchSettingsGood = _vm.Match.VerifyMatchSettings();
            }
            finally
            {
                Mouse.OverrideCursor = null;
                this.IsHitTestVisible = true;
            }

            if (!matchSettingsGood)
            {
                if (Options.CurrentUserOptions.MatchingScheme == Darwin.Model.MatchingSchemeType.MachineLearning)
                {
                    MessageBox.Show("Sorry, at least some individuals in your database " +
                        "are missing embeddings needed to run the current match settings." + Environment.NewLine + Environment.NewLine +
                        "Please use Re-compute Embeddings in Settings -> Current Catalog Schemes in the main window, " +
                        "or recreate your database with the current settings.", "Missing Embeddings", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Sorry, at least some individuals in your database " +
                        "are missing feature points needed to run the current match settings." + Environment.NewLine + Environment.NewLine +
                        "Please use Rediscover Features in Settings -> Current Catalog Schemes in the main window, " +
                        "or recreate your database with the current feature set scheme.", "Missing Features", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _vm.ProgressBarVisibility = Visibility.Visible;
                _matchingWorker.RunWorkerAsync();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.PauseMatching)
            {
                PauseButton.Content = "Pause";
                _vm.PauseMatching = false;
            }
            else
            {
                PauseButton.Content = "Continue";
                _vm.PauseMatching = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.MatchRunning)
            {
                _vm.CancelMatching = true;
                _matchingWorker.CancelAsync();
            }
            else
            {
                this.Close();
            }
        }

        private void SelectAllCategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectableCategories != null)
            {
                foreach (var cat in _vm.SelectableCategories)
                    cat.IsSelected = true;
            }
        }

        private void ClearAllCategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectableCategories != null)
            {
                foreach (var cat in _vm.SelectableCategories)
                    cat.IsSelected = false;
            }
        }

        private void MatchWork(object sender, DoWorkEventArgs e)
        {
            bool done = false;
            _vm.MatchRunning = true;

            do
            {
                if (_matchingWorker.CancellationPending)
                {
                    e.Cancel = true;
                    done = true;
                }
                else if (_vm.PauseMatching)
                {
                    // Sleep for a small amount of time
                    Thread.Sleep(100);
                }
                else
                {
                    // Do Work
                    float percentComplete = _vm.Match.MatchSingleIndividual(_vm.SelectableCategories.Where(c => c.IsSelected).ToList()); 

                    int roundedProgress = (int)Math.Round(percentComplete * 100);

                    _vm.MatchProgressPercent = roundedProgress;
                    _matchingWorker.ReportProgress(roundedProgress);

                    if (percentComplete >= 1.0)
                    {
                        //***1.5 - sort the results here, ONCE, rather than as list is built
                        _vm.Match.MatchResults.Sort();
                        done = true;
                    }
                }
            } while (!done);

            // Below is a try at running MatchSingleIndividual in a Parallel For.  Note that the cancel and pause aren't done correctly,
            // but currently not pursuing this, since as of the writing of this comment, the processing was IO bound, so parallelizing
            // didn't help.  Leaving it below in case we need to come back to it. MatchSingleIndividual should be set up to run in multiple
            // threads.
            //
            //_vm.MatchRunning = true;
            //object locker = new object();

            //var selectedCategories = _vm.SelectableCategories.Where(c => c.IsSelected).ToList();

            //Parallel.For(0, _vm.Database.AllFins.Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (index, state) =>
            //{
            //    if (_matchingWorker.CancellationPending)
            //    {
            //        e.Cancel = true;
            //        state.Stop();
            //    }
            //    else if (_vm.PauseMatching)
            //    {
            //        // Sleep for a small amount of time
            //        Thread.Sleep(100);
            //    }
            //    else
            //    {
            //        // Do Work
            //        float percentComplete = _vm.Match.MatchSingleIndividual(selectedCategories);

            //        lock (locker)
            //        {
            //            int roundedProgress = (int)Math.Round(percentComplete * 100);

            //            _vm.MatchProgressPercent = roundedProgress;
            //            _matchingWorker.ReportProgress(roundedProgress);

            //            if (percentComplete >= 1.0)
            //            {
            //                //***1.5 - sort the results here, ONCE, rather than as list is built
            //                _vm.Match.MatchResults.Sort();
            //            }
            //        }
            //    }
            //});
        }

        private void MatchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!_vm.CancelMatching)
            {
                if (_vm.Match.MatchResults == null || _vm.Match.MatchResults.Count < 1)
                {
                    MessageBox.Show("Selected Catalog Categories are ALL EMPTY!");
                    return;
                }
                else
                {
                    // Matching is done, go to the results window
                    var matchingResultsWindowVM = new MatchingResultsWindowViewModel(
                        _vm.Match.UnknownFin,
                        _vm.Match.MatchResults,
                        _vm.Database);
                    var matchingResultsWindow = new MatchingResultsWindow(matchingResultsWindowVM);
                    matchingResultsWindow.Show();
                }
            }

            this.Close();
        }
    }
}
