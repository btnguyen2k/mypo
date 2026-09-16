using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.PubSub;

/// <summary>
/// Indicates that a portfolio has been deleted.
/// </summary>
public sealed record PortfolioDeletedEvent(PortfolioEntity Portfolio) : PubSubMessage;
