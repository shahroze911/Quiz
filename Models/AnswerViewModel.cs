using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quiz.Models
{
    public class AnswerViewModel
    {
        public int QuestionID { get; set; }
        public string SelectedAnswer { get; set; }
    }
}