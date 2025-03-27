namespace LeaveTracker.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadMedicalReport(IFormFile file);

    }
}
