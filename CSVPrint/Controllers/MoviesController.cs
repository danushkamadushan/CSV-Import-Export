using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CSVPrint.Data;
using CSVPrint.Models;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Data;
using Microsoft.AspNetCore.Hosting;

namespace CSVPrint.Controllers
{
    public class MoviesController : Controller
    {
        private readonly CSVPrintContext _context;
        private IHostingEnvironment Environment;

        public MoviesController(CSVPrintContext context, IHostingEnvironment _environment)
        {
            _context = context;
            Environment = _environment;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var movies = from m in _context.Movies
                         select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title.Contains(searchString));
            }

            ViewBag.Search = searchString;
            return View(await movies.ToListAsync());
        }

        public async Task<FileResult> GetCSV(string searchString)
        {
            var model = await _context.Movies.ToListAsync();

            string csv = string.Empty;
            if (model != null)
            {
                var columns = new Dictionary<string, string> { };

                columns = new Dictionary<string, string>
                {
                    { "Title", "Title" },
                    { "ReleaseDate", "Release Date" },
                    { "Genre", "Genre" },
                    { "Price", "Price" }
                };

                csv = CreateCSV(model, searchString, columns);
            }

            MemoryStream memoryStream = new MemoryStream();
            TextWriter tw = new StreamWriter(memoryStream);

            tw.WriteLine(csv);
            tw.Flush();
            tw.Close();

            return File(memoryStream.GetBuffer(), "text/csv", "MovieDetail_" + DateTime.Now.ToShortDateString() + ".csv");
        }


        private string CreateCSV<T>(
            IEnumerable<T> enumerable,
            string searchString,
            Dictionary<string, string> columns)
        {
            var encoding = new ASCIIEncoding();
            var csv = enumerable.ToCsv(searchString, columns);

            string stringToCompare = csv.ToString().Trim(new Char[] { ' ', '*', '.', ',', '"', '"', ';', '/', '\\', '(', ')' });

            // Check for Unauthorized contents
            string pattern = @"<!*[^<>]*>"; // identify html
            var isScriptsTags = Regex.Match(stringToCompare, pattern);
            if (isScriptsTags.Success)
            {
                var csvDataInvalid = "Unauthorized contents are not allowed to generate";
                return csvDataInvalid;
            }

            return csv;    
        }

        [HttpPost]
        public async Task<IActionResult> UploadCSV(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                //Get file and save it to project location
                string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                //Read saved file from saved location
                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                //Read data from saved file
                string csvData = System.IO.File.ReadAllText(filePath);

                bool firstRow = true; //if your csv have Header row this should be "true" / if your dont have Header row this should be "false"

                //Split data into rows
                foreach (string row in csvData.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        //Read header row
                        if (firstRow)
                        {
                            firstRow = false;
                        }
                        else // Read data rows
                        {
                            string pattern = @"<!*[^<>]*>"; // identify html
                            var r = row.Split(',');

                            //if data contain html tags, clear the whole record from inserting
                            for (int i = 0; i < 4; i++)
                            {
                                var isScriptsTags = Regex.Match(r[i], pattern);
                                if (isScriptsTags.Success)
                                {
                                    r[i] = null;
                                }
                            }

                            var movieDetail = new Movies()
                            {
                                Title = r[0] == null ? "" : r[0].Trim(),
                                ReleaseDate = r[1] == null ? DateTime.Now : Convert.ToDateTime(r[1].Trim()),
                                Genre = r[2] == null ? "" : r[2].Trim(),
                                Price = Convert.ToDecimal(r[3].Trim())
                            };
                            _context.Add(movieDetail);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        public FileResult DownloadCSVTemplate()
        {
            string path = Path.Combine(this.Environment.WebRootPath, "Uploads/TempCSV.csv");
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            return File(bytes, "application/octet-stream", "TempCSV.csv");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movies = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movies == null)
            {
                return NotFound();
            }

            return View(movies);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price")] Movies movies)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movies);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movies);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movies = await _context.Movies.FindAsync(id);
            if (movies == null)
            {
                return NotFound();
            }
            return View(movies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price")] Movies movies)
        {
            if (id != movies.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movies);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MoviesExists(movies.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movies);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movies = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movies == null)
            {
                return NotFound();
            }

            return View(movies);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movies = await _context.Movies.FindAsync(id);
            _context.Movies.Remove(movies);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MoviesExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}
