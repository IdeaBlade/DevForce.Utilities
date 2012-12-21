/*
/// <changelog>
///   <item version="1.0.2.0" when="Jan-2012">Refactored from driver; support stored procs; ignore static properties; show key icon; </item>
/// </changelog>
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LINQPad.Extensibility.DataContext;

namespace IdeaBlade.Drivers {

  /// <summary>
  /// Uses reflection to obtain EntityQueries and stored proces defined on the EntityManager and returns the schema
  /// for returned data.
  /// </summary>
  internal class SchemaHelper {

    public static List<ExplorerItem> GetSchema(Type customType) {
      return GetEntityQueries(customType).Concat(GetRoutines(customType)).ToList();
    }

    private static List<ExplorerItem> GetEntityQueries(Type customType) {
      // We'll start by retrieving all the properties of the custom type that implement IEnumerable<T>:
      var topLevelProps =
      (
        from prop in customType.GetProperties()
        where prop.PropertyType != typeof(string)

        // Display all properties of type IEnumerable<T> (except for string!)
        let ienumerableOfT = prop.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1")
        where ienumerableOfT != null

        orderby prop.Name

        select new ExplorerItem(prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table) {
          IsEnumerable = true,
          ToolTipText = DataContextDriver.FormatTypeName(prop.PropertyType, false),
          DragText = prop.Name,

          // Store the entity type to the Tag property. We'll use it later.
          Tag = ienumerableOfT.GetGenericArguments()[0]
        }

      ).ToList();

      // Create a lookup keying each element type to the properties of that type. This will allow
      // us to build hyperlink targets allowing the user to click between associations:
      var elementTypeLookup = topLevelProps.ToLookup(tp => (Type)tp.Tag);

      // Populate the columns (properties) of each entity:
      foreach (ExplorerItem table in topLevelProps)
        table.Children = ((Type)table.Tag)
          .GetProperties(BindingFlags.Instance | BindingFlags.Public)
          .Where(childProp => !EntityMemberProvider.IsAspect(childProp))
          .Select(childProp => GetChildItem(elementTypeLookup, childProp))
          .OrderBy(childItem => childItem.Kind)
          .ToList();

      return topLevelProps;
    }

    private static ExplorerItem GetChildItem(ILookup<Type, ExplorerItem> elementTypeLookup, PropertyInfo childProp) {
      // If the property's type is in our list of entities, then it's a Many:1 (or 1:1) reference.
      // We'll assume it's a Many:1 (we can't reliably identify 1:1s purely from reflection).
      if (elementTypeLookup.Contains(childProp.PropertyType))
        return new ExplorerItem(childProp.Name, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne) {
          HyperlinkTarget = elementTypeLookup[childProp.PropertyType].First(),
          // FormatTypeName is a helper method that returns a nicely formatted type name.
          ToolTipText = DataContextDriver.FormatTypeName(childProp.PropertyType, true)
        };

      // Is the property's type a collection of entities?
      Type ienumerableOfT = childProp.PropertyType.GetInterface("System.Collections.Generic.IEnumerable`1");
      if (ienumerableOfT != null) {
        Type elementType = ienumerableOfT.GetGenericArguments()[0];
        if (elementTypeLookup.Contains(elementType))
          return new ExplorerItem(childProp.Name, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany) {
            HyperlinkTarget = elementTypeLookup[elementType].First(),
            ToolTipText = DataContextDriver.FormatTypeName(elementType, true)
          };
      }

      // Ordinary property:
      var isKey = childProp.GetCustomAttributes(false).Any(a => a.GetType().Name == "KeyAttribute");
      return new ExplorerItem(childProp.Name + " (" + DataContextDriver.FormatTypeName(childProp.PropertyType, false) + ")",
        ExplorerItemKind.Property, isKey ? ExplorerIcon.Key : ExplorerIcon.Column) { DragText = childProp.Name };
    }


    private static IEnumerable<ExplorerItem> GetRoutines(Type contextType) {
      // This looks for stored proc methods on the sub-typed EM.  Unlike the EF driver, it does not group the procs into an SP category.
      // The logic is actually stolen somewhat from the EF driver.
      // Sprocs currently return nothing (ignored here) or an IEnumerable of entity type, complex type, or nullable scalar primitive.
      // It's too slow to load metadata to check if entity/complex type, so we instead check for a nullable<t> return type, which indicates a scalar.

      var procs = (
                from mi in contextType.GetMethods()
                where mi.DeclaringType.FullName != "IdeaBlade.EntityModel.EntityManager"
                where mi.ReturnType.IsGenericType
                where typeof(IEnumerable<>).IsAssignableFrom(mi.ReturnType.GetGenericTypeDefinition())

                orderby mi.Name

                let rettype = mi.ReturnType.GetGenericArguments()[0]
                let scalartype = rettype.IsGenericType && rettype.GetGenericTypeDefinition() == typeof(Nullable<>)

                select new ExplorerItem(mi.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.StoredProc) {
                  ToolTipText = mi.Name,
                  DragText = mi.Name + "(" + (mi.GetParameters().Any<ParameterInfo>() ? "..." : "") + ")",
                  Children =
                    // Get parms
                              (from param in mi.GetParameters()
                               select new ExplorerItem(param.Name + " (" + DataContextDriver.FormatTypeName(param.ParameterType, false) + ")", ExplorerItemKind.Parameter, ExplorerIcon.Parameter)
                              )
                    // And regular properties (if a complex or entity type)
                              .Union
                              (from col in rettype.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                               where scalartype == false
                               where !EntityMemberProvider.IsAspect(col)
                               select new ExplorerItem(col.Name, ExplorerItemKind.Property, ExplorerIcon.Column)
                              )
                    // And a really screwy way to include a scalar return property.
                              .Union
                              (from t in new[] { rettype }
                               where scalartype == true
                               select new ExplorerItem(DataContextDriver.FormatTypeName(t, false), ExplorerItemKind.Property, ExplorerIcon.Column)
                              )
                              .ToList()
                }).ToList();

      return procs;
    }

  }
}
