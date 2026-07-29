# Changelog

## [1.4.0] - 2026-07-27

### Changed

- Stop sending `severity` from `ListAlertsAsync`. `GET /alerts` declares no such filter, so it never filtered, and it becomes a 422 once `ApiVersion` is `2026-07-27` or later. The argument is still accepted and ignored
- Accept either wire shape from `ListEvaluationCriteriaAsync`. The endpoint is moving from a bare array to a paginated envelope, so the client now reads both and keeps returning `List<EvaluationCriteriaResponse>`
- Throw `ArgumentException` from `SearchAsync` when `query` is blank, rather than deferring to a 422 that names the wire parameter `q` instead of the field
- Sync to the current OpenAPI spec, adding 23 paths and dropping the trailing-slash `/sources/` form
- Deprecate `DeleteAgentRunAsync`. It never deleted anything — the endpoint it calls is documented as "Cancel an agent run", and the API has no delete-a-run operation. Use `CancelAgentRunAsync`

### Added

- Add `SeclaiApiVersion` with constants for each dated API version, plus `Default`, `Latest` and `Known`. An `ApiVersion` this release was not built against is rejected at construction, since a newer version can reshape responses this client would mis-decode; set `AllowUnknownApiVersion` to override
- Add `SeclaiClient.Typed`, an opt-in surface carrying typed forms of the 21 methods that return raw JSON — alerts, alert configs, model alerts and recommendations, the model catalog, playground experiments, search, docs search and generation tiers. Each delegates to its raw counterpart and deserializes, so both issue the same request
- Add an `AiConversationHistoryOptions` overload of `GetAgentAiConversationHistoryAsync` carrying the required `step_type` plus `step_id`, `limit` and `offset`
- Add 24 response models covering those endpoints, including `AlertResponse`, `AlertDetailResponse`, `AlertConfigResponse`, `ModelAlertResponse`, `ExperimentDetailResponse` and `SearchResponse`
- Add `GetMeAsync` returning the authenticated user's account ID and organization memberships
- Add `DisableAgentAsync`, `EnableAgentAsync`, and `GetAgentCallersAsync` to pause and resume an agent across every trigger path
- Add `SetEmailTriggerConfigAsync` to set the alias, sender allowlist, and inbound-handling flags on an `EMAIL_RECEIVED` trigger. Unset properties are omitted rather than sent as `null`, which the API reads as "clear this field"
- Add agent-email opt-out methods `ListAgentEmailOptOutsAsync` and `RemoveAgentEmailOptOutAsync`
- Add inbound sender blocklist methods `ListBlockedEmailSendersAsync`, `BlockEmailSenderAsync`, `UnblockEmailSenderAsync`, and `SetAutoBlockModeAsync`
- Add inbound-email observability methods `ListInboundEmailRejectionsAsync`, `GetInboundEmailStatusAsync`, `CancelQueuedEmailRunsAsync`, and `ResumeInboundEmailAsync`
- Add email domain management: `ListEmailDomainsAsync`, `AddEmailDomainAsync`, `RemoveEmailDomainAsync`, `VerifyEmailDomainAsync`, `SetPrimaryEmailDomainAsync`, `UseSharedEmailDomainAsync`, `SendEmailDomainTestEmailAsync`, and `GetDmarcSummaryAsync`
- Add `GetGenerationTiersAsync` mapping each media-generation modality and tier to its model and cost
- Add `SearchDocsAsync` for keyword or semantic search over the Seclai documentation
- Add `ListEvaluationCriteriaPageAsync` for the canonical `{data, pagination}` envelope, which the endpoint emits once `ApiVersion` is `2026-07-27` or later
- Add an `ApiVersion` client option, sent as the `Seclai-Version` header, opting into dated API changes released on or before that date. Omitted by default, so upgrading the SDK alone never changes response shapes
- Add `GetApiVersionAsync` and `UpdateApiVersionAsync` to read the version a request resolves to and to pin or clear the account's version
- Add the `disabled`, `disabled_at` and `disabled_reason` fields to `AgentSummaryResponse`, so the paused state set by `DisableAgentAsync` and `EnableAgentAsync` is visible in the response
- Add `wait_ms` to `AgentRunResponse`, `intent_assessment` to `GenerateAgentStepsResponse`, and `media_types` to `SourceResponse`, `CreateSourceRequest` and `UpdateSourceRequest`

### Fixed

