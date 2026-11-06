# Changelog

## [1.8.0] - 2026-11-05

### Changed

- Return the `result` snapshot from `RunStreamingAgentAndWaitAsync` when the stream ends after the `result` event (sent once `ResultReportedAt` is set and `Output` is the answer) but before `done`, where it returned the `init` snapshot. It still returns rather than throws; set `ThrowOnIncomplete` to throw instead
- Move `SeclaiApiVersion.Latest` from `2026-10-28` to `2026-11-04`, which opts a client that sends `Latest` into two more versions: from `2026-11-02` a definition or sampling-rule write (other than an import) that references a metadata key no trigger declares is refused (400 from `UpdateAgentDefinitionAsync`), where older versions declare each such key optional instead; from `2026-11-04` the API refuses the agent evaluation mode `eval_and_retry`, a new evaluation criterion `StepId` that does not name a result step (`display_result` or `streaming_result`), and `CreateEvaluationResultRequest.RetryTriggered = true`, each with a 400, where older versions store `eval_and_retry` as `sample_and_flag` and accept any step id. It is a `const`, so code compiled against an earlier release keeps the old value until it is rebuilt
- Note server-side changes that apply on every API version, whatever `ApiVersion` a client sends: a run whose `metadata` has a custom key its trigger does not declare, or lacks a required one, is refused with a 400, so declare custom keys with `SetMetadataKeysAsync` first; an agent definition holding a `gate` or `retry` step is rejected (use `if_else`, or `repeat_until` to run steps again); the `agent_eval_fail` rollback trigger is accepted, never fires, and is no longer in the default rollback triggers; `MaxRetries` and `RetryOnFailure` are accepted and have no effect; and `AgentSummaryResponse.EvaluationMode` defaults to `sample_and_flag` server-side instead of `eval_and_retry`
- Deprecate `SetSlackTriggerConfigRequest.Input`. The API now ignores it: a Slack run's input is the text after `/seclai <alias>`, or the message the shortcut was used on, and its files. Set the command name with the new `Alias`

### Added

