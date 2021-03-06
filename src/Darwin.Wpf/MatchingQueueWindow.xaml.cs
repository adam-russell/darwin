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

using Darwin.Database;
using Darwin.Matching;
using Darwin.Model;
using Darwin.Wpf.Commands;
using Darwin.Wpf.ViewModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Threading;

namespace Darwin.Wpf
{
    /// <summary>
    /// Interaction logic for MatchingQueueWindow.xaml
    /// </summary>
    public partial class MatchingQueueWindow : Window
    {
        private BackgroundWorker _matchingWorker = new BackgroundWorker();
        private MatchingQueueViewModel _vm;

        public MatchingQueueWindow(MatchingQueueViewModel vm)
        {
            InitializeComponent();
            
            _matchingWorker.WorkerReportsProgress = true;
            _matchingWorker.WorkerSupportsCancellation = true;
            //_matchingWorker.ProgressChanged += ProgressChanged;
            _matchingWorker.DoWork += MatchWork;
            _matchingWorker.RunWorkerCompleted += MatchWorker_RunWorkerCompleted;

            _vm = vm;
            this.DataContext = vm;
        }

        private void GridHeader_Click(object sender, RoutedEventArgs e)
        {
            var sortableListViewSender = sender as Controls.SortableListView;

            if (sortableListViewSender != null)
                sortableListViewSender.GridViewColumnHeaderClickedHandler(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.MatchingQueue.MatchRunning)
            {
                _vm.CancelMatching = true;
                _matchingWorker.CancelAsync();
            }
            else
            {
                this.Close();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedFin != null)
                _vm.MatchingQueue.Fins.Remove(_vm.SelectedFin);
        }

