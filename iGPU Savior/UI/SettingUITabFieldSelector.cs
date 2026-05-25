using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PotatoOptimization.UI
{
  internal static class SettingUITabFieldSelector
  {
    private const string InteractableSuffix = "InteractableUI";
    private const string ParentSuffix = "Parent";

    public static List<FieldInfo> GetTabInteractableFields(
      IEnumerable<FieldInfo> allFields,
      Type interactableType,
      Type parentType)
    {
      if (allFields == null || interactableType == null || parentType == null)
      {
        return new List<FieldInfo>();
      }

      var fields = allFields.Where(field => field != null).ToList();

      var parentFieldNames = new HashSet<string>(
        fields
          .Where(field => field.FieldType == parentType
                          && field.Name.EndsWith(ParentSuffix, StringComparison.Ordinal))
          .Select(field => field.Name),
        StringComparer.Ordinal);

      return fields
        .Where(field => field.FieldType == interactableType
                        && field.Name.EndsWith(InteractableSuffix, StringComparison.Ordinal)
                        && parentFieldNames.Contains(ToParentFieldName(field.Name)))
        .ToList();
    }

    private static string ToParentFieldName(string interactableFieldName)
    {
      return interactableFieldName.Substring(
               0,
               interactableFieldName.Length - InteractableSuffix.Length)
             + ParentSuffix;
    }
  }
}