- Add a `RunAgentAndPollAsync` overload taking `RunAgentAndPollOptions` (`PollInterval`, `Timeout`, `IncludeStepOutputs`, `ReturnOnResult` and `ThrowOnIncomplete`), and `RunStreamingAgentAndWaitWithOptionsAsync` taking `RunStreamingAgentAndWaitOptions` (`Timeout`, `ReturnOnResult` and `ThrowOnIncomplete`). With `ReturnOnResult` each returns as soon as the run reports its result (the `result` event, or `ResultReportedAt` set on a poll), while `Status` may still be `processing` and the run can still fail
- Add `ThrowOnIncomplete` to both options types, so these helpers never return a run that has not finished. `RunStreamingAgentAndWaitWithOptionsAsync` then throws `StreamingException` when the stream ends without a `done` event, or breaks with an `IOException` or `HttpRequestException`, instead of returning the latest snapshot or letting the transport exception through, and cancelling the caller's token throws `OperationCanceledException`; `RunAgentAndPollAsync` throws `StreamingException` when its timeout elapses, instead of `OperationCanceledException`. The exception's `Run` holds the latest snapshot. It is off by default and will become the default in the next major version
- Add `StreamingException.Run`, the latest snapshot of the unfinished run, and a constructor that sets it. The timeout `StreamingException` of `RunStreamingAgentAndWaitAsync` carries it too
- Add `GetMetadataKeysAsync` and `SetMetadataKeysAsync` to read and replace the run metadata keys an agent's trigger declares, with `SetMetadataKeysRequest`, `MetadataKeyDeclaration`, `DeclarableMetadataKey` and `MetadataKeysResponse`. `SetMetadataKeysRequest` is built with its list (`new SetMetadataKeysRequest(keys)`); its constructor and `MetadataKeys` setter throw `ArgumentNullException` on null, and an empty list declares no keys. `MetadataKeyDeclaration.Required` is omitted when `null`, which the API treats as false
- Add `GetKnowledgeBaseMetadataKeyStatsAsync` to report how often each item-metadata key appears on a knowledge base's content over all time and the last 30 and 7 days, returning `KnowledgeBaseMetadataKeyStatsResponse` with `MetadataKeyWindowResponse`, `MetadataKeyCountResponse` and `MetadataValueCountResponse`
- Add `Alias` to `SetSlackTriggerConfigRequest` and `SlackTriggerConfigResponse`, the name people type after `/seclai` to run the agent
- Add `ResultReportedAt` to `AgentRunResponse`, when the run reported success; from then on `Output` is final
- Add the `SeclaiApiVersion.V2026_11_02` and `V2026_11_04` constants, so those versions are accepted as `ApiVersion`
- Add agent evaluation and prompt-model settings to `UpdateAgentRequest` and `AgentSummaryResponse`: `EvaluationMode`, `SamplingConfig`, `DefaultEvaluationTier`, `PromptModelAutoUpgradeStrategy`, `PromptModelAutoRollbackEnabled` and `PromptModelAutoRollbackTriggers`, plus `TriggerType` on the response. On the request, a null property is not sent and leaves the setting unchanged; to clear the sampling config, tier or rollback triggers, set `SetSamplingConfig`, `SetDefaultEvaluationTier` or `SetPromptModelAutoRollbackTriggers` and leave the value null. `MaxRetries` and `RetryOnFailure` are included for completeness and have no effect
- Add `InputUploadId`, `InputUploadIds` and `ReplayOfRunId` to `AgentRunRequest` and `AgentRunStreamRequest`, to run an agent on uploaded files or rerun it on a prior run's files; add `AgentRunStepResponse.Input` and `CreateAgentRequest.TriggerType`
- Add the AI assistants' conversation and plan details: `HistorySince` on `AiAssistantGenerateRequest` and `MemoryBankAiAssistantRequest` to scope context to the current session; `Mode`, `AgentSteps`, `AgentDescription` and `TriggerType` on `GenerateAgentStepsRequest` to refine an existing workflow; `ConversationId`, `Success`, `Note`, `AgentConfig` and `ExamplePrompts` on `GenerateAgentStepsResponse`; `ConversationId` and `ResultingConfig` on `MemoryBankConversationTurnResponse`; and `ProposedActionResponse.Preview`, the dry run of a proposed agent action
- Add knowledge-base retrieval defaults: `RerankerModel`, `DefaultTopK`, `DefaultTopN` and `DefaultScoreThreshold` on `CreateKnowledgeBaseRequest` and `KnowledgeBaseResponse`, and `Sources` (a list of the new `SourceConnectionResponse`) and `Readonly` on the response
- Add memory-bank embedding and compaction settings to `CreateMemoryBankRequest` and `MemoryBankResponse`: `Mode`, `EmbeddingModel`, `Dimensions`, `ChunkSize`, `ChunkOverlap`, `CompactionPrompt`, `MaxSizeTokens` and `MaxTurns`, and the deprecated `MaxAgeDays`; add `RetentionDays` and `SourceConnectionId` to the response
- Add `DateFrom`, `DateTo`, `MetadataFilter` and `QueryFilter` to `CreateExportRequest`, to export part of a source, and the export's filters, sizes, progress, error and requester to `ExportResponse`
- Add `IndexMode` to `CreateSourceRequest` and `SourceResponse`, and `FreeRetentionDays` and `SystemManaged` to `SourceResponse`
- Add the criterion's agent, step, mode, prompt, tier, expectation config, pass threshold, result summary, `Enabled` and timestamps to `EvaluationCriteriaResponse`
- Add `Agents`, `KnowledgeBases` and `SourceConnections` to `SolutionResponse`, with the new `SolutionAgentResponse`, `SolutionKnowledgeBaseResponse` and `SolutionSourceConnectionResponse`
- Add `Pagination` to `AgentListResponse`, `ExportListResponse` and `SolutionListResponse`

### Fixed

- Throw `StreamingException` from `RunStreamingAgentAndWaitAsync` when its timeout elapses or the caller cancels while the closed stream reads as ended, as documented, where it could return the `init` snapshot instead. An event whose lines arrived before the timeout but whose closing blank line had not is now read, so a `done` received in time is returned
- Return step outputs from `RunAgentAndPollAsync` with `includeStepOutputs` when the run has already ended in the response that starts it, by fetching the run once. That response carries no step outputs, and it used to be returned as is
- Fill `Total` on `AgentListResponse`, `ExportListResponse` and `SolutionListResponse` from the response's `pagination`. The API sends no top-level `total` there, so it was always 0
- Add `CreateKnowledgeBaseRequest.SourceIds`. The API requires at least one source, and there was no way to send it
- Read the plan from `GenerateGovernanceAiPlanAsync`: `GovernanceAiAssistantResponse` now has `ConversationId`, `Note`, `Success`, `ProposedActions` (a list of the new `ProposedPolicyActionResponse`), `PromptCallId` and `ExamplePrompts`. Its only property was `AssistantResponse`, which the API does not send
- Read the embedding migration from `GetSourceEmbeddingMigrationAsync`, `StartSourceEmbeddingMigrationAsync` and `CancelSourceEmbeddingMigrationAsync`: `SourceEmbeddingMigrationResponse` now has `Id`, `Phase`, progress, target model and dimensions, failure message and timestamps. `MigrationId` stays, but the API does not send it

