using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Web;

namespace LingapDVO.Filters
{
    /// <summary>
    /// Action filter that automatically sanitizes all string inputs in the model.
    /// It trims whitespace and strips HTML tags to prevent XSS.
    /// </summary>
    public class SanitizeFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                SanitizeObject(argument);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }

        private void SanitizeObject(object obj)
        {
            if (obj == null) return;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanWrite && p.CanRead);

            foreach (var property in properties)
            {
                var value = property.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    // 1. Trim whitespace
                    var sanitizedValue = value.Trim();

                    // 2. Strip HTML tags (Basic XSS prevention)
                    sanitizedValue = StripHtmlTags(sanitizedValue);

                    // 3. Update the property
                    property.SetValue(obj, sanitizedValue);
                }
            }
        }

        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Simple regex-based tag stripping
            // For more robust needs, use Ganss.Xss.HtmlSanitizer
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
