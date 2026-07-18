namespace ExamBuilderGR.Models;

public sealed class SchoolProfile
{
    public string SchoolName { get; set; } = "«Πυθαγόρειο» Γενικό Λύκειο Σάμου";
    public string SchoolSubtitle { get; set; } = "Ημερήσιο Γενικό Λύκειο";
    public string Address { get; set; } = "Πυθαγόρα 831 00, Σάμος";
    public string Phone { get; set; } = "22730 12345";
    public string Email { get; set; } = "mail@lyk-pythagoreio.sam.sch.gr";
    public string TeacherName { get; set; } = "Μάριος Γιακαλάρας";
    public string TeacherSpecialty { get; set; } = "ΠΕ86 Πληροφορικής";
    public string SchoolYear { get; set; } = "2025 - 2026";
    public string ThemeName { get; set; } = "Classic Light";
    public string SchoolLogoPath { get; set; } = string.Empty;
    public bool ShowSchoolLogo { get; set; } = true;
    public string SchoolLogoPosition { get; set; } = "Αριστερά";
    public double SchoolLogoWidthCm { get; set; } = 2.8d;
    public bool SchoolLogoGrayscale { get; set; }

    public SchoolProfile Clone() => new()
    {
        SchoolName = SchoolName,
        SchoolSubtitle = SchoolSubtitle,
        Address = Address,
        Phone = Phone,
        Email = Email,
        TeacherName = TeacherName,
        TeacherSpecialty = TeacherSpecialty,
        SchoolYear = SchoolYear,
        ThemeName = ThemeName,
        SchoolLogoPath = SchoolLogoPath,
        ShowSchoolLogo = ShowSchoolLogo,
        SchoolLogoPosition = SchoolLogoPosition,
        SchoolLogoWidthCm = SchoolLogoWidthCm,
        SchoolLogoGrayscale = SchoolLogoGrayscale
    };
}