## [1.7.0] - 2026-10-31

### Changed

- **Breaking:** Make a call that passes a bare `default` literal where a new options overload also fits, such as `ListAlertsAsync(default)` or `ListAgentRunsAsync("a1", default)`, ambiguous at compile time. Pass `null` or name the argument (`page: default`). A bare `null` still binds to the original overload on every compiler, since the options types are structs, and compiled code is unaffected
- Move `SeclaiApiVersion.Latest` from `2026-10-21` to `2026-10-28`, which opts a client that sends `Latest` into one more version: an `if_else` or `switch` step's `AgentRunStepResponse.Output` is the result of the arm it took (its input when that arm is empty or it took none) rather than the decision object, so read the arm from the new `Decision`. It is a `const`, so code compiled against an earlier release keeps the old value until it is rebuilt

### Added

- Add `ListAgentExamplesAsync(query, useCase, page, limit)` to list the agent gallery, returning `AgentExampleListResponse`, and add `AgentTemplate`, `ResourceBindings` and `CreateMissingResources` to `CreateAgentRequest` to create an agent from a gallery entry. `AgentSummaryResponse` gains `CreatedResources`, the resources the create made
- Add `SetSlackTriggerConfigAsync` to set the Slack connection, form, channels, visibility and conversation settings of an agent's SLACK_COMMAND trigger, with `SetSlackTriggerConfigRequest`, `SlackTriggerConfigResponse` and `SlackFormField` for each form field. An empty `Form` or `ChannelIds` clears it, and `ClearSlackTriggerMaxTurnsAsync` removes the turn limit
- Add agent run limits: `MaxCreditsPerPeriod`, `MaxCreditsPerRun`, `MaxRunsPerDay`, `MaxRunsPerHour` and `MaxRunsTotal` on `AgentSummaryResponse` and `UpdateAgentRequest`, with a `SetMax…` flag for each on `UpdateAgentRequest` that removes the limit when its value is left null, and `ApplyImportRunLimits` to take the limits from an imported `AgentDefinition`
- Add source spend limits: `MaxCreditsPerPeriod`, `SpendCapAction` and `SpendCapDeleteOrder` on `UpdateSourceRequest` and `SourceResponse`, `SpendCapReached` on `SourceResponse`, and `ClearSourceFieldsAsync` with `SourceField` to send a source setting as `null`, which removes the spend limit (`MaxCreditsPerPeriod`) or the retention window (`RetentionDays`); `ClearSourceRetentionAsync` now calls it. `ReleaseWaitingReason` on `SourceContentStatusResponse` says why an approved held item is still waiting
- Add `Decision` to `AgentRunStepResponse`, the arm an `if_else` or `switch` step took
- Add an options overload for every list or lookup whose endpoint takes filters this client did not send, leaving the existing signatures unchanged: `ListAgentRunsAsync` (`AgentRunListOptions.Status`), `ListEvaluationResultsAsync`, `ListAgentEvaluationResultsAsync`, `ListEvaluationRunsAsync` and `ListCompatibleRunsAsync` (status, step, flagged-only and time-range filters), `GetNonManualEvaluationSummaryAsync` (`Days`, `StartDate`, `EndDate`), `ListAlertsAsync` (agent, source connection and time range), `ListAlertConfigsAsync` (agent, source connection, `Scope`), `ListOrganizationAlertPreferencesAsync` (`OrganizationId`, `IncludeDefaults`), `ListMemoryBanksAsync` (`Type`), `GetMemoryBankStatsAsync` (period), `GetMemoryBankAiLastConversationAsync` and `GetAiAssistantMemoryBankHistoryAsync` (`Limit`, `Offset`), `ListModelsAsync` (`SupportsInputMedia`, `SupportsOutputMedia`), `ListModelAlertsAsync` (`AgentId`, `UnreadOnly`), `GetModelRecommendationsAsync` (capability and token minimums) and `ListSolutionsAsync` (`Search`), plus `ListGovernanceAiConversationsAsync(limit)`. The options overloads of `ListAlertsAsync`, `ListAlertConfigsAsync`, `ListModelAlertsAsync`, `GetModelRecommendationsAsync` and `ListModelsAsync` also have typed counterparts on `client.Typed`; the API does not declare a schema for `GetMemoryBankStatsAsync`, which stays `JsonElement`
- Add the `SeclaiApiVersion.V2026_10_28` constant, so that version is accepted as `ApiVersion`

