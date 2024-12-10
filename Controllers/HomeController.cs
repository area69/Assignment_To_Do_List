using Assignment_To_Do_List.Models; // Reference to the TaskModel class
using ClosedXML.Excel; // Library for handling Excel files
using Microsoft.AspNetCore.Mvc; // For MVC features
namespace Assignment_To_Do_List.Controllers
{
    public class HomeController : Controller
    {
        // Static list to simulate an in-memory database for tasks
        private static List<TaskModel> tasks = new List<TaskModel>();

        // Display the list of tasks in the view
        public IActionResult Index()
        {
            return View(tasks); // Pass the tasks list to the view for display
        }

        // Action to add a new task
        [HttpPost]
        public IActionResult AddTask(TaskModel task)
        {
            if (ModelState.IsValid)
            {
                // Simple way to generate unique IDs
                task.Id = tasks.Count + 1;
                tasks.Add(task); // Add the new task to the in-memory list
                return RedirectToAction("Index"); // Redirect to the Index view to display the updated list
            }
            return View("Index", tasks); // Show the Index view with validation errors if the task is invalid
        }

        // Action to display the edit form for an existing task
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var task = tasks.Find(t => t.Id == id); // Find the task by its ID
            if (task == null) return NotFound(); // Return a 404 if the task is not found
            return PartialView("_TaskForm", task); // Return the task data to the partial view for editing
        }

        // Action to update an existing task
        [HttpPost]
        public IActionResult EditTask(int editId, TaskModel task)
        {
            if (ModelState.IsValid)
            {
                var existingTask = tasks.Find(t => t.Id == editId); // Find the task by ID
                if (existingTask != null)
                {
                    // Update task properties with the new values
                    existingTask.Title = task.Title;
                    existingTask.AssignedTo = task.AssignedTo;
                    existingTask.Details = task.Details;
                    existingTask.DateStarted = task.DateStarted;
                    existingTask.DateOfCompletion = task.DateOfCompletion;
                    existingTask.Status = task.Status;

                    return RedirectToAction("Index"); // Redirect to the Index page to refresh the list
                }
            }
            return View("Index", tasks); // Return to the Index view if the task is not found or data is invalid
        }

        // Action to delete a task
        public IActionResult Delete(int id)
        {
            var task = tasks.Find(t => t.Id == id); // Find the task by ID
            if (task != null)
            {
                tasks.Remove(task); // Remove the task from the list
            }
            return RedirectToAction("Index"); // Redirect to the Index view to refresh the list
        }

        // Action to export the tasks to an Excel file
        [HttpGet]
        public IActionResult ExportToExcel()
        {
            using (var workbook = new XLWorkbook()) // Create a new Excel workbook
            {
                var worksheet = workbook.Worksheets.Add("Tasks"); // Add a worksheet called "Tasks"
                var currentRow = 1; // Start at the first row

                // Add headers to the Excel file
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Title";
                worksheet.Cell(currentRow, 3).Value = "Assigned To";
                worksheet.Cell(currentRow, 4).Value = "Date Started";
                worksheet.Cell(currentRow, 5).Value = "Date of Completion";
                worksheet.Cell(currentRow, 6).Value = "Status";
                worksheet.Cell(currentRow, 7).Value = "Details";

                // Add task data to the Excel file
                foreach (var task in tasks)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = task.Id;
                    worksheet.Cell(currentRow, 2).Value = task.Title;
                    worksheet.Cell(currentRow, 3).Value = task.AssignedTo;
                    worksheet.Cell(currentRow, 4).Value = task.DateStarted?.ToString("yyyy-MM-dd");
                    worksheet.Cell(currentRow, 5).Value = task.DateOfCompletion?.ToString("yyyy-MM-dd");
                    worksheet.Cell(currentRow, 6).Value = task.Status;
                    worksheet.Cell(currentRow, 7).Value = task.Details;
                }

                // Format the date columns for better display in Excel
                worksheet.Column(4).Style.NumberFormat.Format = "yyyy-MM-dd";
                worksheet.Column(5).Style.NumberFormat.Format = "yyyy-MM-dd";

                // Save the file to a memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream); // Save workbook content to stream
                    var fileBytes = stream.ToArray(); // Convert stream to byte array

                    // Return the file to the user as a downloadable Excel file
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Tasks.xlsx");
                }
            }
        }

        // Action to import tasks from an Excel file
        [HttpPost]
        public IActionResult ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please upload a valid Excel file."; // Show error if file is invalid
                return View("Error");
            }

            try
            {
                using (var stream = new MemoryStream()) // Create a memory stream for the file data
                {
                    file.CopyTo(stream); // Copy the uploaded file data into the memory stream
                    using (var workbook = new XLWorkbook(stream)) // Open the Excel file from the stream
                    {
                        var worksheet = workbook.Worksheet(1); // Get the first worksheet from the workbook
                        if (worksheet == null)
                        {
                            ViewBag.Error = "No worksheet found in the uploaded file."; // Show error if no worksheet found
                            return View("Error");
                        }

                        var rows = worksheet.RowsUsed().Skip(1); // Skip the header row and get the rest of the rows
                        if (!rows.Any())
                        {
                            ViewBag.Error = "The worksheet contains no data."; // Show error if there is no data
                            return View("Error");
                        }

                        // Iterate over each row and import task data
                        foreach (var row in rows)
                        {
                            if (string.IsNullOrEmpty(row.Cell(1).GetValue<string>()))
                                continue; // Skip rows without valid IDs

                            var task = new TaskModel
                            {
                                Id = int.TryParse(row.Cell(1).GetValue<string>(), out var id) ? id : 0,
                                Title = row.Cell(2).GetValue<string>(),
                                AssignedTo = row.Cell(3).GetValue<string>(),
                                DateStarted = DateTime.TryParse(row.Cell(4).GetValue<string>(), out var dateStarted) ? dateStarted : (DateTime?)null,
                                DateOfCompletion = DateTime.TryParse(row.Cell(5).GetValue<string>(), out var dateOfCompletion) ? dateOfCompletion : (DateTime?)null,
                                Status = row.Cell(6).GetValue<string>(),
                                Details = row.Cell(7).GetValue<string>()
                            };

                            // Avoid adding duplicate tasks
                            if (task.Id > 0 && !tasks.Any(t => t.Id == task.Id))
                            {
                                tasks.Add(task); // Add valid task to the list
                            }
                        }
                    }
                }

                return RedirectToAction("Index"); // Redirect back to the Index page after import
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // Log the exception for debugging purposes
                ViewBag.Error = $"Error importing file: {ex.Message}. Please check the file format and data."; // Show error message
                return View("Error");
            }
        }
    }
}
