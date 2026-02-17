namespace ShinyKmlRecorder.Services;

public interface IExportSampleService
{
    Guid? SelectedWorkId { get; set; }
    ExportFormat ExportFormat { get; set; }
    ExportDelivery ExportDelivery { get; set; }

    string? AzureAccountUrl { get; set; }
    string? AzureContainerName { get; set; }
    string? AzureSasToken { get; set; }

    string? EmailTo { get; set; }
    string? EmailSubject { get; set; }
    string? EmailBody { get; set; }

    void ResetFilters();
}
