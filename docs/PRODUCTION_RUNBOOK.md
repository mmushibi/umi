Production Deployment Runbook — UmiHealth

Scope: Steps to deploy, validate, and rollback UmiHealth production services.

Prerequisites
- CI/CD secrets set (see docs/SECRETS_CHECKLIST.md).
- Approved maintenance window for schema migrations (if required).
- Backups: Recent DB backup taken and verified restore procedure documented.
- On-call contacts and escalation list available.
- Access: `gh` and `az` CLIs installed and authenticated for operator.

Pre-deploy checks
1. Confirm required secrets exist in GitHub Actions or Azure Key Vault.
2. Verify the branch to deploy (usually `main` or `release/*`).
3. Confirm image tag format and that release notes are prepared.
4. Run smoke tests against staging and confirm no regressions.
5. Ensure monitoring & alerting is active and notification channels functional.

Deployment Steps (recommended via GitHub Actions)
1. Dispatch production workflow (one-command script available at `scripts/one-command-deploy.ps1`) or trigger `ci-cd.yml` with `environment: production`.
2. CI pipeline builds images, pushes to registry, runs integration tests and then starts deployment.
3. Pipeline runs EF Core migrations (ensure DB connection secret points to production DB). Migrations are executed in a controlled step and should be logged.
4. Pipeline updates production resources (App Service, AKS, or manifests) with the new image tag.
5. Pipeline runs post-deploy smoke tests (health endpoints, basic auth flow, sample queries).

Smoke Tests (automated)
- GET /health (returns healthy services & dependency checks)
- POST /api/v1/auth/login (sample user) — verify tokens issued
- GET /api/v1/reports/summary (basic analytics query)
- Real-time SignalR connect test (if applicable)

Verification & Monitoring
- Confirm web app(s) show `2xx` responses for health endpoints.
- Monitor logs for 5xx spikes for 10–15 minutes post-deploy.
- Check Application Insights for increased exceptions or latency.
- Confirm background job runner (Hangfire or Jobs container) is running and processing queue.
- Verify database metrics for long-running migrations or high locks.

Rollback Procedure
- If critical failures occur, rollback by redeploying previous known-good image tag.
- Use GitHub Actions to re-run previous successful workflow run or update App Service container to previous tag.
- If schema changes are incompatible with downgrade, follow DB restore plan from backups.

Post-deploy Tasks
- Notify stakeholders of successful deploy with release notes and verification checks.
- Observe metrics and alerts for 30–60 minutes.
- If changes included migrations, monitor DB performance and locks for 24 hours.

Incident Response (high level)
1. Identify severity via alerts and logs.
2. If production is down, escalate to on-call, open incident ticket, and start rollback if required.
3. Collect logs, dump recent error traces, and preserve crash dumps for post-mortem.
4. After recovery, run RCA and schedule follow-up.

Contact & Escalation
- Primary: DevOps lead (email/Slack)
- Secondary: Backend lead
- PagerDuty / on-call rotation: (populate with org-specific info)

Notes & Safety
- Always coordinate DB schema migrations with the team. Prefer additive migrations that are backward compatible when possible.
- For destructive schema changes, follow the two-step deploy: deploy code that tolerates both schemas, run migration, then deploy cleanup release.
