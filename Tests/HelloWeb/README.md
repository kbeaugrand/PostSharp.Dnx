This example shows how to use PostSharp with ASP.NET Core 1.0 in order to automatically encrypt or decrypt values.

We provide a base value filtering framework that can be easily extended. The only implemented algorithm in this example is string inversion, implemented in  class `ReverseAttribute`. 
You can implement more transformations by deriving from the `FilterAttribute` abstract class. Filters can be applied to parameters, instance fields and instance properties. Wher you apply
a filter on a parameter, the filter will be applied automatically when the method will be invoked. However, when applied on fields and properties, filters are *not* automatically applied. 
They are applied only when the `IFilterable.ApplyFilter` interface method is invoked. The framework also defines a filter named `ApplyFiltersAttribute`, which has the effect to apply filters recursively. 

Let's put the pieces of the puzzle together:

1. In `LoginViewModel`, we add `[Reverse]` to the `Password` property.
2. In `AccountController.Login`, we add `[ApplyFilters]` to the `model` parameter of type `LoginViewModel`. This causes the `LoginViewModel` object to be filtered, i.e. recursively the `Password` property will be decrypted. 

