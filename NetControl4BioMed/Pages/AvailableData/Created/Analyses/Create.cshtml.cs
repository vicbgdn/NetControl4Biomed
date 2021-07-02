using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetControl4BioMed.Data;
using NetControl4BioMed.Data.Enumerations;
using NetControl4BioMed.Data.Models;
using NetControl4BioMed.Helpers.Extensions;
using NetControl4BioMed.Helpers.InputModels;
using NetControl4BioMed.Helpers.Interfaces;
using NetControl4BioMed.Helpers.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnumerationProteinCollectionType = NetControl4BioMed.Data.Enumerations.ProteinCollectionType;
using GeneticAlgorithm = NetControl4BioMed.Helpers.Algorithms.Analyses.Genetic;
using GreedyAlgorithm = NetControl4BioMed.Helpers.Algorithms.Analyses.Greedy;

namespace NetControl4BioMed.Pages.AvailableData.Created.Analyses
{
    [RequestFormLimits(ValueLengthLimit = 16 * 1024 * 1024)]
    public class CreateModel : PageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IReCaptchaChecker _reCaptchaChecker;

        public CreateModel(IServiceProvider serviceProvider, UserManager<User> userManager, ApplicationDbContext context, IConfiguration configuration, IReCaptchaChecker reCaptchaChecker)
        {
            _serviceProvider = serviceProvider;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _reCaptchaChecker = reCaptchaChecker;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel : IValidatableObject
        {
            [DataType(DataType.Text)]
            [Required(ErrorMessage = "This field is required.")]
            public string Name { get; set; }

            [DataType(DataType.MultilineText)]
            public string Description { get; set; }

            [DataType(DataType.Text)]
            [Required(ErrorMessage = "This field is required.")]
            public bool IsPublic { get; set; }

            [DataType(DataType.Text)]
            [Required(ErrorMessage = "This field is required.")]
            public string NetworkId { get; set; }

            [DataType(DataType.MultilineText)]
            [Required(ErrorMessage = "This field is required.")]
            public string SourceProteinData { get; set; }

            [DataType(DataType.MultilineText)]
            [Required(ErrorMessage = "This field is required.")]
            public string SourceProteinCollectionData { get; set; }

            [DataType(DataType.MultilineText)]
            [Required(ErrorMessage = "This field is required.")]
            public string TargetProteinData { get; set; }

            [DataType(DataType.MultilineText)]
            [Required(ErrorMessage = "This field is required.")]
            public string TargetProteinCollectionData { get; set; }

            [Range(0, 10000, ErrorMessage = "The value must be a positive integer lower than 10000.")]
            [Required(ErrorMessage = "This field is required.")]
            public int MaximumIterations { get; set; }

            [Range(0, 1000, ErrorMessage = "The value must be a positive integer lower than 1000.")]
            [Required(ErrorMessage = "This field is required.")]
            public int MaximumIterationsWithoutImprovement { get; set; }

            [DataType(DataType.Text)]
            [Required(ErrorMessage = "This field is required.")]
            public string Algorithm { get; set; }

            public GreedyAlgorithm.Parameters GreedyAlgorithmParameters { get; set; }

            public GeneticAlgorithm.Parameters GeneticAlgorithmParameters { get; set; }

            [DataType(DataType.Text)]
            [Required(ErrorMessage = "This field is required.")]
            public string ReCaptchaToken { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                // Check the selected algorithm.
                if (Algorithm == AnalysisAlgorithm.Greedy.ToString())
                {
                    // Check if the parameters don't match the algorithm.
                    if (GreedyAlgorithmParameters == null)
                    {
                        // Return an error.
                        yield return new ValidationResult("The parameters do not match the chosen algorithm.", new List<string> { string.Empty });
                    }
                    // Get the validation results for the parameters.
                    var validationResults = GreedyAlgorithmParameters.Validate(validationContext);
                    // Go over each validation error.
                    foreach (var validationResult in validationResults)
                    {
                        // Return an error.
                        yield return new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(item => $"Input.{nameof(GreedyAlgorithmParameters)}.{item}"));
                    }
                }
                else if (Algorithm == AnalysisAlgorithm.Genetic.ToString())
                {
                    // Check if the parameters don't match the algorithm.
                    if (GeneticAlgorithmParameters == null)
                    {
                        // Return an error.
                        yield return new ValidationResult("The parameters do not match the chosen algorithm.", new List<string> { string.Empty });
                    }
                    // Get the validation results for the parameters.
                    var validationResults = GeneticAlgorithmParameters.Validate(validationContext);
                    // Go over each validation error.
                    foreach (var validationResult in validationResults)
                    {
                        // Return an error.
                        yield return new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(item => $"Input.{nameof(GeneticAlgorithmParameters)}.{item}"));
                    }
                }
            }
        }

        public ViewModel View { get; set; }

        public class ViewModel
        {
            public ItemModel Network { get; set; }

            public bool HasNetworkDatabases { get; set; }

            public IEnumerable<ItemModel> SourceProteinCollections { get; set; }

            public IEnumerable<ItemModel> TargetProteinCollections { get; set; }
        }

        public class ItemModel
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string networkId, string analysisId, bool loadDemonstration)
        {
            // Check if the demonstration should be loaded.
            if (loadDemonstration)
            {
                // Check if there are no demonstration items configured.
                if (string.IsNullOrEmpty(_configuration["Data:Demonstration:AnalysisId"]))
                {
                    // Try to get a demonstration control path.
                    var controlPath = _context.ControlPaths
                        .Include(item => item.Analysis)
                            .ThenInclude(item => item.Network)
                        .Where(item => item.Analysis.IsPublic && item.Analysis.IsDemonstration && item.Analysis.Network.IsPublic && item.Analysis.Network.IsDemonstration)
                        .AsNoTracking()
                        .FirstOrDefault();
                    // Check if there was no demonstration control path found.
                    if (controlPath == null || controlPath.Analysis == null || controlPath.Analysis.Network == null)
                    {
                        // Display a message.
                        TempData["StatusMessage"] = "Error: There are no demonstration analyses available.";
                        // Redirect to the index page.
                        return RedirectToPage("/AvailableData/Created/Analyses/Index");
                    }
                    // Update the demonstration item IDs.
                    _configuration["Data:Demonstration:NetworkId"] = controlPath.Analysis.Network.Id;
                    _configuration["Data:Demonstration:AnalysisId"] = controlPath.Analysis.Id;
                    _configuration["Data:Demonstration:ControlPathId"] = controlPath.Id;
                }
                // Get the IDs of the configured demonstration item.
                networkId = _configuration["Data:Demonstration:NetworkId"];
                analysisId = _configuration["Data:Demonstration:AnalysisId"];
            }
            // Get the current user.
            var user = await _userManager.GetUserAsync(User);
            // Define the view.
            View = new ViewModel { };
            // Check if there was an analysis provided.
            if (!string.IsNullOrEmpty(analysisId))
            {
                // Try to get the analysis with the provided ID.
                var analyses = _context.Analyses
                    .Where(item => item.IsPublic || (user != null && item.AnalysisUsers.Any(item1 => item1.Email == user.Email)))
                    .Where(item => item.Id == analysisId);
                // Check if there was an ID provided, but there was no analysis found.
                if (analyses == null || !analyses.Any())
                {
                    // Display a message.
                    TempData["StatusMessage"] = "Error: The specified analysis could not be found, or you don't have access to it.";
                    // Redirect to the index page.
                    return RedirectToPage("/AvailableData/Created/Analyses/Index");
                }
                // Update the view.
                View.Network = analyses
                    .Select(item => item.Network)
                    .Select(item => new ItemModel
                    {
                        Id = item.Id,
                        Name = item.Name
                    })
                    .FirstOrDefault();
                View.HasNetworkDatabases = _context.NetworkDatabases
                    .Any(item => item.Network.Id == View.Network.Id);
                View.SourceProteinCollections = View.HasNetworkDatabases ? _context.ProteinCollections
                    .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Source))
                    .Select(item => new ItemModel
                    {
                        Id = item.Id,
                        Name = item.Name
                    }) : Enumerable.Empty<ItemModel>();
                View.TargetProteinCollections = View.HasNetworkDatabases ? _context.ProteinCollections
                    .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Target))
                    .Select(item => new ItemModel
                    {
                        Id = item.Id,
                        Name = item.Name
                    }) : Enumerable.Empty<ItemModel>();
                // Check if there wasn't any network found.
                if (View.Network == null)
                {
                    // Display a message.
                    TempData["StatusMessage"] = "Error: The network corresponding to the specified analysis could not be found, or you do not have access to it.";
                    // Redirect to the index page.
                    return RedirectToPage("/AvailableData/Created/Analyses/Index");
                }
                // Define the input.
                Input = new InputModel
                {
                    Name = analyses
                        .Select(item => item.Name)
                        .FirstOrDefault(),
                    Description = analyses
                        .Select(item => item.Description)
                        .FirstOrDefault(),
                    IsPublic = user == null,
                    NetworkId = View.Network.Id,
                    SourceProteinData = JsonSerializer.Serialize(analyses
                        .Select(item => item.AnalysisProteins)
                        .SelectMany(item => item)
                        .Where(item => item.Type == AnalysisProteinType.Source)
                        .Select(item => item.Protein.Name)),
                    SourceProteinCollectionData = JsonSerializer.Serialize(analyses
                        .Select(item => item.AnalysisProteinCollections)
                        .SelectMany(item => item)
                        .Where(item => item.Type == AnalysisProteinCollectionType.Source)
                        .Select(item => item.ProteinCollection.Id)
                        .AsEnumerable()
                        .Intersect(View.SourceProteinCollections.Select(item => item.Id))),
                    TargetProteinData = JsonSerializer.Serialize(analyses
                        .Select(item => item.AnalysisProteins)
                        .SelectMany(item => item)
                        .Where(item => item.Type == AnalysisProteinType.Target)
                        .Select(item => item.Protein.Name)),
                    TargetProteinCollectionData = JsonSerializer.Serialize(analyses
                        .Select(item => item.AnalysisProteinCollections)
                        .SelectMany(item => item)
                        .Where(item => item.Type == AnalysisProteinCollectionType.Target)
                        .Select(item => item.ProteinCollection.Id)
                        .AsEnumerable()
                        .Intersect(View.TargetProteinCollections.Select(item => item.Id))),
                    MaximumIterations = analyses
                        .Select(item => item.MaximumIterations)
                        .FirstOrDefault(),
                    MaximumIterationsWithoutImprovement = analyses
                        .Select(item => item.MaximumIterationsWithoutImprovement)
                        .FirstOrDefault(),
                    Algorithm = analyses
                        .Select(item => item.Algorithm)
                        .FirstOrDefault()
                        .ToString()
                };
                // Update the parameters.
                Input.GreedyAlgorithmParameters = Input.Algorithm == AnalysisAlgorithm.Greedy.ToString() ? JsonSerializer.Deserialize<GreedyAlgorithm.Parameters>(analyses.Select(item => item.Parameters).FirstOrDefault()) : new GreedyAlgorithm.Parameters();
                Input.GeneticAlgorithmParameters = Input.Algorithm == AnalysisAlgorithm.Genetic.ToString() ? JsonSerializer.Deserialize<GeneticAlgorithm.Parameters>(analyses.Select(item => item.Parameters).FirstOrDefault()) : new GeneticAlgorithm.Parameters();
                // Display a message.
                TempData["StatusMessage"] = "Success: The analysis has been loaded successfully.";
                // Return the page.
                return Page();
            }
            // Check if there wasn't a network provided.
            if (string.IsNullOrEmpty(networkId))
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: A network is required to create an analysis.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
            }
            // Update the view.
            View.Network = _context.Networks
                .Where(item => item.IsPublic || item.NetworkUsers.Any(item1 => item1.Email == user.Email))
                .Where(item => item.Id == networkId)
                .Select(item => new ItemModel
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .FirstOrDefault();
            View.HasNetworkDatabases = _context.NetworkDatabases
                .Any(item => item.Network.Id == View.Network.Id);
            View.SourceProteinCollections = View.HasNetworkDatabases ? _context.ProteinCollections
                .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Source))
                .Select(item => new ItemModel
                {
                    Id = item.Id,
                    Name = item.Name
                }) : Enumerable.Empty<ItemModel>();
            View.TargetProteinCollections = View.HasNetworkDatabases ? _context.ProteinCollections
                .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Target))
                .Select(item => new ItemModel
                {
                    Id = item.Id,
                    Name = item.Name
                }) : Enumerable.Empty<ItemModel>();
            // Check if there wasn't any network found.
            if (View.Network == null)
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: The specified network could not be found, or you do not have access to it.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
            }
            // Define the input.
            Input = new InputModel
            {
                IsPublic = user == null,
                NetworkId = View.Network.Id,
                SourceProteinData = JsonSerializer.Serialize(Enumerable.Empty<string>()),
                SourceProteinCollectionData = JsonSerializer.Serialize(Enumerable.Empty<string>()),
                TargetProteinData = JsonSerializer.Serialize(Enumerable.Empty<string>()),
                TargetProteinCollectionData = JsonSerializer.Serialize(Enumerable.Empty<string>()),
                MaximumIterations = 100,
                MaximumIterationsWithoutImprovement = 25,
                Algorithm = AnalysisAlgorithm.Greedy.ToString(),
                GreedyAlgorithmParameters = new GreedyAlgorithm.Parameters(),
                GeneticAlgorithmParameters = new GeneticAlgorithm.Parameters()
            };
            // Return the page.
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get the current user.
            var user = await _userManager.GetUserAsync(User);
            // Define the view.
            View = new ViewModel
            {
                SourceProteinCollections = _context.ProteinCollections
                    .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Source))
                    .Select(item => new ItemModel
                    {
                        Id = item.Id,
                        Name = item.Name
                    }),
                TargetProteinCollections = _context.ProteinCollections
                    .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Target))
                    .Select(item => new ItemModel
                    {
                        Id = item.Id,
                        Name = item.Name
                    })
            };
            // Check if there wasn't a network provided.
            if (string.IsNullOrEmpty(Input.NetworkId))
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: A network is required to create an analysis.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
            }
            // Update the view.
            View.Network = _context.Networks
                .Where(item => item.IsPublic || item.NetworkUsers.Any(item1 => item1.Email == user.Email))
                .Where(item => item.Id == Input.NetworkId)
                .Select(item => new ItemModel
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .FirstOrDefault();
            View.HasNetworkDatabases = _context.NetworkDatabases
                .Any(item => item.Network.Id == View.Network.Id);
            View.SourceProteinCollections = View.HasNetworkDatabases ? _context.ProteinCollections
                .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Source))
                .Select(item => new ItemModel
                {
                    Id = item.Id,
                    Name = item.Name
                }) : Enumerable.Empty<ItemModel>();
            View.TargetProteinCollections = View.HasNetworkDatabases ? _context.ProteinCollections
                .Where(item => item.ProteinCollectionTypes.Any(item1 => item1.Type == EnumerationProteinCollectionType.Target))
                .Select(item => new ItemModel
                {
                    Id = item.Id,
                    Name = item.Name
                }) : Enumerable.Empty<ItemModel>();
            // Update the parameters.
            Input.GreedyAlgorithmParameters = Input.GreedyAlgorithmParameters ?? new GreedyAlgorithm.Parameters();
            Input.GeneticAlgorithmParameters = Input.GeneticAlgorithmParameters ?? new GeneticAlgorithm.Parameters();
            // Check if there wasn't any network found.
            if (View.Network == null)
            {
                // Display a message.
                TempData["StatusMessage"] = "Error: The specified network could not be found, or you do not have access to it.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
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
                // Get the validation errors.
                var validationErrors = ModelState.Values.Select(item => item.Errors).SelectMany(item => item).ToList();
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "An error has been encountered. Please check again the input fields.");
                // Go over each validation error.
                foreach (var validationError in validationErrors)
                {
                    // Add an error to the model.
                    ModelState.AddModelError(string.Empty, validationError.ErrorMessage);
                }
                // Redisplay the page.
                return Page();
            }
            // Check if the public availability isn't valid.
            if (user == null && !Input.IsPublic)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "You are not logged in, so the analysis must be set as public.");
                // Redisplay the page.
                return Page();
            }
            // Try to get the algorithm.
            try
            {
                // Get the algorithm.
                var algorithm = EnumerationExtensions.GetEnumerationValue<AnalysisAlgorithm>(Input.Algorithm);
            }
            catch (Exception)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, $"The analysis algorithm couldn't be determined from the provided string.");
                // Redisplay the page.
                return Page();
            }
            // Try to deserialize the source data.
            if (!Input.SourceProteinData.TryDeserializeJsonObject<IEnumerable<string>>(out var sourceProteins) || sourceProteins == null)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "The provided source data could not be deserialized.");
                // Redisplay the page.
                return Page();
            }
            // Try to deserialize the source protein collection data.
            if (!Input.SourceProteinCollectionData.TryDeserializeJsonObject<IEnumerable<string>>(out var sourceProteinCollectionIds) || sourceProteinCollectionIds == null)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "The provided source protein collection data could not be deserialized.");
                // Redisplay the page.
                return Page();
            }
            // Try to get the source protein collections with the provided IDs.
            var sourceProteinCollections = View.SourceProteinCollections
                .Where(item => sourceProteinCollectionIds.Contains(item.Id));
            // Try to deserialize the target data.
            if (!Input.TargetProteinData.TryDeserializeJsonObject<IEnumerable<string>>(out var targetProteins) || targetProteins == null)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "The provided target data could not be deserialized.");
                // Redisplay the page.
                return Page();
            }
            // Try to deserialize the target protein collection data.
            if (!Input.TargetProteinCollectionData.TryDeserializeJsonObject<IEnumerable<string>>(out var targetProteinCollectionIds) || targetProteinCollectionIds == null)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "The provided target protein collection data could not be deserialized.");
                // Redisplay the page.
                return Page();
            }
            // Try to get the target protein collections with the provided IDs.
            var targetProteinCollections = View.TargetProteinCollections
                .Where(item => targetProteinCollectionIds.Contains(item.Id));
            // Check if there wasn't any target data found.
            if (!targetProteins.Any() && !targetProteinCollections.Any())
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, "No target data has been provided or none of the target protein collections could be found.");
                // Redisplay the page.
                return Page();
            }
            // Serialize the seed data.
            var data = JsonSerializer.Serialize(sourceProteins
                .Select(item => new AnalysisProteinInputModel
                {
                    Protein = new ProteinInputModel
                    {
                        Id = item
                    },
                    Type = "Source"
                })
                .Concat(targetProteins
                    .Select(item => new AnalysisProteinInputModel
                    {
                        Protein = new ProteinInputModel
                        {
                            Id = item
                        },
                        Type = "Target"
                    })));
            // Define a new task.
            var task = new AnalysesTask
            {
                Scheme = HttpContext.Request.Scheme,
                HostValue = HttpContext.Request.Host.Value,
                Items = new List<AnalysisInputModel>
                {
                    new AnalysisInputModel
                    {
                        Name = Input.Name,
                        Description = Input.Description,
                        IsPublic = Input.IsPublic,
                        Data = data,
                        MaximumIterations = Input.MaximumIterations,
                        MaximumIterationsWithoutImprovement = Input.MaximumIterationsWithoutImprovement,
                        Algorithm = Input.Algorithm,
                        Parameters = Input.Algorithm == AnalysisAlgorithm.Greedy.ToString() ? JsonSerializer.Serialize(Input.GreedyAlgorithmParameters) :
                            Input.Algorithm == AnalysisAlgorithm.Genetic.ToString() ? JsonSerializer.Serialize(Input.GeneticAlgorithmParameters) :
                            null,
                        Network = new NetworkInputModel
                        {
                            Id = Input.NetworkId
                        },
                        AnalysisUsers = user != null ?
                            new List<AnalysisUserInputModel>
                            {
                                new AnalysisUserInputModel
                                {
                                    User = new UserInputModel
                                    {
                                        Id = user.Id
                                    },
                                    Email = user.Email
                                }
                            } :
                            new List<AnalysisUserInputModel>(),
                        AnalysisProteinCollections = sourceProteinCollections
                            .Select(item => item.Id)
                            .Select(item => new AnalysisProteinCollectionInputModel
                            {
                                ProteinCollection = new ProteinCollectionInputModel
                                {
                                    Id = item
                                },
                                Type = "Source"
                            })
                            .Concat(targetProteinCollections
                                .Select(item => item.Id)
                                .Select(item => new AnalysisProteinCollectionInputModel
                                {
                                    ProteinCollection = new ProteinCollectionInputModel
                                    {
                                        Id = item
                                    },
                                    Type = "Target"
                                }))
                    }
                }
            };
            // Define the IDs of the created items.
            var ids = Enumerable.Empty<string>();
            // Try to run the task.
            try
            {
                // Run the task.
                ids = await task.CreateAsync(_serviceProvider, CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Add an error to the model.
                ModelState.AddModelError(string.Empty, exception.Message);
                // Redisplay the page.
                return Page();
            }
            // Check if there wasn't any ID returned.
            if (ids == null || !ids.Any())
            {
                // Display a message.
                TempData["StatusMessage"] = $"Success: 1 analysis defined successfully and scheduled for generation.";
                // Redirect to the index page.
                return RedirectToPage("/AvailableData/Created/Analyses/Index");
            }
            // Display a message.
            TempData["StatusMessage"] = $"Success: 1 analysis defined successfully with the ID \"{ids.First()}\" and scheduled for generation.";
            // Redirect to the index page.
            return RedirectToPage("/AvailableData/Created/Analyses/Details/Index", new { id = ids.First() });
        }
    }
}