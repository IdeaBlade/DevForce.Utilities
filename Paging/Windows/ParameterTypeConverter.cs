
using System;
using System.ComponentModel;
using System.Globalization;

namespace IdeaBlade.Windows {

  /// <summary>
  /// <see cref="TypeConverter"/> for the <see cref="Parameter"/> type.
  /// </summary>
  public class ParameterTypeConverter : TypeConverter {

    // from a string to a Parameter
    // for something like this:
    //   <ib:SortDescriptor PropertyPath="City" Direction="Ascending" />
    /// <summary>
    /// Returns true if converting from a string to a <see cref="Parameter"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sourceType"></param>
    /// <returns></returns>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
      return (sourceType == typeof(string));
    }

    /// <summary>
    /// Returns a <see cref="Parameter"/> from the given string.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
      string str = value as string;
      if (str != null) {
        Parameter parameter = new Parameter();
        parameter.Value = str;
        return parameter;
      }
      return base.ConvertFrom(context, culture, value);
    }

    // from Parameter to string
    /// <summary>
    /// Returns true if converting from a <see cref="Parameter"/> to a string.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="destinationType"></param>
    /// <returns></returns>
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
      return (destinationType == typeof(string));
    }

    /// <summary>
    /// Returns the string representation of a <see cref="Parameter"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="culture"></param>
    /// <param name="value"></param>
    /// <param name="destinationType"></param>
    /// <returns></returns>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
      if (destinationType == typeof(string)) {
        Parameter parameter = value as Parameter;
        if (parameter != null) {
          if (parameter.Value != null) {
            return parameter.Value.ToString();
          }
          return null;
        }
      }
      return base.ConvertTo(context, culture, value, destinationType);
    }
  }
  
}
