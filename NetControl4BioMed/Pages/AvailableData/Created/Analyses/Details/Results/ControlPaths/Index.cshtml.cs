using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NetControl4BioMed.Data;
using NetControl4BioMed.Data.Enumerations;
using NetControl4BioMed.Data.Models;
using NetControl4BioMed.Helpers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetControl4BioMed.Pages.AvailableData.Created.Analyses.Details.Results.ControlPaths
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly LinkGenerator _linkGenerator;

        public IndexModel(UserManager<User> userManager, ApplicationDbContext context, LinkGenerator linkGenerator)
        {
            _userManager = userManager;
            _context = context;
            _linkGenerator = linkGenerator;
        }

        public ViewModel View { get; set; }

        public class ViewModel
        {
            public Analysis Analysis { get; set; }

            public HashSet<Protein> SourceProteins { get; set; }

            public SearchViewModel<ControlPath> Search { get; set; }

            public static SearchOptionsViewModel SearchOptions { get; } = new SearchOptionsViewModel
            {
                SearchIn = new Dictionary<string, string>
                {
                    { "Id", "ID" },
                    { "ProteinId", "Protein ID" },
                    { "ProteinName", "Protein name" },
                    { "SourceProteinId", "Source protein ID" },
                    { "SourceProteinName", "Source protein name" },
                    { "TargetProteinId", "Target protein ID" },
                    { "TargetProteinName", "Target protein name" },
                    { "InteractionId", "Interaction ID" },
                    { "InteractionName", "Interaction name" }
                },
                Filter = new Dictionary<string, string>
                {
                },
                SortBy = new Dictionary<string, string>
                {
                    { "Id", "ID" },
                    { "ControlProteinCount", "Number of control proteins" }
                }
            };
        }

        public async Task<IActionResult> OnGetAsync(string id, string searchString = null, IEnumerable<string> searchIn = null, IEnumerable<string> filter = null, string sortBy = null, string sortDirection = null, int? itemsPerPage = null, int? currentPage = 1)
        {
            // Check if there isn't any ID provided.
            if (string.IsNullOrEmpty(id))
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No ID has been provided.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
            }
            // Get the current user.
            var user = await _userManager.GetUserAsync(User);
            // Get the items with the provided ID.
            var items = _context.Analyses
                .Where(item => item.IsPublic || (user != null && item.AnalysisUsers.Any(item1 => item1.Email == user.Email)))
                .Where(item => item.Id == id);
            // Check if there were no items found.
            if (items == null || items.Count() != 1)
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No item has been found with the provided ID, or you don't have access to it.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
            }
            // Define the search input.
            var input = new SearchInputViewModel(ViewModel.SearchOptions, id, searchString, searchIn, filter, sortBy, sortDirection, itemsPerPage, currentPage);
            // Check if any of the provided variables was null before the reassignment.
            if (input.NeedsRedirect)
            {
                // Redirect to the page where they are all explicitly defined.
                return RedirectToPage(new { id = input.Id, searchString = input.SearchString, searchIn = input.SearchIn, filter = input.Filter, sortBy = input.SortBy, sortDirection = input.SortDirection, itemsPerPage = input.ItemsPerPage, currentPage = input.CurrentPage });
            }
            // Start with all of the items.
            var query = items
                .Select(item => item.ControlPaths)
                .SelectMany(item => item);
            // Select the results matching the search string.
            query = query
                .Where(item => !input.SearchIn.Any() ||
                    input.SearchIn.Contains("Id") && item.Id.Contains(input.SearchString) ||
                    input.SearchIn.Contains("ProteinId") && item.Paths.Any(item1 => item1.PathProteins.Any(item2 => item2.Protein.Id.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("ProteinName") && item.Paths.Any(item1 => item1.PathProteins.Any(item2 => item2.Protein.Name.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("SourceProteinId") && item.Paths.Any(item1 => item1.PathProteins.Where(item2 => item2.Type == PathProteinType.Source).Any(item2 => item2.Protein.Id.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("SourceProteinName") && item.Paths.Any(item1 => item1.PathProteins.Where(item2 => item2.Type == PathProteinType.Source).Any(item2 => item2.Protein.Name.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("TargetProteinId") && item.Paths.Any(item1 => item1.PathProteins.Where(item2 => item2.Type == PathProteinType.Target).Any(item2 => item2.Protein.Id.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("TargetProteinName") && item.Paths.Any(item1 => item1.PathProteins.Where(item2 => item2.Type == PathProteinType.Target).Any(item2 => item2.Protein.Name.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("InteractionId") && item.Paths.Any(item1 => item1.PathInteractions.Any(item2 => item2.Interaction.Id.Contains(input.SearchString))) ||
                    input.SearchIn.Contains("InteractionName") && item.Paths.Any(item1 => item1.PathInteractions.Any(item2 => item2.Interaction.Name.Contains(input.SearchString))));
            // Sort it according to the parameters.
            switch ((input.SortBy, input.SortDirection))
            {
                case var sort when sort == ("Id", "Ascending"):
                    query = query.OrderBy(item => item.Id);
                    break;
                case var sort when sort == ("Id", "Descending"):
                    query = query.OrderByDescending(item => item.Id);
                    break;
                case var sort when sort == ("ControlProteinCount", "Ascending"):
                    query = query.OrderBy(item => item.Paths.Select(item1 => item1.PathProteins).SelectMany(item1 => item1).Where(item1 => item1.Type == PathProteinType.Source).Select(item1 => item1.Protein).Distinct().Count());
                    break;
                case var sort when sort == ("ControlProteinCount", "Descending"):
                    query = query.OrderByDescending(item => item.Paths.Select(item1 => item1.PathProteins).SelectMany(item1 => item1).Where(item1 => item1.Type == PathProteinType.Source).Select(item1 => item1.Protein).Distinct().Count());
                    break;
                default:
                    break;
            }
            // Include the related entities.
            query = query
                .Include(item => item.Paths)
                    .ThenInclude(item => item.PathProteins)
                        .ThenInclude(item => item.Protein);
            // Define the view.
            View = new ViewModel
            {
                Analysis = items
                    .First(),
                SourceProteins = items
                    .Select(item => item.AnalysisProteins)
                    .SelectMany(item => item)
                    .Where(item => item.Type == AnalysisProteinType.Source)
                    .Select(item => item.Protein)
                    .Distinct()
                    .ToHashSet(),
                Search = new SearchViewModel<ControlPath>(_linkGenerator, HttpContext, input, query)
            };
            // Return the page.
            return Page();
        }
    }
}