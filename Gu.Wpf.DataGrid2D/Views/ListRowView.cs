﻿namespace Gu.Wpf.DataGrid2D
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;

    public class ListRowView : RowView<IView2D>, INotifyPropertyChanged
    {
        private static readonly EventDescriptorCollection Events = TypeDescriptor.GetEvents(typeof(ListRowView));

        internal ListRowView(IView2D source, int index, PropertyDescriptorCollection properties)
            : base(source, index, properties)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override EventDescriptorCollection GetEvents() => Events;

        public override EventDescriptorCollection GetEvents(Attribute[] attributes) => Events;

        internal void RaiseAllChanged()
        {
            foreach (PropertyDescriptor property in this.GetProperties())
            {
                this.OnPropertyChanged(property.Name);
            }
        }

        internal void RaiseColumnsChanged(int startColumn, int count)
        {
            var collection = this.GetProperties();
            for (int i = startColumn; i < startColumn + count; i++)
            {
                this.OnPropertyChanged(collection[i].Name);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
