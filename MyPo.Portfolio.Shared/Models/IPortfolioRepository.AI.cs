namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Saves a new symbol analysis report in the repository.
	/// </summary>
	public ValueTask<SymbolAnalysisEntity?> CreateSymbolAnalysisAsync(SymbolAnalysisEntity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an existing symbol analysis for a specific item and AI configuration.
	/// </summary>
	public ValueTask<SymbolAnalysisEntity?> GetSymbolAnalysisAsync(string ownerId, string marketId, string itemType, string itemCode, string analysisType, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing symbol analysis report in the repository.
	/// </summary>
	public ValueTask<SymbolAnalysisEntity?> UpdateSymbolAnalysisAsync(SymbolAnalysisEntity entity, CancellationToken cancellationToken = default);
}
