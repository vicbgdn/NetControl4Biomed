using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using NetControl4BioMed.Data;
using NetControl4BioMed.Data.Models;
using NetControl4BioMed.Helpers.InputModels;
using NetControl4BioMed.Helpers.Interfaces;
using NetControl4BioMed.Helpers.Tasks;

namespace NetControl4BioMed.Pages.Administration.Databases.Databases
{
    [Authorize(Roles = "Administrator")]
    public class DeleteModel : PageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _context;

        public DeleteModel(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            _serviceProvider = serviceProvider;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public IEnumerable<string> Ids { get; set; }
        }

        public ViewModel View { get; set; }

        public class ViewModel
        {
            public IEnumerable<Database> Items { get; set; }
        }

        public IActionResult OnGet(IEnumerable<string> ids)
        {
            // Check if there aren't any IDs provided.
            if (ids == null || !ids.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No or invalid IDs have been provided.";
                // Redirect to the index page.
                return RedirectToPage("/Administration/Databases/Databases/Index");
            }
            // Define the view.
            View = new ViewModel
            {
                Items = _context.Databases
                    .Where(item => ids.Contains(item.Id))
            };
            // Check if there weren't any items found.
            if (View.Items == null || !View.Items.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No items have been found with the provided IDs.";
                // Redirect to the index page.
                return RedirectToPage("/Administration/Databases/Databases/Index");
            }
            // Check if the generic database is among the items to be deleted.
            if (View.Items.Any(item => item.DatabaseType.Name == "Generic"))
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: The generic database can't be deleted.";
                // Redirect to the index page.
                return RedirectToPage("/Administration/Databases/Databases/Index");
            }
            // Return the page.
            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if there aren't any IDs provided.
            if (Input.Ids == null || !Input.Ids.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No or invalid IDs have been provided.";
                // Redirect to the index page.
                return RedirectToPage("/Administration/Databases/Databases/Index");
            }
            // Define the view.
            View = new ViewModel
            {
                Items = _context.Databases
                    .Where(item => Input.Ids.Contains(item.Id))
            };
            // Check if there weren't any items found.
            if (View.Items == null || !View.Items.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: No items have been found with the provided IDs.";
                // Redirect to the index page.
                return RedirectToPage("/Administration/Databases/Databases/Index");
            }
            // Check if the generic database is among the items to be deleted.
            if (View.Items.Any(item => item.DatabaseType.Name == "Generic"))
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: The generic database can't be deleted.";
                // Redirect to the index page.
                return RedirectToPage("/Administration/Databases/Databases/Index");
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
                Name = $"{nameof(IAdministrationTaskManager)}.{nameof(IAdministrationTaskManager.DeleteDatabases)}",
                IsRecurring = false,
                Data = JsonSerializer.Serialize(new DatabasesTask
                {
                    Items = View.Items.Select(item => new DatabaseInputModel
                    {
                        Id = item.Id
                    })
                }, new JsonSerializerOptions { IgnoreNullValues = true })
            };
            // Mark the task for addition.
            _context.BackgroundTasks.Add(task);
            // Save the changes to the database.
            _context.SaveChanges();
            // Create a new Hangfire background job.
            var jobId = BackgroundJob.Enqueue<IAdministrationTaskManager>(item => item.DeleteDatabases(task.Id, CancellationToken.None));
            // Display a message.
            TempData["StatusMessage"] = $"Success: A new background job was created to delete {itemCount} database{(itemCount != 1 ? "s" : string.Empty)}.";
            // Redirect to the index page.
            return RedirectToPage("/Administration/Databases/Databases/Index");
        }
    }
}
