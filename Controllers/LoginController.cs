using Quiz.Models; // Add appropriate using directive for SubmissionConfirmationViewModel
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web.Mvc;
using System.Web.Security;

namespace Quiz.Controllers
{
    public class LoginController : Controller
    {
        private QuizDBEntities _context = new QuizDBEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string username)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    Session["Username"] = user.Username;
                    if (user.RoleID == 1) // Admin Role
                    {
                        ViewBag.Role = "Admin";
                        ViewBag.AdminName = user.Username;
                        return RedirectToAction("AdminDashboard");
                    }
                    else if (user.RoleID == 2) // User Role
                    {
                        ViewBag.Role = "User";
                        ViewBag.Username = user.Username; // Set the username in the ViewBag
                        return View("UserDashboard", _context.Questions.ToList());
                    }
                }
                // If user not found, show pop-up message
                ViewBag.ErrorMessage = "User not found.";
                return View();
            }
            return View(username);
        }

        // Logout action
        public ActionResult Logout()
        {

            Session.Clear();
            return RedirectToAction("Index", "Login");
            }


        public ActionResult AdminDashboard()
        {
            var viewModel = new AdminDashboardInsightsViewModel();

            // Retrieve data and calculate statistics
            viewModel.TotalQuestions = _context.Questions.Count();
            viewModel.TotalUsers = _context.Users.Count();
            viewModel.TotalResults = _context.Results.Count();
            viewModel.AverageScore = (double)_context.Results.Average(r => r.Score);

            // Calculate pass and fail ratios (assuming pass score is 70%)
            int totalPassed = _context.Results.Count(r => r.Score >= 70);
            int totalFailed = _context.Results.Count(r => r.Score < 70);
            int totalStudents = viewModel.TotalUsers; // Total number of students
            viewModel.PassRatio = (double)totalPassed / totalStudents * 100;
            viewModel.FailRatio = (double)totalFailed / totalStudents * 100;

            // Retrieve date-wise stats from the database
            viewModel.DateWiseStats = _context.Results
                .GroupBy(r => r.TestDate)
                .Select(g => new DateWiseStatsViewModel
                {
                    Date = (DateTime)g.Key,
                    QuizCount = g.Count()
                })
                .ToList();
            viewModel.MonthWiseStats = _context.Results
                .Where(r => r.TestDate != null) // Filter out null TestDate values
                .GroupBy(r => r.TestDate.Value.Month) // Access Month property directly after ensuring TestDate is not null
                .Select(g => new MonthWiseStatsViewModel
                {
                    Month = g.Key,
                    QuizCount = g.Count()
                })
                .ToList();



            return View(viewModel);
        }


        public ActionResult UserDashboard()
        {
            if (Session["Username"] == null)
            {
                // If not, redirect to the login page
                return RedirectToAction("Index", "Login");
            }
            try
            {
                // Check if the form has been submitted
                if (Session["FormSubmitted"] != null && (bool)Session["FormSubmitted"])
                {
                    // Clear session flag
                    Session["FormSubmitted"] = false;
                    // If form has been submitted, clear the form elements
                    ViewBag.FormSubmitted = true;
                }
                else
                {
                    ViewBag.FormSubmitted = false;
                }

                var questions = FetchQuestionsFromDatabase();
                return View(questions);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Failed to fetch questions. Please try again later.";
                return View(new List<Question>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitAnswers(string username, FormCollection form)
        {
            try
            {
                var questions = FetchQuestionsFromDatabase();
                int totalQuestions = questions.Count;
                int correctAnswersCount = 0;
                List<Tuple<string, string, bool>> answersWithResult = new List<Tuple<string, string, bool>>();

                // Loop through each question
                foreach (var question in questions)
                {
                    string questionIdString = "answer[" + question.QuestionID + "]";

                    // Check if the question type is Multiple Choice
                    if (question.QuestionType == "Multiple Choice")
                    {
                        string selectedAnswer = form[questionIdString]; // Retrieve the selected answer using the question ID

                        // Check if the selected answer is not null or empty
                        if (!string.IsNullOrEmpty(selectedAnswer))
                        {
                            // Split the selected answer string by '|' to separate the selected answer and the question ID
                            string[] answerParts = selectedAnswer.Split('|');

                            // Ensure that answerParts has at least two elements before extracting values
                            if (answerParts.Length >= 2)
                            {
                                string actualSelectedAnswer = answerParts[0]; // Extract the selected answer
                                string questionId = answerParts[1]; // Extract the question ID

                                // Retrieve the question from the database
                                var currentQuestion = questions.FirstOrDefault(q => q.QuestionID.ToString() == questionId);

                                // Check if the retrieved question exists and the answer is correct
                                if (currentQuestion != null && actualSelectedAnswer == currentQuestion.CorrectAnswer)
                                {
                                    correctAnswersCount++; // Increment the correct answers count
                                    answersWithResult.Add(new Tuple<string, string, bool>(actualSelectedAnswer, currentQuestion.CorrectAnswer, true));
                                }
                                else
                                {
                                    answersWithResult.Add(new Tuple<string, string, bool>(actualSelectedAnswer, currentQuestion.CorrectAnswer, false));
                                }
                            }
                        }
                    }
                    // Check if the question type is Write-in
                    else if (question.QuestionType == "Write-in")
                    {
                        string writtenAnswer = form["answer"]; // Retrieve the written answer using the name attribute
                                                               // Retrieve the correct answer for the write-in question
                        string correctAnswer = question.CorrectAnswer;

                        // Check if the written answer matches the correct answer
                        if (writtenAnswer == correctAnswer)
                        {
                            correctAnswersCount++; // Increment the correct answers count
                            answersWithResult.Add(new Tuple<string, string, bool>(writtenAnswer, correctAnswer, true));
                        }
                        else
                        {
                            answersWithResult.Add(new Tuple<string, string, bool>(writtenAnswer, correctAnswer, false));
                        }
                    }
                   else if (question.QuestionType == "Image")
                    {
                        string selectedAnswer = form[questionIdString]; // Retrieve the selected answer using the question ID

                        // Check if the selected answer is not null or empty
                        if (!string.IsNullOrEmpty(selectedAnswer))
                        {
                            // Split the selected answer string by '|' to separate the selected answer and the question ID
                            string[] answerParts = selectedAnswer.Split('|');

                            // Ensure that answerParts has at least two elements before extracting values
                            if (answerParts.Length >= 2)
                            {
                                string actualSelectedAnswer = answerParts[0]; // Extract the selected answer
                                string questionId = answerParts[1]; // Extract the question ID

                                // Retrieve the question from the database
                                var currentQuestion = questions.FirstOrDefault(q => q.QuestionID.ToString() == questionId);

                                // Check if the retrieved question exists and the answer is correct
                                if (currentQuestion != null && actualSelectedAnswer == currentQuestion.CorrectAnswer)
                                {
                                    correctAnswersCount++; // Increment the correct answers count
                                    answersWithResult.Add(new Tuple<string, string, bool>(actualSelectedAnswer, currentQuestion.CorrectAnswer, true));
                                }
                                else
                                {
                                    answersWithResult.Add(new Tuple<string, string, bool>(actualSelectedAnswer, currentQuestion.CorrectAnswer, false));
                                }
                            }
                        }
                    }
                }

                // Calculate the score based on the number of correct answers
                double score = (double)correctAnswersCount;
                var currentDate = DateTime.Now.Date;
                var currentTime = DateTime.Now.TimeOfDay;
                // Store the score and username in the Result table
                using (var db = new QuizDBEntities())
                {

                    var result = new Result
                    {
                        Username = username,
                        Score = (int?)score,
                        SessionID = 1,
                        TestDate = currentDate, // Store the current date
                        TestTime = currentTime  // Store the current time
                    };

                    db.Results.Add(result);
                    db.SaveChanges();
                }

                // Set a session variable to indicate that the form has been submitted
                Session["FormSubmitted"] = true;

                // Redirect to the SubmissionConfirmation action
                return RedirectToAction("SubmissionConfirmation", new { score = score, username = username, testDate = currentDate.ToString("yyyy-MM-dd"), testTime = currentTime.ToString("hh\\:mm\\:ss") });

            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while processing your answers. Please try again later.";
                return View("Error");
            }
        }






        public ActionResult SubmissionConfirmation(string username, double score, string testDate, string testTime)
        {
            // Convert the date and time strings back to DateTime objects
            DateTime testDateTime = DateTime.ParseExact(testDate + " " + testTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            var model = new SubmissionConfirmationViewModel
            {
                Username = username,
                Score = score,
                TestDate = testDateTime.Date,
                TestTime = testDateTime.TimeOfDay
            };

            SendSubmissionConfirmationEmail(model);

            // Return the view with the model
            return View(model);
        }

        private void SendSubmissionConfirmationEmail(SubmissionConfirmationViewModel model)
        {
            try
            {
                // Create an instance of SmtpClient
                SmtpClient smtpClient = new SmtpClient();

                // Create a MailMessage object
                MailMessage mailMessage = new MailMessage();

                // Set the sender's email address
                mailMessage.From = new MailAddress("sksahotra911@outlook.com");

                // Set the recipient's email address
                mailMessage.To.Add("sksahotra911@gmail.com");

                // Set the subject of the email
                mailMessage.Subject = "Submission Confirmation";

                // Construct the email body in table format
                string emailBody = $@"
            <!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Submission Confirmation</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f0f0f0;
            margin: 0;
            padding: 0;
        }}

        .container {{
            max-width: 600px;
            margin: 20px auto;
            padding: 20px;
            background-color: #fff;
            border-radius: 8px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }}

        h2 {{
            color: #333;
        }}

        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}

        th, td {{
            padding: 10px;
            border-bottom: 1px solid #ddd;
            text-align: left;
        }}

        th {{
            background-color: #f2f2f2;
            font-weight: bold;
        }}

        .button {{
            display: inline-block;
            background-color: #007bff;
            color: #fff;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 5px;
            transition: background-color 0.3s ease;
        }}

        .button:hover {{
            background-color: #0056b3;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Submission Confirmation</h2>
        <p>Thank you, <span id=""username"">{model.Username}</span>, for completing the quiz!</p>
        <table>
            <tr>
                <th>Test Score</th>
                <td id=""score"">{model.Score}</td>
            </tr>
            <tr>
                <th>Test Date</th>
                <td id=""testDate"">{model.TestDate.ToShortDateString()}</td>
            </tr>
            <tr>
                <th>Test Time</th>
                <td>{model.TestTime.ToString("hh\\:mm\\:ss")}</td>
            </tr>
        </table>
        
    </div>
</body>
</html>
        ";

                // Set the body of the email
                mailMessage.Body = emailBody;
                mailMessage.IsBodyHtml = true; // Specify that the body contains HTML

                ViewBag.Message = "Email sent successfully.";
                // Send the email
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during email sending
                ViewBag.Error = "An error occurred while sending the email: " + ex.Message;
            }
        }


        private List<Question> FetchQuestionsFromDatabase()
        {
            // Query the Question table to fetch all questions
            var questions = _context.Questions.ToList();

            // Populate image data properties for each question
            foreach (var question in questions)
            {
                // Assign image data from the question directly to the respective properties
                question.OptionAImageData = question.OptionAImageData;
                question.OptionBImageData = question.OptionBImageData;
                question.OptionCImageData = question.OptionCImageData;
                question.OptionDImageData = question.OptionDImageData;
            }

            return questions;
        }
    }
    }