## [1.6.0] - 2026-10-21

### Changed

- Move `SeclaiApiVersion.Latest` from `2026-10-03` to `2026-10-21`, which opts a client that sends `Latest` into six more versions: from `2026-10-05` a content item held for review reports `ContentStatus` `completed` rather than `failed`, so code that looks for `failed` to find held items should read `HeldForReview`; from `2026-10-13` `CreateSourceAsync` and `UpdateSourceAsync` reject an unknown `MediaTypes` kind, or any kind on a source other than website or RSS, with a 400; from `2026-10-16` `UploadAgentInputAsync` accepts any file type and the upload is ready at once; from `2026-10-18` `DocumentImages` defaults to true with a text-only embedder too; from `2026-10-19` `CreateSourceAsync` rejects `PollingMaxItems` on a source other than RSS with a 400; and from `2026-10-21` a result whose evaluator returned no verdict counts only in `Error` of `GetNonManualEvaluationSummaryAsync`, not in `Failed` or its pass and failure rates, and is left out of `AverageScore` of `GetEvaluationCriteriaSummaryAsync`. It is a `const`, so code compiled against an earlier release keeps the old value until it is rebuilt
- Deprecate `EvaluationResultResponse.Passed` and `EvaluationResultResponse.Comment`. The API sends neither, so they were always false and null. Read `Status` and `Details`

### Added

- Add `SetAcceptedAttachmentsAsync` to declare the named input attachments an agent's trigger accepts, with `SetAcceptedAttachmentsRequest`, `AcceptedAttachmentSlot` and `AcceptedAttachmentsResponse`. A null list is sent as-is and accepts any files
- Add `AcceptedAttachments` to `CreateAgentRequest` and `UpdateAgentDefinitionRequest` to declare the slots in the same save, and to `AgentAttachmentRefsApiResponse` to read them; add `TriggerId` to `AgentSummaryResponse` for the trigger endpoints
- Add content-attachment methods: `ListContentAttachmentsAsync`, `GetContentAttachmentAsync` with its indexed text and `FileWithheld` reason, `ListContentAttachmentEmbeddingsAsync`, and `DownloadContentAttachmentAsync`, which returns the raw `HttpResponseMessage` for streaming
- Add `ListEmailSourceRejectionsAsync` for the mail an email content source refused, returning `EmailSourceRejectionListResponse`
- Add email and cloud-drive source settings: `EmailAllowedSenders`, `DocumentImages`, `FeedAttachments` and the `CloudDrive*` fields on `CreateSourceRequest` (including `CloudDriveConnectionId`) and `UpdateSourceRequest`, and the same on `SourceResponse` plus `EmailAddress`. The request fields are omitted when `null`, so an update leaves them unchanged
- Add `SkippedAttachments` to `ContentDetailResponse`, listing the parts of an email that were not stored as attachments, and `HeldForReview` to `SourceContentStatusResponse`
- Add `RunOnAttachmentMismatch` to `SetEmailTriggerConfigRequest` and `EmailRunOnAttachmentMismatch` to `EmailTriggerConfigResponse`
- Add `IncludeReasoning` to `AgentRunStreamRequest` to receive `stream_reasoning` events from `RunStreamingAgentAsync`; it is omitted when `null`. `RunStreamingAgentAndWaitAsync` reads only the `init` and `done` events, so it does not send the field and reasoning never counts against its timeout
- Add `SeclaiApiVersion` constants `V2026_10_05`, `V2026_10_13`, `V2026_10_16`, `V2026_10_18`, `V2026_10_19` and `V2026_10_21`, so those versions are accepted as `ApiVersion`
- Add `Status` to `TestDraftEvaluationResponse`: `passed`, `failed`, or `error` when the evaluator returned no verdict. `Score` and `Passed` are then not a verdict: `2026-10-21` sends them as `null`, which this client reads as 0 and false, and earlier versions send 0.5 and false, so check `Status` rather than `Passed` to tell a failure from no verdict
- Add `Error` to `NonManualEvaluationSummaryResponse`, the count of results whose evaluator returned no verdict
- Add `Status`, `AgentRunId`, `AgentStepRunId`, `Details`, `RetryTriggered`, `RetryCount`, `Flagged` and `EvaluatedAt` to `EvaluationResultResponse`, which `ListEvaluationResultsAsync` and `CreateEvaluationResultAsync` return
- Add `ClearSourceRetentionAsync` to remove a source's retention window, so its content is kept indefinitely. `UpdateSourceAsync` no longer clears it, see below
- Add `ClearEvaluationCriteriaFieldsAsync(criteriaId, fields, cancellationToken)` with `EvaluationCriteriaField` to clear an evaluation criterion's description, evaluation prompt, expectation config or evaluation tier by sending it as `null`. A cleared tier falls back to the agent's default tier, which is the only way to undo a pinned one
- Add `RetentionDays` to `CreateMemoryBankRequest`, omitted when `null` so the API's default applies, and `RetainIndefinitely`, which sends `retention_days` as `null` so the bank keeps entries indefinitely. Setting both throws an `ArgumentException`. `UpdateMemoryBankRequest` gains the same `RetainIndefinitely`, which sends `retention_days` as 0, the value an update reads as "keep indefinitely"
- Document the 402 that `GetContentAttachmentAsync`, `ListContentAttachmentEmbeddingsAsync` and `DownloadContentAttachmentAsync` throw when the account has no credits left

