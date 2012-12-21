using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace IdeaBlade.Windows {

  #region CollectionViewGroupRoot
  /// <summary>
  /// The root group node returned by the <see cref="ICollectionView.Groups"/>
  /// implementation of the <see cref="EntityQueryPagedCollectionView"/>.
  /// </summary>
  /// <remarks>
  /// Internal use only.
  /// </remarks>
  public class CollectionViewGroupRoot : CollectionViewGroupNode {

    /// <summary>
    /// Internal use only.
    /// </summary>
    /// <param name="groupDescriptions"></param>
    public CollectionViewGroupRoot(IList<GroupDescription> groupDescriptions)
      : base("_Root", 0, null) {

      // Note this is same collection owned by PCV ...
      GroupDescriptions = groupDescriptions;
    }

    /// <summary>
    /// Internal use only.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public ReadOnlyObservableCollection<Object> BuildGroupsFromList(IList items) {

      ProtectedItems.Clear();

      foreach (var item in items) {
        AddItemToGroup(item, 0, this);
      }
      return this.Items;
    }

    /// <summary>
    /// 
    /// </summary>
    public override bool IsBottomLevel {
      get { return false; }
    }

    /// <summary>
    /// 
    /// </summary>
    public IList<GroupDescription> GroupDescriptions {
      get;
      private set;
    }


    // A "special" item is a new item currently being edited.  It's added
    // directly to the root, not to a subgroup.  
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void AddSpecialItem(object item) {
      base.AddItem(item);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void RemoveSpecialItem(object item) {
      base.RemoveItem(item);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void AddNewItemToSubgroup(object item) {
      AddItemToGroup(item, 0, this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void RemoveItemFromSubgroup(object item) {
      RemoveItemFromGroup(item, 0, this);
    }
  }
  #endregion

  #region CollectionViewGroupNode
  /// <summary>
  /// A group node returned by the <see cref="ICollectionView.Groups"/>
  /// implementation of the <see cref="EntityQueryPagedCollectionView"/>.
  /// </summary>
  /// <remarks>
  /// Internal use only.
  /// </remarks>
  public class CollectionViewGroupNode : System.Windows.Data.CollectionViewGroup {

    /// <summary>
    /// Internal use only.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="level"></param>
    /// <param name="parent"></param>
    public CollectionViewGroupNode(object name, int level, CollectionViewGroupNode parent)
      : base(name) {
      _parent = parent;
      _level = level;
      _root = GetRoot();
    }

    /// <summary>
    /// 
    /// </summary>
    public override bool IsBottomLevel {
      get { return _level == _root.GroupDescriptions.Count; }
    }

    private CollectionViewGroupRoot GetRoot() {
      var node = this;
      while (node._parent != null) {
        node = node._parent;
      }
      return node as CollectionViewGroupRoot;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="level"></param>
    /// <param name="parent"></param>
    protected void AddItemToGroup(object item, int level, CollectionViewGroupNode parent) {
      if (IsBottomLevel) {
        AddItem(item);
      } else {
        var gd = _root.GroupDescriptions[level];
        var groupName = gd.GroupNameFromItem(item, level, System.Globalization.CultureInfo.CurrentCulture);
        int newLevel = level + 1;
        var node = GetGroupNode(newLevel, groupName);
        node.AddItemToGroup(item, newLevel, this);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="level"></param>
    /// <param name="parent"></param>
    protected void RemoveItemFromGroup(object item, int level, CollectionViewGroupNode parent) {
      if (IsBottomLevel) {
        RemoveItem(item);
        RemoveGroupIfEmpty();
      } else {
        var gd = _root.GroupDescriptions[level];
        var groupName = gd.GroupNameFromItem(item, level, System.Globalization.CultureInfo.CurrentCulture);
        int newLevel = level + 1;
        var node = GetGroupNode(newLevel, groupName);
        node.RemoveItemFromGroup(item, newLevel, this);
      }
    }

    private void RemoveGroupIfEmpty() {
      var parent = _parent;
      var node = this;

      while (node.ItemCount == 0) {
        parent.ProtectedItems.Remove(node);
        node = parent;
        parent = node._parent;
      }

    }

    private CollectionViewGroupNode GetGroupNode(int level, object name) {
      var node = this.Items.Cast<CollectionViewGroupNode>().Where(n => object.Equals(n.Name, name)).FirstOrDefault();
      if (node == null) {
        node = new CollectionViewGroupNode(name, level, this);
        AddItem(node);
      }
      return node;
    }

    internal IEnumerator GetLeafEnumerator() {
      return new LeafEnumerator(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    protected void AddItem(object item) {
      ProtectedItems.Add(item);
      ProtectedItemCount++;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    protected void RemoveItem(object item) {
      ProtectedItems.Remove(item);
      ProtectedItemCount--;
    }


    private CollectionViewGroupNode _parent;
    private int _level;
    private CollectionViewGroupRoot _root;

  }
  #endregion


  #region LeafEnumerator

  /// <summary>
  /// Enumerators the nodes of the CollectionViewGroupRoot tree.
  /// </summary>
  internal class LeafEnumerator : IEnumerator {

    public LeafEnumerator(CollectionViewGroupNode group) {
      _group = group;
      DoReset();
    }

    bool IEnumerator.MoveNext() {
      while ((_subEnum == null) || !_subEnum.MoveNext()) {
        _index++;
        if (_index >= _group.Items.Count) {
          return false;
        }
        CollectionViewGroupNode node = _group.Items[_index] as CollectionViewGroupNode;
        if (node == null) {
          _current = _group.Items[_index];
          _subEnum = null;
          return true;
        }
        _subEnum = node.GetLeafEnumerator();
      }
      _current = _subEnum.Current;
      return true;
    }

    void IEnumerator.Reset() {
      DoReset();
    }

    object IEnumerator.Current {
      get {
        if ((_index < 0) || (_index >= _group.Items.Count)) {
          throw new InvalidOperationException();
        }
        return _current;
      }
    }

    private void DoReset() {
      _index = -1;
      _subEnum = null;
    }

    private object _current;
    private CollectionViewGroupNode _group;
    private int _index;
    private IEnumerator _subEnum;

  }
  #endregion

}
