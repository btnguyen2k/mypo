# My Portfolio Tracker release notes

## 2026-08-07 - v2.2.0

### Added/Refactoring/Deprecation

- Feat(UI/portfolio-plan-details): Add dynamic sorting to holdings table.

### Fixed/Improvements

- Fix: Portfolio plan is not saved after manual analyzing.

## 2026-08-02 - v2.1.1

### Fixed/Improvements

- Impr(portfolio-plan-details): Add portfolio plan selector to the plan details breadcrumb.
- Fix(portfolio-details): Settlements tab - Fix data table ordering.

## 2026-08-01 - v2.1.0

### Added/Refactoring/Deprecation

- Feat: Dashboard redesign.

## 2026-07-29 - v2.0.3

### Fixed/Improvements

- Fix(review-portfolio-plan): portfolio plan rebalance plan not saved when portfolio plan analyzed manually.
- Fix(background-task): portfolio plan rebalance plan not saved when portfolio plan analyzed periodically.

## 2026-07-29 - v2.0.2

### Added/Refactoring/Deprecation

- Feat(analyze-portfolio): Add rebalance plan, adapt to FinHub v0.14.1.

### Fixed/Improvements

- Patch(CodeQL): Fix CodeQL warnings.
- Patch(background-tasks): Minor fix background task sleep time.

## 2026-07-20 - v2.0.1

### Fixed/Improvements

- Patch(Background Task): Update background tasks sleep time.
- Fix: Portfolio plan not saved in auto-analyzing background task.

## 2026-07-20 - v2.0.0

### Added/Refactoring/Deprecation

- Refactor: Move the current Dashboard to new page Events.
- Feat: Portfolio perferences.
- Feat: Portfolio reports.

### Fixed/Improvements

- Fix(UI): Fix datatables warning if portfolio has no transactions.
- Fix(UI): Fix datatables reinitialise warning.
- Impr(UI): Rework Portfolio Details page for inactive portfolios.
- Patch: MyPortfolioAdd page accepts new query parameter parentId.
- Impr(UX): Reorder returned TxSettlements list.
- Impr(UI): User can now quickly switch to another portfolio from the MyPortfolioDetails page.
- Fix(UI): Fix an error when Datatables is not loaded/initialized properly.

## 2026-06-21 - v1.6.3

### Fixed/Improvements

- Impr(UI): Add print analysis feature to MyPortfolioPlansDetails view.
- Impr: AutoBackgroundAnalyzePortfolioPlansScanner no longer run spotlight analysis if there is no holdings.
- Patch: Refactor PortfolioPlanMetadata checksum calculation.
- Patch: Analysis feature on MyPortfolioPlansDetails page now includes spotlight analysis.

## 2026-06-17 - v1.6.2

### Fixed/Improvements

- Patch: Better logging and error handling for AutoBackgroundAnalyzePortfolioPlansScanner and AutoBackgroundUpdatePortfolioPlansScanner

## 2026-06-17 - v1.6.1

### Fixed/Improvements

- Patch: AutoBackgroundAnalyzePortfolioPlansScanner analyzes portfolio plans only when description is not empty.
- Impr: AutoBackgroundAnalyzePortfolioPlansScanner analyzes portfolio plans early if there are changes in plan type/description/holdings.
- Patch: Add authentication support to FinHubClient.
- Fix(CodeQL): Fix CodeQL warnings.

## 2026-06-14 - v1.6.0

### Added/Refactoring/Deprecation

- Feat: Add Description to Portfolio Plan.
- Feat: Analyze portfolio plan using FinHub API.
- Feat: Add P&L Portfolio Plan type.
- Feat: Rework Stock Symbol Info page.
- Feat(UI): Rework My Preferences page.
- Feat: Add background task to automatically run analysis on portfolio plans.

### Fixed/Improvements

- Fix(UI): Fix a bug that cause Edit Portfolio Plan page to reload when deleting a holding ticker.

## 2026-05-05 - v1.5.3

### Fixed/Improvements

- (Fix) Incorrect link on Edit Plan page.
- (Patch) UI fixes/improvements.
- (Fix) Auto-populate asset metadata.

## 2026-05-04 - v1.5.2

### Fixed/Improvements

- (Patch) Minor UI/UX fix.

## 2026-05-04 - v1.5.1

### Fixed/Improvements

- (Impr) Portfolio Plan - UI improvements.
- (Impr) My Portfolio - UI rework.

## 2026-04-28 - v1.5.0

### Added/Refactoring/Deprecation

- (Feat) Portfolio Planning.

### Fixed/Improvements

- (Patch) Resolve CodeQL warnings.
- (Fix) Incorrect preview total cost when creating new/updating existing Tx.

## 2026-04-17 - v1.4.0

### Added/Refactoring/Deprecation

- (Refactor) Dashboard rework.

### Fixed/Improvements

- (Patch) Fix CodeQL warnings.
- (Impr) Market alerts (new listings) via Telegram.
- (Impr) Market alerts (dividend events) via Telegram.
- (Impr) Cache index constituents locally.

## 2026-03-13 - v1.3.0

### Added/Refactoring/Deprecation

- (Feat) Clean up old events.
- (Feat) Setup preferences to receive market alerts via Telegram.
- (Feat) Send market alerts via Telegram.

### Fixed/Improvements

- (Improvement) Rework Dashboard.

## 2026-03-07 - v1.2.1

### Fixed/Improvements

- (Improvement) Adjust client timeout when calling AI-assisted APIs from FinHub.
- (Fix) Error when updating Assets.
- (Patch) Minor UI fix in page MyPortfolioDetails.

## 2026-03-06 - v1.2.0

### Added/Refactoring/Deprecation

- (Feat) Fetch new listing announcements from FinHub.

### Fixed/Improvements

- (Improvement) Fetch events from FinHub.
- (Fix) Error while updating an asset (from My Portfolio page).

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