### Fixed

- Omit unset properties from every request body instead of sending them as `null`. For most properties the API reads an omitted field and a `null` the same way, so nothing changes; for these it does not:
  - `UpdateSourceRequest.RetentionDays`: an update that left it unset, such as a rename, cleared the source's retention window, so the source kept its content indefinitely. It now leaves the window unchanged. To remove the window, call `ClearSourceRetentionAsync`
  - `UpdateEvaluationCriteriaRequest`: every update sent all seven properties. With `StepId` unset it failed with a 400 ("step_id is required"); with `PassThreshold` unset it wrote a null into a NOT NULL column; with both set it cleared `Description`, `EvaluationPrompt`, `EvaluationTier` and `ExpectationConfig` when they were unset. Unset properties are now left unchanged. To clear one of those four, call `ClearEvaluationCriteriaFieldsAsync`
  - Properties the API rejects as `null` with a 422 and defaults when omitted. A request that left one unset failed and now gets the default: `AddEmailDomainRequest.Delegated` (false), `AgentRunRequest.Priority` (false), `AgentTraceSearchRequest.TopN` (10), `AiAssistantAcceptRequest.ConfirmDeletions` (false), `BlockEmailSenderRequest.MatchType` (`address`), `CreateAlertConfigRequest.DistributionType` (`owner`) and `Enabled` (true), `CreateEvaluationCriteriaRequest.PassThreshold` (0.5) and `Enabled` (true), `CreateEvaluationResultRequest.Flagged` (false), `RetryCount` (0) and `RetryTriggered` (false), `CreateMemoryBankRequest.Type` (`conversation`), `CreateSolutionRequest.Description` (empty), `PlaygroundCreateRequest.SystemPrompt` (empty), `EvaluationMode` (`manual`), `EvaluationComplexity` (`medium`) and `IncludeStepOutputInEvaluation` (false), `StandaloneTestCompactionRequest.BankType` (`conversation`) and `EntryCount` (5), `TestCompactionRequest.EntryCount` (5) and `TestDraftEvaluationRequest.PassThreshold` (0.5)

  `CreateAlertConfigRequest.CooldownMinutes` and `UpdateMemoryBankRequest.RetentionDays` are unchanged: the API resolves a `null` cooldown to the per-alert-type default and reads a `null` retention as "keep", as it does an omitted one; an update still clears the memory-bank retention with 0. `SetAcceptedAttachmentsRequest.AcceptedAttachments` and `UpdateApiVersionRequest.Version` are still sent when `null`, since null is their value, and upload `metadata` is sent as written, nulls included

## [1.5.1] - 2026-10-05

### Changed

