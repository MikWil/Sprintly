namespace Sprintly.Models;

public class AppSettings
{
    public double DefaultHoursPerDay { get; set; } = 8;
    public double DefaultCapacityFactor { get; set; } = 1.0;
    public List<string> DefaultRoles { get; set; } =
    [
        "Developer",
        "QA Engineer",
        "Designer",
        "Tech Lead",
        "Scrum Master",
        "DevOps"
    ];
    public string Locale { get; set; } = "en-US";
}
