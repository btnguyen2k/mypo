# My Portfolio Tracker release notes

## 2026-02-28 - v1.1.1

### Fixed/Improvements

- (Patch) Fix error when upserting market events to DB.

## 2026-02-28 - v1.1.0

### Added/Refactoring/Deprecation

- (Feat) Fetch incoming events from FinHub.
- (Feat) Reword dashboard page.

### Fixed/Improvements

- (Patch) Optimize background stock info refresh on MyPortfolioDetails page.
- (Fix) Error when updating asset tags.
- (Fix) Some CodeQL warnings.

## 2026-02-21 - v1.0.1

### Fixed/Improvements

- (Patch) Improve AI analysis prompt.
- (Patch) UI/UX improvements.
- (Patch) Fix login page when external authenticator has empty config.

## 2026-02-16 - v1.0.0

### Added/Refactoring/Deprecation

- (Feat) Apply AI to analyze stock symbols.

### Fixed/Improvements

- (Patch) Fix CodeQL warnings.
- (Patch) Other fixes and enhancements.

## 2026-02-12 - v0.8.0

### Added/Refactoring/Deprecation

- (Feat) Add Metadata attribute to AssetEntity, replacing Tags attribute.
- (Feat) Auto update Asset metadata.

### Fixed/Improvements

- (Patch) Fix some CodeQL warnings.

## 2026-02-11 - v0.7.1

### Fixed/Improvements

- (Patch) Fix Portfolio Summary page after adding new Distribution settlement type.
- (Patch) Optimize refreshing stock info in the background.
- (Patch) Improve datetime parsing from datetime picker.
- (Patch) Fix timezone conversion when saving TxBuySell and TxSettlement.
- (Patch) Add global exception handler for API endpoints.

## 2026-02-10 - v0.7.0

### Added/Refactoring/Deprecation

- (Feat) Add Stock Symbol Info page.

### Fixed/Improvements

- (Patch) Optimize loading of stock quotes in the background.
- (Patch) Add settlement type Distribution.

## 2026-02-06 - v0.6.0

### Added/Refactoring/Deprecation

- (Feat) Integrate with FinHub API.

## 2026-02-03 - v0.5.3

### Fixed/Improvements

- (Patch) UI/UX fix.
- (Patch) More accepted date/time formats.

## 2026-02-03 - v0.5.2

### Fixed/Improvements

- (Patch) Update MyPortfolio page.

## 2026-02-03 - v0.5.1

### Fixed/Improvements

- (Patch) Add Unsettled P/L to portfolio summary page.
- (Patch) Redesign portfolio asset page.
- (Patch) UI fix portfolio buys/sells page.
- (Patch) UI fix portfolio settlements page.

## 2026-02-01 - v0.5.0

### Added/Refactoring/Deprecation

- (Refactor) Redesign MyPortfolio Details page.
- (Feat) Add value format and quantity format to market metadata.
- (Feat) Add default market info to portfolio metadata.

### Fixed/Improvements

- (Patch) Fix ROI calculation.

## 2026-01-31 - v0.4.0

### Added/Refactoring/Deprecation

- (Feat) Rework MyPortfolio/Assets view.
- (Feat) Rework MyPortfolio/ROI view.

### Fixed/Improvements

- (Patch) UI fixes and improvements.

## 2026-01-28 - v0.3.2

### Fixed/Improvements

- (Fix) Parsing date/time when creating/updating portfolio transactions.
- (Fix) Parsing date/time when creating/updating portfolio ROI records.

## 2026-01-28 - v0.3.1

### Fixed/Improvements

- (Patch) MyPortfolio page: UI/UX improvement.
- (Patch) MyPortfolio/AddTx+UpdateTx+DeleteTx pagex: UI/UX improvement.

## 2026-01-27 - v0.3.0

### Added/Refactoring/Deprecation

- (Feat) Portfolio owner can manage who have view access to the Portfolio.

### Fixed/Improvements

- (Patch) Add AU/US/VN generic stock markets for timezone and currency purposes.

## 2026-01-26 - v0.2.2

### Fixed/Improvements

- (Patch) Fix Docker build.

## 2026-01-25 - v0.2.1

### Fixed/Improvements

- (Patch) Fix release workflow.
- (Patch) Fix High & Medium CodeQL items.

## 2026-01-25 - v0.2.0

### Added/Refactoring/Deprecation

- (Refactor) page Buy/Sell calculator.
- (Feat) Add tool Price Run.
- (Feat) Manually add/update/delete ROI records.

### Fixed/Improvements

- (Impr) Add flow "Add and Open" when creating new portfolio.

## 2026-01-22 - v0.1.0

### Added/Refactoring/Deprecation

- (Feat) Manage invesment portfolio.
- (Feat) Record buy/sell transactions.
- (Feat) Manage portfolio assets.
- (Feat) Manage ROI records.
