using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetControl4BioMed.Data;
using NetControl4BioMed.Data.Enumerations;
using NetControl4BioMed.Data.Models;
using NetControl4BioMed.Helpers.InputModels;
using NetControl4BioMed.Helpers.Interfaces;
using NetControl4BioMed.Helpers.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NetControl4BioMed.Pages.AvailableData.Created.Networks
{
    public class DeleteModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IReCaptchaChecker _reCaptchaChecker;

        public DeleteModel(UserManager<User> userManager, ApplicationDbContext context, IReCaptchaChecker reCaptchaChecker)
        {
            _userManager = userManager;
            _context = context;
            _reCaptchaChecker = reCaptchaChecker;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public IEnumerable<string> Ids { get; set; }

            public string ReCaptchaToken { get; set; }
        }

        public ViewModel View { get; set; }

        public class ViewModel
        {
            public IEnumerable<Network> Items { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(IEnumerable<string> ids)
        {
            // Check if there aren't any IDs provided.
            if (ids == null || !ids.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No or invalid IDs have been provided.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Networks/Index");
            }
            // Get the current user.
            var user = await _userManager.GetUserAsync(User);
            // Check if there isn't any user found.
            if (user == null)
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: An error occured while trying to load the user data. If you are already logged in, please log out and try again.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Networks/Index");
            }
            // Define the view.
            View = new ViewModel
            {
                Items = _context.Networks
                    .Where(item => item.NetworkUsers.Any(item1 => item1.Type == NetworkUserType.Owner && item1.Email == user.Email))
                    .Where(item => ids.Contains(item.Id))
            };
            // Check if there weren't any items found.
            if (View.Items == null || !View.Items.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No networks have been found with the provided IDs, or you don't have access to them.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Networks/Index");
            }
            // Return the page.
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if there aren't any IDs provided.
            if (Input.Ids == null || !Input.Ids.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No or invalid IDs have been provided.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Networks/Index");
            }
            // Get the current user.
            var user = await _userManager.GetUserAsync(User);
            // Check if there isn't any user found.
            if (user == null)
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: An error occured while trying to load the user data. If you are already logged in, please log out and try again.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Networks/Index");
            }
            // Define the view.
            View = new ViewModel
            {
                Items = _context.Networks
                    .Where(item => item.NetworkUsers.Any(item1 => item1.Type == NetworkUserType.Owner && item1.Email == user.Email))
                    .Where(item => Input.Ids.Contains(item.Id))
            };
            // Check if there weren't any items found.
            if (View.Items == null || !View.Items.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No networks have been found with the provided IDs, or you don't have access to them.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Networks/Index");
            }
            // Check if the reCaptcha is valid.
            if (!await _reCaptchaChecker.IsValid(Input.ReCaptchaToken))
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "The reCaptcha verification failed.");
                // Return the page.
                return Page();
            }
            // Check if the provided model isn't valid.
            if (!ModelState.IsValid)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "An error has been encountered. Please check again the input fields.");
                // Redisplay the page.
                return Page();
            }
            // Save the number of items found.
            var itemCount = View.Items.Count();
            // Define a new task.
            var task = new BackgroundTask
            {
                DateTimeCreated = DateTime.UtcNow,
                Name = $"{nameof(IContentTaskManager)}.{nameof(IContentTaskManager.DeleteNetworksAsync)}",
                IsRecurring = false,
                Data = JsonSerializer.Serialize(new NetworksTask
                {
                    Items = View.Items.Select(item => new NetworkInputModel
                    {
                        Id = item.Id
                    })
                })
            };
            // Mark the task for addition.
            _context.BackgroundTasks.Add(task);
            // Save the changes to the database.
            await _context.SaveChangesAsync();
            // Create a new Hangfire background job.
            var jobId = BackgroundJob.Enqueue<IContentTaskManager>(item => item.DeleteNetworksAsync(task.Id, CancellationToken.None));
            // Display a message.
            TempData["StatusMessage"] = $"Success: A new background job was created to delete {itemCount} network{(itemCount != 1 ? "s" : string.Empty)}.";
            // Redirect to the index page.
            return RedirectToPage("/AvailableData/Created/Networks/Index");
        }
    }
}