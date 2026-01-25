namespace MyPo.Portfolio.Shared.Models;

public partial interface IPortfolioRepository
{
	/// <summary>
	/// Gets ROI summary for a portfolio.
	/// </summary>
	public ValueTask<PnlSummary> GetRoiSummaryForPortfolioAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets ROI records for a portfolio.
	/// </summary>
	public ValueTask<IEnumerable<RoiRec>> GetRoiRecsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new ROI record.
	/// </summary>
	public ValueTask<RoiRec?> CreateRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an ROI record by its ID.
	/// </summary>
	public ValueTask<RoiRec?> GetRoiRecByIdAsync(string roiRecId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing ROI record.
	/// </summary>
	public ValueTask<RoiRec?> UpdateRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an existing ROI record.
	/// </summary>
	public ValueTask<bool> DeleteRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default);
}
