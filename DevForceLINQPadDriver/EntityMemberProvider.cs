/*
/// <changelog>
///   <item version="1.0.0.0" when="Jan-2012">Initial version</item>
///   <item version="1.0.2.0" when="Jan-24-2012">Some refactoring to support complex types; cache properties by type; ignore static properties; </item>
/// </changelog>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IdeaBlade.Drivers {

  /// <summary>
  /// Custom provider to ensure LINQPad doesn't follow navigation properties, or the EntityAspect/ComplexAspect.
  /// </summary>
  class EntityMemberProvider : LINQPad.ICustomMemberProvider {

    private object _objectToWrite;
    PropertyInfo[] _propsToWrite;
    private static bool _firstTime = true;
    private static Dictionary<Type, PropertyInfo[]> _typeLookup = new Dictionary<Type, PropertyInfo[]>();

    public EntityMemberProvider(object objectToWrite) {

      _objectToWrite = objectToWrite;

      // The property list here differs slightly from the schema - nav properties will not be followed.
      PropertyInfo[] props = null;
      if (!_typeLookup.TryGetValue(objectToWrite.GetType(), out props)) {
        props = objectToWrite.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(p => p.GetIndexParameters().Length == 0)
        .Where(p => !IsAspect(p))
        .Where(p => !IsNavigationProperty(p.PropertyType))
        .ToArray();
        _typeLookup.Add(objectToWrite.GetType(), props);
      }

      _propsToWrite = props;
    }

    /// <summary>
    /// This returns true for entities and complex types.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool IsEntityOrComplexType(Type t) {
      dynamic store = DevForceTypes.EntityMetadataStore;
      // This seems to avoid a timing glitch (?) where the first type tested won't be evaluated as an entity type.
      if (_firstTime) {
        store.IsEntityType(t);
        _firstTime = false;
      }
      return (bool)store.IsEntityType(t);
    }

    /// <summary>
    /// Checks for either EntityAspect or ComplexAspect, both of which should be ignored.
    /// </summary>
    /// <param name="pi"></param>
    /// <returns></returns>
    public static bool IsAspect(PropertyInfo pi) {
      return pi.PropertyType.FullName == "IdeaBlade.EntityModel.EntityAspect" ||
             pi.PropertyType.FullName == "IdeaBlade.EntityModel.ComplexAspect";
    }

    /// <summary>
    /// This is used to determine when a property should be followed.  
    /// We don't want to follow navigation properties.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool IsNavigationProperty(Type t) {
      if (t.IsGenericType) {
        Type iEnumerableOfT = t.GetInterface("System.Collections.Generic.IEnumerable`1");
        if (iEnumerableOfT != null) t = iEnumerableOfT.GetGenericArguments()[0];
        else if (t.IsInterface && t.Name == "IEnumerable`1") t = t.GetGenericArguments()[0];
      }
      // We do want to follow complex types, but not entities.
      var isEntity = IsEntityOrComplexType(t) && !DevForceTypes.IsComplexType(t);
      return isEntity;
    }

    #region ICustomMemberProvider Members

    public IEnumerable<string> GetNames() {
      return _propsToWrite.Select(p => p.Name);
    }

    public IEnumerable<Type> GetTypes() {
      return _propsToWrite.Select(p => p.PropertyType);
    }

    public IEnumerable<object> GetValues() {
      return _propsToWrite.Select(p => p.GetValue(_objectToWrite, null));
    }

    #endregion
  }
}
