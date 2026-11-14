namespace DataflowImport.Models;

public record ImportReportModel(int Imported, decimal ImportedTotal, int BatchesSaved, IReadOnlyList<string> Rejected, int MostParsersAtOnce);
