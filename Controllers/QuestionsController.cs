using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Quiz.Models;

namespace Quiz.Controllers
{
    public class QuestionsController : Controller
    {
        private QuizDBEntities db = new QuizDBEntities();

        public ActionResult Index()
        {
            var questions = db.Questions.Include(q => q.Category);
            return View(questions.ToList());
        }

        // GET: Questions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Question question = db.Questions.Find(id);
            if (question == null)
            {
                return HttpNotFound();
            }
            return View(question);
        }

        // GET: Questions/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Question question, HttpPostedFileBase optionAImage, HttpPostedFileBase optionBImage, HttpPostedFileBase optionCImage, HttpPostedFileBase optionDImage)
        {
            if (ModelState.IsValid)
            {
                bool imagesUploadedSuccessfully = await ProcessImageAsync(question, optionAImage, optionBImage, optionCImage, optionDImage);

                if (!imagesUploadedSuccessfully)
                {
                    ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
                    ModelState.AddModelError("", "One or more images failed to upload.");
                    return View(question);
                }

                if (question.QuestionType == "Write-in")
                {
                    // If the question type is write-in, set the correct answer directly from the form field
                    question.CorrectAnswer = Request.Form["CorrectAnswer"];
                }

                try
                {
                    db.Questions.Add(question);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    // Log validation errors
                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                        {
                            Console.WriteLine($"Validation Error: {validationError.PropertyName} - {validationError.ErrorMessage}");
                        }
                    }

                    // Add a generic error message
                    ModelState.AddModelError("", "An error occurred while saving the data.");
                    ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
                    return View(question);
                }
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
            return View(question);
        }


        private async Task<bool> ProcessImageAsync(Question question, HttpPostedFileBase optionAImage, HttpPostedFileBase optionBImage, HttpPostedFileBase optionCImage, HttpPostedFileBase optionDImage)
        {
            try
            {
                // Process Option A Image
                if (optionAImage != null && optionAImage.ContentLength > 0)
                {
                    using (var binaryReader = new BinaryReader(optionAImage.InputStream))
                    {
                        question.OptionAImageData = binaryReader.ReadBytes(optionAImage.ContentLength);
                    }
                }

                // Process Option B Image
                if (optionBImage != null && optionBImage.ContentLength > 0)
                {
                    using (var binaryReader = new BinaryReader(optionBImage.InputStream))
                    {
                        question.OptionBImageData = binaryReader.ReadBytes(optionBImage.ContentLength);
                    }
                }

                // Process Option C Image
                if (optionCImage != null && optionCImage.ContentLength > 0)
                {
                    using (var binaryReader = new BinaryReader(optionCImage.InputStream))
                    {
                        question.OptionCImageData = binaryReader.ReadBytes(optionCImage.ContentLength);
                    }
                }

                // Process Option D Image
                if (optionDImage != null && optionDImage.ContentLength > 0)
                {
                    using (var binaryReader = new BinaryReader(optionDImage.InputStream))
                    {
                        question.OptionDImageData = binaryReader.ReadBytes(optionDImage.ContentLength);
                    }
                }

                return true; // Image(s) uploaded successfully
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during image processing
                Console.WriteLine($"Error processing image: {ex.Message}");
                return false; // Image upload failed
            }
        }



        // GET: Questions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Question question = db.Questions.Find(id);
            if (question == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
            return View(question);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Question question, HttpPostedFileBase optionAImage, HttpPostedFileBase optionBImage, HttpPostedFileBase optionCImage, HttpPostedFileBase optionDImage)
        {
            if (ModelState.IsValid)
            {
                bool imagesUploadedSuccessfully = await ProcessImageAsync(question, optionAImage, optionBImage, optionCImage, optionDImage);

                if (!imagesUploadedSuccessfully)
                {
                    ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
                    ModelState.AddModelError("", "One or more images failed to upload.");
                    return View(question);
                }

                if (question.QuestionType == "Write-in")
                {
                    // If the question type is write-in, set the correct answer directly from the form field
                    question.CorrectAnswer = Request.Form["CorrectAnswer"];
                }

                try
                {
                    db.Entry(question).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    // Log validation errors
                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                        {
                            Console.WriteLine($"Validation Error: {validationError.PropertyName} - {validationError.ErrorMessage}");
                        }
                    }

                    // Add a generic error message
                    ModelState.AddModelError("", "An error occurred while saving the data.");
                    ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
                    return View(question);
                }
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", question.CategoryID);
            return View(question);
        }

        // GET: Questions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Question question = db.Questions.Find(id);
            if (question == null)
            {
                return HttpNotFound();
            }
            return View(question);
        }
        private byte[] ConvertToByteArray(HttpPostedFileBase file)
        {
            byte[] byteArray = null;
            if (file != null)
            {
                using (var inputStream = file.InputStream)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        inputStream.CopyTo(memoryStream);
                        byteArray = memoryStream.ToArray();
                    }
                }
            }
            return byteArray;
        }


        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Question question = db.Questions.Find(id);
            db.Questions.Remove(question);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}