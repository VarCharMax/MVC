namespace MVC.Attributes
{
  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
  public class JSONDocumentPropertyAttribute : Attribute
  {
    public bool Copy { get; set; } = true;
  }
}
