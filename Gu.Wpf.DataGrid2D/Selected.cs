﻿#pragma warning disable SA1202
namespace Gu.Wpf.DataGrid2D
{
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    public static class Selected
    {
        public static readonly DependencyProperty CellItemProperty = DependencyProperty.RegisterAttached(
            "CellItem",
            typeof(object),
            typeof(Selected),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnCellItemChanged,
                OnCellItemIndexCoerce));

        public static readonly DependencyProperty IndexProperty = DependencyProperty.RegisterAttached(
            "Index",
            typeof(RowColumnIndex?),
            typeof(Selected),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIndexChanged,
                OnSelectedIndexCoerce));

        private static readonly DependencyProperty IsSubscribingChangesProperty = DependencyProperty.RegisterAttached(
            "IsSubscribingChanges",
            typeof(bool),
            typeof(Selected),
            new PropertyMetadata(BooleanBoxes.False));

        private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(Selected),
            new PropertyMetadata(BooleanBoxes.False));

        private static readonly DependencyProperty PropertyDescriptorProperty = DependencyProperty.RegisterAttached(
            "PropertyDescriptor",
            typeof(PropertyDescriptor),
            typeof(Selected),
            new PropertyMetadata(null));

        private static readonly RoutedEventHandler SelectedCellsChangedHandler = OnSelectedCellsChanged;

        public static void SetCellItem(this DataGrid element, object value)
        {
            element.SetValue(CellItemProperty, value);
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static object GetCellItem(this DataGrid element)
        {
            return element.GetValue(CellItemProperty);
        }

        public static void SetIndex(this DataGrid element, RowColumnIndex? value)
        {
            element.SetValue(IndexProperty, value);
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static RowColumnIndex? GetIndex(this DataGrid element)
        {
            return (RowColumnIndex?)element.GetValue(IndexProperty);
        }

        private static void OnSelectedCellsChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            var dataGrid = (DataGrid)sender;
            if (Equals(dataGrid.GetValue(IsUpdatingProperty), BooleanBoxes.True))
            {
                return;
            }

            dataGrid.SetValue(IsUpdatingProperty, BooleanBoxes.True);
            try
            {
                if (dataGrid.SelectedCells.Count != 1)
                {
                    dataGrid.SetValue(IndexProperty, null);
                    return;
                }

                var index = dataGrid.SelectedRowColumnIndex();
                dataGrid.SetValue(IndexProperty, index);
                UpdateSelectedCellItemFromView(dataGrid);
            }
            finally
            {
                dataGrid.SetValue(IsUpdatingProperty, BooleanBoxes.False);
            }
        }

        private static void OnCellItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(d.GetValue(IsUpdatingProperty), BooleanBoxes.True))
            {
                return;
            }

            d.SetValue(IsUpdatingProperty, BooleanBoxes.True);
            try
            {
                var dataGrid = (DataGrid)d;
                dataGrid.UnselectAllCells();
                if (e.NewValue == null)
                {
                    dataGrid.SetIndex(null);
                    return;
                }

                for (var r = 0; r < dataGrid.Items.Count; r++)
                {
                    for (var c = 0; c < dataGrid.Columns.Count; c++)
                    {
                        var column = dataGrid.Columns[c];
                        var cellItem = GetCellItem(column, dataGrid.Items[r]);
                        if (Equals(cellItem, e.NewValue))
                        {
                            var index = new RowColumnIndex(r, c);
                            dataGrid.SetIndex(index);
                            var cell = dataGrid.GetCell(index);
                            cell.IsSelected = true;
                            return;
                        }
                    }
                }

                dataGrid.SetIndex(null);
            }
            finally
            {
                d.SetValue(IsUpdatingProperty, BooleanBoxes.False);
            }
        }

        private static object OnCellItemIndexCoerce(DependencyObject d, object basevalue)
        {
            if (Equals(d.GetValue(IsSubscribingChangesProperty), BooleanBoxes.False))
            {
                SubscribeSelectionChanges((DataGrid)d);
            }

            return basevalue;
        }

        private static void OnIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(d.GetValue(IsUpdatingProperty), BooleanBoxes.True))
            {
                return;
            }

            d.SetValue(IsUpdatingProperty, BooleanBoxes.True);
            try
            {
                var dataGrid = (DataGrid)d;
                dataGrid.UnselectAllCells();
                var rowColumnIndex = (RowColumnIndex?)e.NewValue;
                if (rowColumnIndex == null)
                {
                    return;
                }

                var cell = dataGrid.GetCell(rowColumnIndex.Value);
                if (cell != null)
                {
                    cell.IsSelected = true;
                }

                UpdateSelectedCellItemFromView(dataGrid);
            }
            finally
            {
                d.SetValue(IsUpdatingProperty, BooleanBoxes.False);
            }
        }

        private static object OnSelectedIndexCoerce(DependencyObject d, object basevalue)
        {
            if (Equals(d.GetValue(IsSubscribingChangesProperty), BooleanBoxes.False))
            {
                SubscribeSelectionChanges((DataGrid)d);
            }

            return basevalue;
        }

        private static void SubscribeSelectionChanges(DataGrid dataGrid)
        {
            dataGrid.UpdateHandler(DataGridCell.SelectedEvent, SelectedCellsChangedHandler, true);
            dataGrid.UpdateHandler(DataGridCell.UnselectedEvent, SelectedCellsChangedHandler, true);
            dataGrid.SetValue(IsSubscribingChangesProperty, BooleanBoxes.True);
        }

        private static RowColumnIndex? SelectedRowColumnIndex(this DataGrid dataGrid)
        {
            var item = dataGrid.CurrentItem;
            if (item == null)
            {
                return null;
            }

            var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item);
            if (row == null || dataGrid.CurrentColumn == null)
            {
                return null;
            }

            var rowIndex = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
            return new RowColumnIndex(rowIndex, dataGrid.CurrentColumn.DisplayIndex);
        }

        private static DataGridCell GetCell(this DataGrid dataGrid, RowColumnIndex index)
        {
            if (index.Column < 0 || index.Column >= dataGrid.Columns.Count)
            {
                return null;
            }

            if (index.Row < 0 || index.Row >= dataGrid.ItemContainerGenerator.Items.Count)
            {
                return null;
            }

            var dataGridColumn = dataGrid.Columns[index.Column];
            var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index.Row);
            var content = dataGridColumn.GetCellContent(row);
            var cell = content.Ancestors()
                              .OfType<DataGridCell>()
                              .FirstOrDefault();

            return cell;
        }

        private static void UpdateSelectedCellItemFromView(DataGrid dataGrid)
        {
            var index = dataGrid.GetIndex();
            if (index == null || index.Value.Column < 0 || index.Value.Column >= dataGrid.Columns.Count)
            {
                dataGrid.SetValue(CellItemProperty, null);
                return;
            }

            var column = dataGrid.Columns.ElementAtOrDefault<DataGridColumn>(index.Value.Column);
            var item = dataGrid.Items.ElementAtOrDefault(index.Value.Row);

            var cellItem = GetCellItem(column, item);
            dataGrid.SetValue(CellItemProperty, cellItem);
        }

        private static object GetCellItem(this DataGridColumn column, object item)
        {
            if (column == null || item == null)
            {
                return null;
            }

            var descriptor = (PropertyDescriptor)column.GetValue(PropertyDescriptorProperty);
            if (descriptor != null)
            {
                return descriptor.GetValue(item);
            }

            var binding = (column as DataGridBoundColumn)?.Binding as Binding;
            if (binding == null)
            {
                return null;
            }

            descriptor = TypeDescriptor.GetProperties(item)
                                       .OfType<PropertyDescriptor>()
                                       .SingleOrDefault(x => x.Name == binding.Path.Path);
            column.SetValue(PropertyDescriptorProperty, descriptor);

            return descriptor?.GetValue(item);
        }
    }
}