- Throw `ApiException` from the typed list methods of version-gated endpoints when a successful response is not a list — an error-shaped object, text, `null`, or an empty body. It carries the request URL and the response body. Earlier, depending on the method and the body, it was an empty or null result, a `JsonException`, an `InvalidOperationException`, or already an `ApiException`; an empty list reads as "no results". An explicit `"data": null` is an empty list ([#12](https://github.com/seclai/seclai-csharp/issues/12))
- Send only the SDK's value when `DefaultHeaders` also names the API-key header, `Authorization` under a bearer option, or `X-Account-Id` with `AccountId` set. The value from `ApiKey`, the bearer options or `AccountId` now wins; 1.5.0 sent both, as `k, other`, and left the server to choose ([#13](https://github.com/seclai/seclai-csharp/issues/13))
- Send one value when `DefaultHeaders` names a header twice in different letter case, or names `Accept` on a method that sets it — every method except `DownloadSourceExportAsync` and `DownloadAgentRunAttachmentAsync`, which set none and still send the `DefaultHeaders` one. Where the SDK sets `Accept`, its value is the one sent; 1.5.0 appended the other ([#13](https://github.com/seclai/seclai-csharp/issues/13))
- Change the order of the request headers the SDK writes. It was the auth headers, `Accept`, `Seclai-Version`, then `DefaultHeaders`; it is now `DefaultHeaders`, `Accept`, the auth headers, then `Seclai-Version`. The headers and their values are the same when no two layers name the same one ([#13](https://github.com/seclai/seclai-csharp/issues/13))

### Added

- Add `Typed.ListMemoryBankTemplatesAsync` and `Typed.GetAgentsUsingMemoryBankAsync`, returning the list as `List<Dictionary<string, JsonElement>>` on both response shapes. The raw methods of the same name are unchanged and return the response body as sent ([#12](https://github.com/seclai/seclai-csharp/issues/12))
- Add `Pagination` to `EvaluationResultListResponse`, `EvaluationRunSummaryListResponse`, `CompatibleRunListResponse`, `AgentEmailOptOutListResponse`, `BlockedEmailSenderListResponse`, `OrganizationAlertPreferenceListResponse`, `EmailDomainsListResponse`, `ExperimentListResponse` and `GenerationTierListResponse`, and `Page` and `Limit` to `EvaluationResultListResponse` ([#12](https://github.com/seclai/seclai-csharp/issues/12))

### Fixed

- Return the items, and `Total` where the model has it, from `ListAgentEmailOptOutsAsync`, `ListBlockedEmailSendersAsync`, `SetAutoBlockModeAsync`, `ListOrganizationAlertPreferencesAsync`, `ListEmailDomainsAsync`, `Typed.ListExperimentsAsync` and `Typed.GetGenerationTiersAsync` once `ApiVersion` is `2026-07-27` or later. The API then answers with `{data, pagination}`, and these read only their default key: `Items`, `Experiments` and `Tiers` came back empty, and `Preferences` and `Domains` null, with no error ([#12](https://github.com/seclai/seclai-csharp/issues/12))
- Return the list from `GetAgentCallersAsync`, `ListInboundEmailRejectionsAsync`, `ListGovernanceAiConversationsAsync`, `ListSolutionConversationsAsync` and `Typed.ListModelsAsync` once `ApiVersion` is `2026-07-27` or later. They threw `JsonException` ([#12](https://github.com/seclai/seclai-csharp/issues/12))
- Fill `Total`, and `Page` and `Limit` where the model has them, once `ApiVersion` is `2026-07-27` or later, on `ListEvaluationResultsAsync`, `ListAgentEvaluationResultsAsync`, `ListRunEvaluationResultsAsync`, `ListEvaluationRunsAsync`, `ListCompatibleRunsAsync`, `Typed.ListAlertConfigsAsync` and `Typed.ListModelAlertsAsync`. They stayed 0 because the counts arrive inside `pagination` ([#12](https://github.com/seclai/seclai-csharp/issues/12))
- Reject an unknown or empty `Seclai-Version` set on a supplied `HttpClient`'s `DefaultRequestHeaders` with `ConfigurationException`, at construction and before each request. This is a new rejection: the value was sent unchecked, bypassing the guard on `ApiVersion`, and a caller relying on that must now set `AllowUnknownApiVersion`. Two different values there are rejected even with that option; the same value twice is sent once ([#13](https://github.com/seclai/seclai-csharp/issues/13))
- Correct the README's API-versioning section: it listed eleven methods as unsafe after opting in, which is no longer true, and it did not say that opting in turns paging on for the evaluation-criteria, run-evaluation-result and alert-config listings ([#12](https://github.com/seclai/seclai-csharp/issues/12))

## [1.5.0] - 2026-10-04

### Changed

- Move `SeclaiApiVersion.Latest` from `2026-07-27` to `2026-10-03`. It is a `const`, so code compiled against an earlier release keeps the old value until it is rebuilt

### Added

- Add cloud-drive connection management: `ListCloudDriveProvidersAsync`, `ListCloudDrivesAsync`, `GetCloudDriveAsync`, `UpdateCloudDriveAsync`, `DisconnectCloudDriveAsync`, `DeleteCloudDriveAsync`, `GetAgentsUsingCloudDriveAsync`, and `ListCloudDriveRejectionsAsync` for the files a connection skipped. The list methods return the items from either wire shape, and `CloudDriveUpdateRequest` omits unset properties rather than sending `null`
- Add `ListSourceContentsAsync` and `GetSourceContentStatusAsync` to read the indexing status of a source's content, filtered by status or by the `ContentVersionId` values the upload methods return. An empty id list matches nothing and returns an empty page without sending a request
- Add `ListEmbeddingModelsAsync` and `ListRerankerModelsAsync` for the embedder and reranker catalogs with their pricing. Read the models through `Items`, since they arrive under `models` by default and under `data` once `ApiVersion` is `2026-07-27` or later
- Add `SeclaiApiVersion` constants `V2026_08_03`, `V2026_08_21`, `V2026_09_28`, `V2026_09_30` and `V2026_10_03`, so those versions are accepted as `ApiVersion`
- Add `Attachments` to `AgentRunResponse` and `AgentRunStepResponse`, listing the files a run or step produced as `AgentRunFileResponse` on every API version. Each `Id` is accepted by `DownloadAgentRunAttachmentAsync`
- Add `TracePurgedAt` to `AgentRunResponse` and `Warnings` to `AgentRunStepResponse`
- Add `StripQuotedReplyChains` to `CreateMemoryBankRequest`, `UpdateMemoryBankRequest` and `MemoryBankResponse`
- Add reasoning-effort support: `Effort` on `PlaygroundCreateRequest` and `ExperimentDetailResponse`, and `EffortOptions` on `PromptModelResponse` and `ModelRecommendationResponse`
- Add `ChatCapable`, `GenerationCreditsPerVariant` and `Input30mCacheWriteCreditsPer1000Tokens` to `PromptModelResponse`, and the same 30-minute cache rate to `VariantOptionResponse`
- Add `ExtractedMediaCapped`, `ExtractedMediaCount` and `ExtractedMediaLimit` to `ContentDetailResponse`, `MediaName`, `PageNumber`, `SourceMime` and `SourceUrl` to `ContentEmbeddingResponse`, and `EmbedderWarning` to `FileUploadResponse` and `ContentFileUploadResponse`
- Add `GovernanceConversationId` to `AiAssistantFeedbackRequest`
- Add `Page`, `Limit` and `Pagination` to `KnowledgeBaseListResponse` and `MemoryBankListResponse`, plus `KnowledgeBases` and `MemoryBanks` as the default-shape names for `Data`

### Fixed

- Return the knowledge bases from `ListKnowledgeBasesAsync`. The endpoint answers with `knowledge_bases` by default and the model read only `data`, so `Data` was always empty; it is now filled from either key, and `Total` from the flat field or from `pagination`
- Return the memory banks from `ListMemoryBanksAsync`, which had the same defect with `memory_banks`
- Return the matches from `SearchAgentRunsAsync`. The endpoint answers with `matches` and the model read `results`, so `Results` was always null

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

- Validate a `Seclai-Version` supplied through `DefaultHeaders`, not just the `ApiVersion` option, and send it once. `HttpHeaders` appends rather than replaces, so the caller's entry previously joined ours and left two values on the request for the server to choose between
- Throw from `GetAgentAiConversationHistoryAsync` when `StepType` is unset, rather than omitting the parameter and deferring to a 422 naming the wire parameter
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

[1.8.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.8.0
[1.7.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.7.0
[1.6.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.6.0
[1.5.1]: https://github.com/seclai/seclai-csharp/releases/tag/1.5.1
[1.5.0]: https://github.com/seclai/seclai-csharp/releases/tag/1.5.0
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
