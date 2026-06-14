using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
    /// <summary>
    /// Gets current user's portfolio Buy/Sell transactions.
    /// </summary>
    /// <param name="id">ID of the portfolio.</param>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_BUYS_SELLS)]
    public async ValueTask<ActionResult<ApiResp<IEnumerable<TxBuySellResp>>>> GetMyPortfolioTxBuySells([FromRoute] string id)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        // validate portfolio, must be current user's portfolio
        var existingPortfolio = await GetPortfolioIfAccessible(currentUser, id);
        if (existingPortfolio == null)
        {
            return ResponseNoData(404, "Portfolio not found or not accessible.");
        }

        var txList = await PortfolioRepository.GetTxBuySellListByPortfolioIdAsync(id);
        var result = new List<TxBuySellResp>();
        foreach (var tx in txList)
        {
            var market = Globals.MarketsMap.TryGetValue(tx.MarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
            result.Add(TxBuySellResp.BuildFrom(tx, market));
        }
        return ResponseOk(result);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static (CreateOrUpdateTxBuySellReq?, ObjectResult?) ValidateTxBuySell(CreateOrUpdateTxBuySellReq reqTx, TxBuySellEntity? existingTx)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // validate transaction type, must be either BUY or SELL
        reqTx.Type = reqTx.Type.ToUpper().Trim();
        if (!(existingTx?.IsSettled ?? false) && reqTx.Type != TxBuySellEntity.TX_TYPE_BUY && reqTx.Type != TxBuySellEntity.TX_TYPE_SELL)
        {
            return (null, ResponseNoData(400, $"Transaction type must be either '{TxBuySellEntity.TX_TYPE_BUY}' or '{TxBuySellEntity.TX_TYPE_SELL}', currently '{reqTx.Type}'."));
        }

        // validate item code, must not be empty
        reqTx.ItemCode = reqTx.ItemCode.ToUpper().Trim();
        if (!(existingTx?.IsSettled ?? false) && string.IsNullOrEmpty(reqTx.ItemCode))
        {
            return (null, ResponseNoData(400, "Item code must not be empty."));
        }

        // validate price, must be positive
        if (!(existingTx?.IsSettled ?? false) && reqTx.Price <= 0.00m)
        {
            return (null, ResponseNoData(400, "Price must be a positive value."));
        }

        // validate quantity, must be positive
        if (!(existingTx?.IsSettled ?? false) && reqTx.Quantity <= 0)
        {
            return (null, ResponseNoData(400, "Quantity must be a positive value."));
        }

        // validate market, must be a valid one or empty
        reqTx.MarketId = reqTx.MarketId?.Trim() ?? null;
        if (!(existingTx?.IsSettled ?? false) && !string.IsNullOrEmpty(reqTx.MarketId))
        {
            var market = Globals.MarketsMap.TryGetValue(reqTx.MarketId.ToUpper(), out var mkt) ? mkt : null;
            if (market == null)
            {
                return (null, ResponseNoData(400, $"Market '{reqTx.MarketId}' is not recognized."));
            }
            // shift the transaction's timezone to market's timezone
            reqTx.Time = new DateTimeOffset(reqTx.Time.DateTime, market.TZ.GetUtcOffset(reqTx.Time));
        }
        reqTx.Time = reqTx.Time.ToUniversalTime(); // convert to UTC for storing into database
        return (reqTx, null);
    }

    /// <summary>
    /// Creates a new Buy/Sell transaction record and Adds it to current user's portfolio.
    /// </summary>
    /// <param name="id">ID of the portfolio to add transaction record to.</param>
    /// <param name="req">Details of the transaction record to add.</param>
    /// <returns></returns>
    [HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_BUYS_SELLS)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<TxBuySellResp>>> AddTxBuySellToMyPortfolio([FromRoute] string id, [FromBody] CreateOrUpdateTxBuySellReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var (reqTx, validationResult) = ValidateTxBuySell(req, null);
        if (validationResult != null)
        {
            return validationResult;
        }

        // validate portfolio, must be current user's portfolio
        var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
        if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return ResponseNoData(400, "Portfolio not found or mismatched.");
        }

        var tx = new TxBuySellEntity
        {
            PortfolioId = reqTx!.Value.PortfolioId,
            Type = reqTx!.Value.Type,
            Time = reqTx!.Value.Time == default ? DateTimeOffset.UtcNow : reqTx!.Value.Time,
            Quantity = reqTx!.Value.Quantity,
            Price = reqTx!.Value.Price,
            Notes = reqTx!.Value.Notes?.Trim() ?? null,
            FeeTx = reqTx!.Value.FeeTx ?? 0.0m,
            FeeTax = reqTx!.Value.FeeTax ?? 0.0m,
            FeeOther = reqTx!.Value.FeeOther ?? 0.0m,
            ItemType = reqTx!.Value.ItemType,
            ItemCode = reqTx!.Value.ItemCode,
            MarketId = reqTx!.Value.MarketId,
            IsSettled = false,
        };
        var result = await PortfolioRepository.CreateTxBuySellAsync(tx);
        if (result == null)
        {
            return ResponseNoData(500, "Failed to create transaction record.");
        }
        return ResponseOk(TxBuySellResp.BuildFrom(tx));
    }

    /// <summary>
    /// Updates an existing Buy/Sell transaction record from current user's portfolio.
    /// </summary>
    /// <param name="id">ID of the portfolio.</param>
    /// <param name="txid">ID of the transaction record.</param>
    /// <param name="req">Details of the transaction record to update.</param>
    /// <returns></returns>
    [HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_BUY_SELL_TX_ID)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<TxBuySellResp>>> UpdateMyPortfolioTxBuySell([FromRoute] string id, [FromRoute] string txid, [FromBody] CreateOrUpdateTxBuySellReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var existingTx = await PortfolioRepository.GetTxBuySellAsync(txid);
        if (existingTx == null)
        {
            return ResponseNoData(404, "Transaction record not found.");
        }
        var (reqTx, validationResult) = ValidateTxBuySell(req, existingTx);
        if (validationResult != null)
        {
            return validationResult;
        }

        // validate portfolio, must be current user's portfolio
        var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
        if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return ResponseNoData(400, "Portfolio not found or mismatched.");
        }

        if (existingTx.IsSettled)
        {
            // for settled transactions, only notes can be updated
            existingTx.Notes = reqTx!.Value.Notes?.Trim() ?? null;
        }
        else
        {
            existingTx.Type = reqTx!.Value.Type;
            existingTx.Time = reqTx!.Value.Time == default ? DateTimeOffset.UtcNow : reqTx!.Value.Time;
            existingTx.Quantity = reqTx!.Value.Quantity;
            existingTx.Price = reqTx!.Value.Price;
            existingTx.Notes = reqTx!.Value.Notes?.Trim() ?? null;
            existingTx.FeeTx = reqTx!.Value.FeeTx ?? 0.0m;
            existingTx.FeeTax = reqTx!.Value.FeeTax ?? 0.0m;
            existingTx.FeeOther = reqTx!.Value.FeeOther ?? 0.0m;
            existingTx.ItemType = reqTx!.Value.ItemType;
            existingTx.ItemCode = reqTx!.Value.ItemCode;
            existingTx.MarketId = reqTx!.Value.MarketId;
        }
        existingTx = await PortfolioRepository.UpdateTxBuySellAsync(existingTx);
        if (existingTx == null)
        {
            return ResponseNoData(500, "Failed to update settled transaction record.");
        }
        return ResponseOk(TxBuySellResp.BuildFrom(existingTx));
    }

    /// <summary>
    /// Deletes an existing Buy/Sell transaction record from current user's portfolio.
    /// </summary>
    /// <param name="id">ID of the portfolio.</param>
    /// <param name="txid">ID of the transaction record.</param>
    /// <returns></returns>
    [HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_BUY_SELL_TX_ID)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<TxBuySellResp>>> DeleteMyPortfolioTxBuySell([FromRoute] string id, [FromRoute] string txid)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var existingTx = await PortfolioRepository.GetTxBuySellAsync(txid);
        if (existingTx == null)
        {
            return ResponseNoData(404, "Transaction record not found.");
        }

        // validate portfolio, must be current user's portfolio
        var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
        if (!(existingPortfolio?.Id.Equals(existingTx.PortfolioId, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return ResponseNoData(400, "Portfolio not found or mismatched.");
        }

        if (existingTx.IsSettled)
        {
            return ResponseNoData(400, "Cannot delete a settled transaction.");
        }
        else
        {
            var result = await PortfolioRepository.DeleteTxBuySellAsync(existingTx);
            if (!result)
            {
                return ResponseNoData(500, "Failed to delete transaction record.");
            }
            return ResponseOk(TxBuySellResp.BuildFrom(existingTx));
        }
    }

    /// <summary>
    /// Settles an existing Buy/Sell transaction record from current user's portfolio.
    /// </summary>
    /// <param name="id">ID of the portfolio.</param>
    /// <param name="txid">ID of the transaction record.</param>
    /// <param name="req">Details of the transaction record to settle.</param>
    /// <returns></returns>
    [HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_BUY_SELL_TX_ID)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<TxBuySellResp>>> SettleMyPortfolioTxBuySell([FromRoute] string id, [FromRoute] string txid, [FromBody] CreateOrUpdateTxBuySellReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var existingTx = await PortfolioRepository.GetTxBuySellAsync(txid);
        if (existingTx == null)
        {
            return ResponseNoData(404, "Transaction record not found.");
        }
        if (existingTx.IsSettled)
        {
            return ResponseNoData(400, "Transaction is already settled.");
        }
        var (reqTx, validationResult) = ValidateTxBuySell(req, existingTx);
        if (validationResult != null)
        {
            return validationResult;
        }

        // validate portfolio, must be current user's portfolio
        var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
        if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return ResponseNoData(400, "Portfolio not found or mismatched.");
        }

        existingTx.Type = reqTx!.Value.Type;
        existingTx.Time = reqTx!.Value.Time == default ? DateTimeOffset.UtcNow : reqTx!.Value.Time;
        existingTx.Quantity = reqTx!.Value.Quantity;
        existingTx.Price = reqTx!.Value.Price;
        existingTx.Notes = reqTx!.Value.Notes?.Trim() ?? null;
        existingTx.FeeTx = reqTx!.Value.FeeTx ?? 0.0m;
        existingTx.FeeTax = reqTx!.Value.FeeTax ?? 0.0m;
        existingTx.FeeOther = reqTx!.Value.FeeOther ?? 0.0m;
        existingTx.ItemType = reqTx!.Value.ItemType;
        existingTx.ItemCode = reqTx!.Value.ItemCode;
        existingTx.MarketId = reqTx!.Value.MarketId;

        var market = Globals.MarketsMap.TryGetValue(existingTx.MarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
        existingTx = await PortfolioRepository.SettleTxBuySellAsync(existingTx, market);
        if (existingTx == null)
        {
            return ResponseNoData(500, "Failed to settle transaction record.");
        }

        // clear portfolio's metadata refresh timestamp
        existingPortfolio.Metadata ??= new PortfolioMetadata();
        existingPortfolio.Metadata.MetadataRefreshTimestamp = 0;
        await PortfolioRepository.UpdatePortfolioAsync(existingPortfolio);

        return ResponseOk(TxBuySellResp.BuildFrom(existingTx));
    }
}
