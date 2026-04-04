namespace ExamSystem.Web.Models
{
    public class ImportResultViewModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public List<ImportError> Errors { get; set; } = new();
        public List<ImportRowPreview> RowPreviews { get; set; } = new(); // Thêm dòng này
        public string DetectedType { get; set; }
    }

    public class ImportRowPreview
    {
        public int RowIndex { get; set; }
        public string Content { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsParent { get; set; }
        public int ParentIndex { get; set; } = -1;
    }

    public class ImportError
    {
        public int Row { get; set; }
        public string ErrorMessage { get; set; }
    }
}
