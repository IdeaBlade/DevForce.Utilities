using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IdeaBlade.Windows {

  /// <summary>
  /// An <see cref="ObservableCollection{T}"/> of objects implementing <see cref="INotifyPropertyChanged"/>.
  /// </summary>
  /// <remarks>
  /// The <b>ObservableItemCollection</b> is an ObservableCollection which also raises the
  /// <see cref="ItemChanged"/> event when properties on items within the collection change.
  /// </remarks>
  /// <typeparam name="T"></typeparam>
  public class ObservableItemCollection<T> : ObservableCollection<T> where T: INotifyPropertyChanged {

    /// <summary>
    /// Event fired when a property of an item within the collection changes.
    /// </summary>
    public event EventHandler<ItemChangedEventArgs<T>> ItemChanged;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    protected override void InsertItem(int index, T item) {
      base.InsertItem(index, item);
      item.PropertyChanged += item_PropertyChanged;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    protected override void RemoveItem(int index) {
      T item = this.Items[index];
      base.RemoveItem(index);

      item.PropertyChanged -= item_PropertyChanged;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void ClearItems() {
      foreach (var item in Items) {
        item.PropertyChanged -= item_PropertyChanged;
      }

      base.ClearItems();
    }

    private void item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      OnItemChanged((T)sender, e.PropertyName);
    }

    /// <summary>
    /// Fire <see cref="ItemChanged"/> event.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="propertyName"></param>
    protected void OnItemChanged(T item, string propertyName) {
      if (ItemChanged != null) {
        var args = new ItemChangedEventArgs<T>(item, propertyName);
        ItemChanged(this, args);
      }
    }

  }

  /// <summary>
  /// Arguments to the <see cref="ObservableItemCollection.ItemChanged"/> event on the <see cref="ObservableItemCollection{T}"/>.
  /// </summary>
  public class ItemChangedEventArgs<T> : EventArgs where T : INotifyPropertyChanged {

    /// <summary>
    /// Create an instance of this class.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="propertyName"></param>
    public ItemChangedEventArgs(T item, string propertyName) {
      Item = item;
      PropertyName = propertyName;
    }

    /// <summary>
    /// Changed item.
    /// </summary>
    public T Item {
      get;
      private set;
    }

    /// <summary>
    /// Name of item property which changed.
    /// </summary>
    public string PropertyName {
      get;
      private set;
    }

  }

}
