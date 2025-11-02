using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MVC.Helpers
{
  public class JsonPatchDocumentHelper
  {
    public static JsonPatchDocument<TModelTarget>? CreateCopyOfOperations<TModelSource, TModelTarget>(JsonPatchDocument<TModelSource> sourcePatchDocument)
    where TModelSource : class
    where TModelTarget : class
    {
      if (TypeComparer.HaveSameProperties<TModelSource, TModelTarget>())
      {
        var newPatchDocument = new JsonPatchDocument<TModelTarget>();

        foreach (var operation in sourcePatchDocument.Operations)
        {
          newPatchDocument.Operations.Add(new Operation<TModelTarget>(operation.op, operation.path, operation.from, operation.value));
        }

        return newPatchDocument;
      }

      return null;
    }

    public static JsonPatchDocument CreateCopyOfOperations(JsonPatchDocument sourcePatchDocument)
    {
      var newPatchDocument = new JsonPatchDocument();

      foreach (var operation in sourcePatchDocument.Operations)
      {
        newPatchDocument.Operations.Add(new Operation(operation.op, operation.path, operation.from, operation.value));
      }

      return newPatchDocument;
    }

    /// <summary>
    /// Guard for patch operations.
    /// </summary>
    /// <param name="model">ModelState dictionary.</param>
    /// <param name="patch">Strongly-typed JsonPatchDocument.</param>
    /// <param name="numAllowedOperations">Number of permissible operations.</param>
    /// <param name="curOperation">Type of allowed operation.</param>
    /// <exception cref="Exception"></exception>
    /// <example>JsonPatchDocumentHelper.ValidatePatch(model, patch, typeof(MyClass), 2, OperationType.Replace);</example>
    /// <returns>void</returns>
    public static void ValidatePatch<TModel>(ModelStateDictionary model, JsonPatchDocument<TModel> patch, int numAllowedOperations, OperationType curOperation) 
      where TModel : class
    {
      var currentModelInstance = Activator.CreateInstance<TModel>() ?? 
        throw new Exception("Could not create an instance of the current model.");

      //These tests should return immediately.
      if (patch.Operations.Count > numAllowedOperations)
      {
        model.AddModelError("PatchError", $"Patch document operations count ({patch.Operations.Count}) exceeds permissible operations ({numAllowedOperations}).");
        return;
      }

      if (patch.Operations.Any(p => p.OperationType != curOperation))
      {
        model.AddModelError("PatchError", $"Invalid Patch operation: '{patch.Operations.Where(p => p.OperationType != curOperation).First().op}'.");
        return;
      }

      foreach (var operation in patch.Operations)
      {
        if (!PathExists(currentModelInstance, operation.path))
        {
          model.AddModelError("PatchError", $"Invalid patch document: no match found for specified path: {operation.path}.");
          return;
        }

        //These tests can return lists of validation failures.
        switch (operation.op)
        {
          case "add":
          case "replace":
            IsValidForProperty(model, currentModelInstance, operation.path, operation.value);
            if (model.IsValid == true)
            {
              IsPropertyInRange(model, currentModelInstance, operation.path, operation.value);
            }
            break;
        }
      }
    }

    private static void IsValidForProperty<TModel>(ModelStateDictionary model, TModel currentModel, string path, object? val) where TModel : class
    {
      var property = currentModel.GetType().GetProperty(path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

      Type propertyType = property!.PropertyType;

      var success = propertyType.IsAssignableFrom(val!.GetType());

      if (success == false)
      {
        model.AddModelError($"{property.Name}", $"Value type '{val}' is not valid for property '{property.Name}'.");
      }
    }

    private static void IsPropertyInRange<TModel>(ModelStateDictionary model, TModel currentModel, string path, object? val) where TModel : class
    {
      var property = currentModel.GetType().GetProperty(path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

      foreach (var item in property!.GetCustomAttributes(false).OfType<ValidationAttribute>())
      {
        try
        {
          item.Validate(val, new ValidationContext(currentModel));
        }
        catch (ValidationException ex)
        {
          model.AddModelError(path, ex.Message);
        }
      }
    }

    private static bool PathExists<TModel>(TModel currentModel, string path) where TModel : class
    {
      var property = currentModel.GetType().GetProperty(path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

      if (property is null) return false;

      return true;
    }
  }
}
