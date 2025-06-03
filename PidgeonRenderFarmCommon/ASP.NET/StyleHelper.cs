using System.Reflection;
using Microsoft.AspNetCore.Html;

namespace PidgeonRenderFarm.Common.ASP.NET;

public class StyleHelper
{
    public static async Task<IHtmlContent> GetDefaultStylesAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("styles.css"));

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();

                string styles = $"<style>\n{result}\n</style>";

                return new HtmlContentBuilder().AppendHtml(styles);
            }
        }
    }
}