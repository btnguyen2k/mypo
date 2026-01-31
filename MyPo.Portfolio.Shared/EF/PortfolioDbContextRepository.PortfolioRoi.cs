using MyPo.Portfolio.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MyPo.Portfolio.Shared.EF;

public sealed partial class PortfolioDbContextRepository
{
	private DbSet<RoiRec> RoiRecStore { get; set; }

	/// <inheritdoc />
	public async ValueTask<RoiRec?> CreateRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default)
	{
		var entry = await RoiRecStore.AddAsync(roiRec, cancellationToken);
		return await SaveChangesAsync(cancellationToken) > 0 ? entry.Entity : null;
	}

	/// <inheritdoc />
	public async ValueTask<RoiRec?> GetRoiRecByIdAsync(string roiRecId, CancellationToken cancellationToken = default)
	{
		return await RoiRecStore.AsNoTracking().FirstOrDefaultAsync(rr => rr.Id == roiRecId, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<RoiRec?> UpdateRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default)
	{
		var existingEntry = await RoiRecStore.FindAsync([roiRec.Id], cancellationToken);
		if (existingEntry == null)
		{
			return null;
		}
		Entry(existingEntry).CurrentValues.SetValues(PrepareForUpdate(roiRec));
		return await SaveChangesAsync(cancellationToken) > 0 ? existingEntry : null;
	}

	/// <inheritdoc />
	public async ValueTask<bool> DeleteRoiRecAsync(RoiRec roiRec, CancellationToken cancellationToken = default)
	{
		RoiRecStore.Remove(roiRec);
		return await SaveChangesAsync(cancellationToken) > 0;
	}

	/// <inheritdoc />
	public async ValueTask<PnlSummary> GetRoiSummaryForPortfolioAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		var roiSummary = new PnlSummary()
		{
			PortfolioId = portfolioId,
			TotalBuyValue = 0.0m,
			TotalSellValue = 0.0m,
			TotalDividends = 0.0m,
			TotalTax = 0.0m,
			TotalFees = 0.0m,
			TotalCashIn = 0.0m,
			TotalCashOut = 0.0m,
			TotalInterest = 0.0m,
		};
		var rows = await RoiRecStore.AsNoTracking()
			.Where(rr => rr.PortfolioId == portfolioId)
			.Where(rr => rr.Status != RoiRec.STATUS_ARCHIVED)
			.GroupBy(rr => rr.TxType)
			.Select(g => new
			{
				TxType = g.Key,
				TotalValue = g.Sum(rr => rr.TxValue)
			})
			.ToListAsync(cancellationToken);
		foreach (var row in rows)
		{
			switch (row.TxType)
			{
				case RoiRec.TX_TYPE_BUY:
					roiSummary.TotalBuyValue = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_SELL:
					roiSummary.TotalSellValue = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_DIVIDEND:
					roiSummary.TotalDividends = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_TAX:
					roiSummary.TotalTax = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_INTEREST:
					roiSummary.TotalInterest = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_FEE:
					roiSummary.TotalFees = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_CASHIN:
					roiSummary.TotalCashIn = row.TotalValue;
					break;
				case RoiRec.TX_TYPE_CASHOUT:
					roiSummary.TotalCashOut = row.TotalValue;
					break;
			}
		}

		return roiSummary;
	}

	/// <inheritdoc />
	public async ValueTask<IEnumerable<RoiRec>> GetRoiRecsByPortfolioIdAsync(string portfolioId, CancellationToken cancellationToken = default)
	{
		return await RoiRecStore.AsNoTracking()
			.Where(rr => rr.PortfolioId == portfolioId).Where(rr => rr.Status != RoiRec.STATUS_ARCHIVED)
			.OrderByDescending(rr => rr.TxTime)
			.ToListAsync(cancellationToken);
	}
}
