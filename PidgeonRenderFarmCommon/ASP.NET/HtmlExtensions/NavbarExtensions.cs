using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using PidgeonRenderFarm.Common.Models;

namespace PidgeonRenderFarm.Common.ASP.NET.HtmlExtensions;

public static class NavbarExtensions
{
    public static async Task<IHtmlContent> DisplayNavbarDropdown(this IHtmlHelper html, string header, Dictionary<string, string> items, string? headerLink = null)
    {
        NavbarDropdownModel model = new(header, items, headerLink);
        return await html.PartialAsync("Partial/_NavbarDropdown", model);
    }
    public static async Task<IHtmlContent> DisplayNavbarDropdown(this IHtmlHelper html, NavbarDropdownModel model)
    {
        return await html.PartialAsync("Partial/_NavbarDropdown", model);
    }
}