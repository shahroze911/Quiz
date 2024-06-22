using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quiz.Models
{
    public class AdminDashboardInsightsViewModel
    {
        public int TotalQuestions { get; set; }
        public int TotalUsers { get; set; }
        public int TotalResults { get; set; }
        public double AverageScore { get; set; }
        public double PassRatio { get; set; }
        public double FailRatio { get; set; }
        public List<DateWiseStatsViewModel> DateWiseStats { get; set; }
        public List<MonthWiseStatsViewModel> MonthWiseStats { get; set; }
    }

    public class DateWiseStatsViewModel
    {
        public DateTime Date { get; set; }
        public int QuizCount { get; set; }
    }

    public class MonthWiseStatsViewModel
    {
        public int Month { get; set; }
        public int QuizCount { get; set; }
    }

}