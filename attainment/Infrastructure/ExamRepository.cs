using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Infrastructure;

using System.Text.Json; 
public class ExamRepository(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public static Exam Parse(string json)
    {
        var exam = JsonSerializer.Deserialize<Exam>(json);
        return exam;
    }
}