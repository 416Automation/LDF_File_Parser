using LDF_FILEPARSER;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LDF_File_Viewer
{

    /// <summary>
    /// Interaction logic for LinViewer.xaml
    /// </summary>
    public partial class LinViewer : UserControl
    {
        public LinViewerViewModel _viewModel;

        public LinViewer()
        {
            InitializeComponent();
            _viewModel = new LinViewerViewModel();
            _viewModel.SelectedFrameChangedEvent += () => ResizeAndStretchColumns();
            DataContext = _viewModel;
            __signalViewer.ItemContainerGenerator.StatusChanged += HandleStatusChanged;
            //__signalViewer.Loaded += (s, e) => ResizeAndStretchColumns();
            __signalViewer.SizeChanged += HandleSizeChanged;
        }

        private void HandleStatusChanged(object sender, EventArgs e)
        {
            if (((ItemContainerGenerator)sender).Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                ResizeAndStretchColumns();
            }
        }

        private void HandleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                ResizeAndStretchColumns();
            }
        }

        private (GridViewColumn?, int) FindColumnByTag(string tag)
        {
            int index = 0;
            foreach (var column in __signalContainer.Columns)
            {
                if (column.Header is GridViewColumnHeader gvh && (string?)gvh.Tag == tag)
                {
                    return (column, index);
                }
                index++;
            }
            return (null, -1);
        }

        private void ResizeAndStretchColumns()
        {
            if (__signalContainer?.Columns.Count == 0) return;

            (GridViewColumn? stretchColumn, int stretchIndex) = FindColumnByTag("BitValueColumn");
            if (stretchColumn is null) return;
            
            __signalViewer.UpdateLayout();

            // Ensure all other columns have explicit widths
            for (int i = 0; i < __signalContainer.Columns.Count; i++)
            {
                if (i != stretchIndex)
                {
                    double maxWidth = GetWidestCellWidth(i);
                    if (maxWidth <= __signalContainer.Columns[i].ActualWidth) maxWidth = __signalContainer.Columns[i].ActualWidth;
                    __signalContainer.Columns[i].Width = maxWidth;
                }
            }

            __signalViewer.UpdateLayout();

            double otherWidths = 0;
            for (int i = 0; i < __signalContainer.Columns.Count; i++)
            {
                if (i == stretchIndex)
                {
                    continue;
                }

                var col = __signalContainer.Columns[i];
                otherWidths += col.ActualWidth;
            }

            double listViewWidth = __signalViewer.ActualWidth;
            var scrollViewer = FindVisualChild<ScrollViewer>(__signalViewer);
            Debug.Print($"{nameof(scrollViewer.ComputedVerticalScrollBarVisibility)} == {scrollViewer.ComputedVerticalScrollBarVisibility}");
            double scrollBarWidth = (scrollViewer != null && scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible) ? SystemParameters.VerticalScrollBarWidth : 0;
            double remainingWidth = listViewWidth - otherWidths - scrollBarWidth - 10;

            if (remainingWidth > 0)
            {
                stretchColumn.Width = remainingWidth;
            }

            __signalViewer.UpdateLayout();
        }

        private double GetWidestCellWidth(int columnIndex)
        {
            double maxWidth = 0;

            foreach (var item in __signalViewer.Items)
            {
                var listViewItem = __signalViewer.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (listViewItem == null)
                    continue;

                var presenter = FindVisualChild<GridViewRowPresenter>(listViewItem);
                if (presenter == null)
                    continue;

                FrameworkElement cell = null;
                int childCount = VisualTreeHelper.GetChildrenCount(presenter);
                if (childCount > columnIndex)
                {
                    cell = VisualTreeHelper.GetChild(presenter, columnIndex) as FrameworkElement;
                }
                if (cell != null)
                {
                    cell.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    maxWidth = Math.Max(maxWidth, cell.DesiredSize.Width);
                }
            }

            return maxWidth;
        }

        public static T FindVisualChild<T>(DependencyObject parent) 
            where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void TextBox_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
                e.Effects = DragDropEffects.None;
        }

        private void Button_DragLeave(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length != 0)
            {
                // TODO add validations and logs
                var test = files[0];
                _viewModel.BrowseFile(test);
            }
        }
    }
}
