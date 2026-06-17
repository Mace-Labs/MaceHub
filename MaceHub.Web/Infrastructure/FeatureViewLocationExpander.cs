using Microsoft.AspNetCore.Mvc.Razor;

namespace MaceHub.Web.Infrastructure;

/// <summary>
/// Resolves Razor views from vertical-slice folders under <c>Features/</c> using the
/// controller's namespace rather than its name, so sub-feature nesting that name-parsing
/// cannot express resolves correctly:
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
        var controllerType = (context.ActionContext.ActionDescriptor as
            Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)?.ControllerTypeInfo;

        var ns = controllerType?.Namespace;
        if (ns is not null && ns.StartsWith(FeaturesNamespacePrefix, StringComparison.Ordinal))
        {
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
            yield return $"/Features/{featurePath}/{{0}}.cshtml";

            // Probe the parent feature so a sub-feature can reuse a partial it owns.
            var lastSlash = featurePath.LastIndexOf('/');
            if (lastSlash > 0)
            {
                var parentPath = featurePath[..lastSlash];
                yield return $"/Features/{parentPath}/{{0}}.cshtml";
            }
        }

        yield return "/Common/Views/Shared/{0}.cshtml";

        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
}