        private void AddFinzButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Options.CurrentUserOptions.CurrentTracedFinsPath;
            openDialog.Title = "Select Fiz File(s) To Queue";
            openDialog.Multiselect = true;
            openDialog.Filter = CustomCommands.TracedFinFilter;
            if (openDialog.ShowDialog() == true)
            {
                if (openDialog.FileNames != null && openDialog.FileNames.Length > 0)
                {
                    try
                    {
                        // We're not doing anything async below, so the display may not update with 
                        // the wait cursor nicely.  Should probably change the add finz stuff to be 
                        // async
                        this.IsHitTestVisible = false;
                        Mouse.OverrideCursor = Cursors.Wait;

                        foreach (var filename in openDialog.FileNames)
                        {
                            if (string.IsNullOrEmpty(filename))
                                continue;

                            var fin = CatalogSupport.OpenFinz(filename);

                            if (fin == null)
                            {
                                MessageBox.Show(this, "Problem opening finz file: " + System.IO.Path.GetFileName(filename), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                if (_vm.MatchingQueue.Fins.Where(f => f.FinFilename == fin.FinFilename).Any())
                                {
                                    MessageBox.Show("The finz file "
                                                    + System.IO.Path.GetFileName(filename)
                                                    + " was previously added to the queue.", "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    _vm.MatchingQueue.Fins.Add(fin);
                                    _vm.SelectedFin = _vm.MatchingQueue.Fins.Last();
                                }
                            }
                        }
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                        this.IsHitTestVisible = true;
                    }
                }
            }
        }

        private void MatchWork(object sender, DoWorkEventArgs e)
        {
            if (_vm.MatchingQueue.Fins.Count < 1)
                return;

            bool done = false;
            _vm.MatchingQueue.MatchRunning = true;

            int currentIndex = 0;

            do
            {
                if (_matchingWorker.CancellationPending)
                {
                    e.Cancel = true;
                    done = true;
                    _vm.MatchingQueue.MatchRunning = false;
                }
                else if (_vm.PauseMatching)
                {
                    // Sleep for a small amount of time
                    Thread.Sleep(100);
                }
                else
                {
                    // TODO: Put this logic inside the MatchingQueue class?
                    if (_vm.MatchingQueue.Matches.Count < currentIndex + 1)
                    {
                        switch (_vm.MatchingQueue.Database.CatalogScheme.FeatureSetType)
                        {
                            case Features.FeatureSetType.DorsalFin:
                                _vm.MatchingQueue.Matches.Add(new Match(
                                    _vm.MatchingQueue.Fins[currentIndex],
                                    _vm.MatchingQueue.Database, null,
                                    _vm.MatchingQueue.RegistrationMethod,
                                    (_vm.MatchingQueue.RangeOfPoints == RangeOfPointsType.AllPoints) ? true : false));
                                break;

                            case Features.FeatureSetType.Bear:
                                _vm.MatchingQueue.Matches.Add(new Match(
                                    _vm.MatchingQueue.Fins[currentIndex],
                                    _vm.MatchingQueue.Database,
                                    null,
                                    null,
                                    true));
                                break;

                            default:
                                throw new NotImplementedException();
                        }

                        // This needs to run on the UI thread since it affects dependency objects
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _vm.SelectedFin = _vm.MatchingQueue.Fins[currentIndex];
                            DatabaseGrid.ScrollIntoView(_vm.SelectedFin);
                        }), DispatcherPriority.Background);
                    }

                    // Do Work
                    float percentComplete = _vm.MatchingQueue.Matches[currentIndex].MatchSingleIndividual(_vm.MatchingQueue.Database.Categories.ToList()); 

                    int roundedProgress = (int)Math.Round(percentComplete * 100);

                    _vm.CurrentUnknownPercent = roundedProgress;

                    var totalProgress = (int)Math.Round(((float)currentIndex / _vm.MatchingQueue.Fins.Count + percentComplete / _vm.MatchingQueue.Fins.Count) * 100);

                    _vm.QueueProgressPercent = totalProgress;
                    _matchingWorker.ReportProgress(roundedProgress);

                    if (percentComplete >= 1.0)
                    {
                        //***1.5 - sort the results here, ONCE, rather than as list is built
                        _vm.MatchingQueue.Matches[currentIndex].MatchResults.Sort();

                        if (currentIndex >= _vm.MatchingQueue.Fins.Count - 1)
                        {
                            _vm.SaveMatchResults();
                            done = true;
                            _vm.MatchingQueue.MatchRunning = false;
                        }
                        else
                        {
                            currentIndex++;
                        }
                    }
                }
            } while (!done);
        }

        private void MatchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!_vm.CancelMatching)
            {
                var summary = _vm.GetMatchSummary();

                MessageBox.Show(this, "Your matching queue has finished.\nYour results are in the " +
                    Options.MatchQResultsFolderName + " folder.\n\nSummary:\n" + summary,
                    "Queue Complete", MessageBoxButton.OK);
            }

            this.Close();
        }

        private void SaveQueueButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.InitialDirectory = Options.CurrentUserOptions.CurrentMatchQueuePath;
            dlg.FileName = "Untitled";
            dlg.DefaultExt = ".que";
            dlg.Filter = CustomCommands.QueueFilenameFilter;

            if (dlg.ShowDialog() == true)
            {
                _vm.SaveQueue(dlg.FileName);
            }
        }

        private void LoadQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var openQueueDialog = new OpenFileDialog();
            openQueueDialog.Filter = CustomCommands.QueueFilenameFilter;
            openQueueDialog.InitialDirectory = Options.CurrentUserOptions.CurrentMatchQueuePath;

            if (openQueueDialog.ShowDialog() == true)
            {
                try
                {
                    this.IsHitTestVisible = false;
                    Mouse.OverrideCursor = Cursors.Wait;

                    // TODO: Load should really be async
                    _vm.LoadQueue(openQueueDialog.FileName);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    this.IsHitTestVisible = true;
                }
            }
        }

        private void ViewResultsButton_Click(object sender, RoutedEventArgs e)
        {
            var openQueueResultsDialog = new OpenFileDialog();
            openQueueResultsDialog.Filter = CustomCommands.QueueResultsFilenameFilter;
            openQueueResultsDialog.InitialDirectory = Options.CurrentUserOptions.CurrentMatchQueueResultsPath;

            if (openQueueResultsDialog.ShowDialog() == true)
            {
                try
                {
                    DarwinDatabase resultsDB;
                    DatabaseFin databaseFin;
                    MatchResults results = _vm.LoadMatchResults(openQueueResultsDialog.FileName, out resultsDB, out databaseFin);

                    if (resultsDB == null || databaseFin == null || results == null)
                        throw new Exception("Missing object");

                    if (resultsDB.Filename.ToLower() != _vm.MatchingQueue.Database.Filename.ToLower())
                        MessageBox.Show(this,
                            "Warning: This queue was run against a different database " + Environment.NewLine +
                            "the currently loaded database.  The database used for the queue " + Environment.NewLine +
                            "is being loaded to view the results.  This will not change the " + Environment.NewLine +
                            "database you have loaded.",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    var matchingResultsWindowVM = new MatchingResultsWindowViewModel(
                        databaseFin,
                        results,
                        resultsDB);
                    var matchingResultsWindow = new MatchingResultsWindow(matchingResultsWindowVM);
                    matchingResultsWindow.Show();
                }
                catch (Exception ex)
                {
                    Trace.Write(ex);
                    MessageBox.Show(this, "There was a problem loading your results.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RunMatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.VerifyMatchSettings())
            {
                MessageBox.Show("Sorry, at least some individuals in your database " +
                    "are missing feature points needed to run the current match settings." + Environment.NewLine + Environment.NewLine +
                    "Please use Rediscover Features in Settings -> Current Catalog Schemes in the main window, " +
                    "or recreate your database with the current feature set scheme.", "Missing Features", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                _matchingWorker.RunWorkerAsync();
            }
        }
    }
}