- Decode either wire shape in `ListRunEvaluationResultsAsync`. The endpoint answers with a bare array, which the declared envelope type could not read, so the method returned nothing; it now also reads the canonical `{data, pagination}` envelope. `ListAgentEvaluationResultsAsync` is genuinely flat and is unaffected
- Send the `q` query parameter from `SearchAsync` instead of `query`. The API requires `q`, so every search call had been failing validation since 1.1.0
- Paginate `ListModelAlertsAsync` with the `offset` the endpoint declares instead of `page`, which it does not accept — every page after the first returned page 1
- Send `step_type` from `GetAgentAiConversationHistoryAsync` via the new `AiConversationHistoryOptions` overload. The API marks `step_type` required and the previous signature had no way to supply it, so every call answered 422. That signature is kept and marked `[Obsolete]` so compiled consumers keep working
- Request `GET /sources` rather than `GET /sources/`. The trailing-slash form is no longer declared by the API
- Point `CancelAgentRunAsync` at `DELETE /agents/runs/{run_id}`. It posted to `/agents/runs/{run_id}/cancel`, a path the API has never exposed, so cancelling a run always failed

## [1.3.0] - 2026-06-05

### Added

- Add `GetAgentAttachmentReferencesAsync` to read an agent's static attachment-reference contract before staging uploads ([#9](https://github.com/seclai/seclai-csharp/pull/9))
- Add `DownloadAgentRunAttachmentAsync` for a file emitted by a run step ([#9](https://github.com/seclai/seclai-csharp/pull/9))
- Add `DeleteExperimentAsync` to soft-delete a model playground experiment ([#9](https://github.com/seclai/seclai-csharp/pull/9))

## [1.2.0] - 2026-05-22

### Added

- Add `PreviewImportAgentAsync` to dry-run an agent definition import and surface unresolved entity refs ([#8](https://github.com/seclai/seclai-csharp/pull/8))

## [1.1.4] - 2026-04-24

### Added

- Add `ListModelsAsync` and `GetModelAsync` for the model catalog ([#7](https://github.com/seclai/seclai-csharp/pull/7))
- Add model playground methods `ListExperimentsAsync`, `CreateExperimentAsync`, `GetExperimentAsync`, and `CancelExperimentAsync` ([#7](https://github.com/seclai/seclai-csharp/pull/7))

## [1.1.3] - 2026-04-02

### Added

- Add agent definition export support ([#6](https://github.com/seclai/seclai-csharp/pull/6))

## [1.1.2] - 2026-03-27

### Changed

- Default the SSO domain, client ID, and region so a profile only needs `sso_account_id` ([#5](https://github.com/seclai/seclai-csharp/pull/5))

## [1.1.1] - 2026-03-26

### Added

- Add OAuth SSO authentication with `~/.seclai/config` profiles, an on-disk token cache, and automatic refresh ([#4](https://github.com/seclai/seclai-csharp/pull/4))
- Add an `AccountId` option, sent as the `X-Account-Id` header, to switch organization account context ([#4](https://github.com/seclai/seclai-csharp/pull/4))

## [1.1.0] - 2026-03-24

### Added

- Expand endpoint coverage to knowledge bases, memory banks, sources, source exports, embedding migrations, content, solutions, alerts, governance, evaluations, and the AI assistants ([#3](https://github.com/seclai/seclai-csharp/pull/3))
- Add `RunStreamingAgentAsync` for SSE-based run streaming ([#3](https://github.com/seclai/seclai-csharp/pull/3))
- Add `RunAgentAndPollAsync` for environments where SSE is impractical ([#3](https://github.com/seclai/seclai-csharp/pull/3))
- Add `SearchAsync` across all resource types in an account ([#3](https://github.com/seclai/seclai-csharp/pull/3))

## [1.0.7] - 2026-01-30

### Added

- Add `UploadFileToContentAsync` to replace existing content with a file upload

## [1.0.6] - 2026-01-27

### Changed

- Accept a run ID alone in the agent run detail and cancel methods; the agent ID is no longer required

## [1.0.5] - 2026-01-27

### Added

- Add an option to include step details in the agent run detail response

## [1.0.4] - 2026-01-27

### Fixed

- Correct the file upload endpoint

## [1.0.3] - 2026-01-17

### Fixed

- Correct the published documentation link

## [1.0.2] - 2026-01-13

### Fixed

- Correct the AI assistant request paths

## [1.0.1] - 2026-01-13

### Added

- Add `RunStreamingAgentAndWaitAsync` to block until a streaming run completes

## [1.0.0] - 2026-01-12

_Stable release. Packaging only; no API changes since 0.0.1._

## [0.0.1] - 2026-01-12

_Initial release._

[1.4.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.4.0
[1.3.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.3.0
[1.2.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.2.0
[1.1.4]: https://github.com/seclai/seclai-csharp/releases/tag/1.1.4
[1.1.3]: https://github.com/seclai/seclai-csharp/releases/tag/1.1.3
[1.1.2]: https://github.com/seclai/seclai-csharp/releases/tag/1.1.2
[1.1.1]: https://github.com/seclai/seclai-csharp/releases/tag/1.1.1
[1.1.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.1.0
[1.0.7]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.7
[1.0.6]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.6
[1.0.5]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.5
[1.0.4]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.4
[1.0.3]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.3
[1.0.2]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.2
[1.0.1]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.1
[1.0.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.0.0
[0.0.1]: https://github.com/seclai/seclai-csharp/releases/tag/0.0.1
