using MVC.Attributes;
using System.Reflection;

namespace MVC.Helpers
{
  public static class TypeComparer
  {
    public static bool HaveSameProperties<TSource, TTarget>()
    {
      Type typeSource = typeof(TSource);
      Type typeTarget = typeof(TTarget);

      // Exclude any properties with JSONDocumentPropertyAttribute.Copy == false.
      PropertyInfo[] propertiesSource = [.. typeSource
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p => 
            !p.GetCustomAttributes(typeof(JSONDocumentPropertyAttribute), true)
              .OfType<JSONDocumentPropertyAttribute>()
              .Any(a => a.Copy == false)
          )];

      PropertyInfo[] propertiesTarget = typeTarget.GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (PropertyInfo propS in propertiesSource)
      {
        PropertyInfo? matchingPropT = propertiesTarget.FirstOrDefault(p => p.Name == propS.Name && p.PropertyType == propS.PropertyType);

        if (matchingPropT == null)
        {
          return false;
        }
      }

      return true;
    }
  }
}
