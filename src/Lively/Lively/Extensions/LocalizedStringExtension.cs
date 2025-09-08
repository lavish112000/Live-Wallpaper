using Lively.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Markup;

namespace Lively.Extensions
{
    public class LocalizedStringExtension : MarkupExtension
    {
        public string ResourceKey { get; set; }

        public LocalizedStringExtension(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(ResourceKey))
                return string.Empty;

            // Return placeholder if in design mode
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
                return $"[{ResourceKey}]";

            try
            {
                var resourceService = App.Services?.GetRequiredService<IResourceService>();
                return resourceService?.GetString(ResourceKey) ?? $"!{ResourceKey}";
            }
            catch
            {
                return $"!{ResourceKey}";
            }
        }
    }
}
