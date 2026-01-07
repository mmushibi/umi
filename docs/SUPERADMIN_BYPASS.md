Super-admin DB access and safe bypass
===================================

This document describes the recommended approach for super-admin/global queries to avoid accidental Row-Level Security (RLS) bypass and to ensure actions are audited.

Recommendations
---------------
- Do NOT rely on application role strings alone to bypass RLS. RLS is enforced by database session settings.
- Provide a dedicated server-side helper that sets DB session context intentionally when super-admin needs to operate across tenants. Example: call `set_config('app.current_tenant_id', '')` only after explicit authorization and auditing.
- Use a separate elevated DB role for maintenance tasks that require bypassing RLS; restrict access to only specific service accounts.
- Always write an audit log entry into `audit_logs` when a super-admin performs cross-tenant operations. Prefer using a stored procedure that writes the audit entry within the same DB transaction.
- Document any endpoints that perform explicit tenant context switching and require extra review.

Scaffolding provided
--------------------
- `set_tenant_context` and `clear_tenant_context` functions exist in `database/row_level_security.sql`. Use them intentionally from server-side code when performing admin cross-tenant reads/writes, and ensure the operation is wrapped with audit logging.

Next steps
----------
- Implement an application-level helper to call a DB stored procedure which sets context and inserts an audit record atomically.
- Limit elevated DB role credentials to trusted operations in production.
