using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quiz.Models
{
    public class SubmissionConfirmationViewModel
    {
        public string Username { get; set; }
        public double? Score { get; set; }        
        public DateTime TestDate { get; internal set; }
        public TimeSpan TestTime { get; internal set; }
    }
}