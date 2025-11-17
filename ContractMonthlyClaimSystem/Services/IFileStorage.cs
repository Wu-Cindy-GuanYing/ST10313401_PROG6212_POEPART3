namespace ContractMonthlyClaimSystem.Services
{
    public interface IFileStorage
    {
        Task<(string storedPath, long size, string contentType)> SaveAsync(IFormFile file, string subFolder, CancellationToken ct);
    }


    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] Allowed = new[]{

"application/pdf",
"application/vnd.openxmlformats-officedocument.wordprocessingml.document",
"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
};
        private const long MaxSize = 10 * 1024 * 1024; // 10 MB
        public LocalFileStorage(IWebHostEnvironment env) => _env = env;
        public async Task<(string, long, string)> SaveAsync(IFormFile file, string subFolder, CancellationToken ct)
        {
            if (file == null || file.Length == 0) throw new InvalidOperationException("No file uploaded.");
            if (file.Length > MaxSize) throw new InvalidOperationException("Max file size is 10MB.");
            if (!Allowed.Contains(file.ContentType)) throw new InvalidOperationException("Only pdf, docx, xlsx allowed.");


            var root = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(root);
            var safeBase = Path.GetFileNameWithoutExtension(file.FileName);
            var ext = Path.GetExtension(file.FileName);
            var name = $"{safeBase}_{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(root, name);
            await using var s = File.Create(full);
            await file.CopyToAsync(s, ct);
            var relative = Path.Combine("/uploads", subFolder, name).Replace("\\", " / ");
        return (relative, file.Length, file.ContentType);
        }
    }
}