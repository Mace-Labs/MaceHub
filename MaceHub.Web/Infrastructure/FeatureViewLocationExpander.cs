using Microsoft.AspNetCore.Mvc.Razor;

namespace MaceHub.Web.Infrastructure;

/// <summary>
/// Resolves Razor views from vertical-slice folders under <c>Features/</c>, driven
/// by the controller's <em>namespace</em> rather than its name. The namespace already
/// encodes the full feature path (including one level of sub-feature nesting), so it
/// expresses nesting that name-parsing (<c>ExampleController</c> → <c>Example</c>) cannot.
///
/// Examples of the resolved feature path:
///   MaceHub.Web.Features.Example             → "Example"
///   MaceHub.Web.Features.Example.Details     → "Example/Details"
///   MaceHub.Web.Features.Seo.Opportunities   → "Seo/Opportunities"
/// </summary>
public sealed class FeatureViewLocationExpander : IViewLocationExpander
{
    private const string FeaturesNamespacePrefix = "MaceHub.Web.Features.";
    private const string FeaturePathKey = "featurePath";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // The controller type's namespace is the source of truth for the feature path.
        var controllerType = (context.ActionContext.ActionDescriptor as
            Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)?.ControllerTypeInfo;

        var ns = controllerType?.Namespace;
        if (ns is not null && ns.StartsWith(FeaturesNamespacePrefix, StringComparison.Ordinal))
        {
            // "MaceHub.Web.Features.Example.Details" → "Example/Details"
            var relative = ns[FeaturesNamespacePrefix.Length..];
            context.Values[FeaturePathKey] = relative.Replace('.', '/');
        }
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(FeaturePathKey, out var featurePath) &&
            !string.IsNullOrEmpty(featurePath))
        {
            // 1. Feature-local views and partials (covers "_ExampleRow" resolving with no path).
            yield return $"/Features/{featurePath}/{{0}}.cshtml";

            // 2. Walk up one level so a sub-feature can use a partial owned by its parent
            //    feature (e.g. Example/Details reusing Example/_ExampleRow).
            var lastSlash = featurePath.LastIndexOf('/');
            if (lastSlash > 0)
            {
                var parentPath = featurePath[..lastSlash];
                yield return $"/Features/{parentPath}/{{0}}.cshtml";
            }
        }

        // 3. Shared, cross-slice layouts and partials.
        yield return "/Common/Views/Shared/{0}.cshtml";

        // Preserve the framework defaults as a final fallback.
        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
}
