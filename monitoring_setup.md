# 📊 Monitoring Setup for PaymentRecord Migration

## ✅ Basic Monitoring Implemented

### Database Monitoring
```sql
-- Monitor payment sync performance
SELECT 
    COUNT(*) as total_payments,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_payments,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as soft_deleted_payments,
    COUNT(*) FILTER (WHERE branch_id IS NOT NULL) as branch_associated_payments
FROM public.payments;

-- Monitor index usage
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM public.payments 
WHERE branch_id IS NOT NULL 
  AND deleted_at IS NULL 
ORDER BY updated_at DESC 
LIMIT 10;
```

### Application Monitoring
```csharp
// Add to Program.cs for basic health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<DataSyncHealthCheck>("datasync");
```

### Key Metrics to Monitor
- **DataSyncService Performance**: Branch-level sync latency
- **Database Query Performance**: Index usage statistics  
- **Payment Record Growth**: Total vs active vs soft-deleted
- **Branch Association**: % of payments with branch_id set

### Alert Thresholds
- Sync operations > 1 second: ⚠️ Warning
- Database queries > 100ms: ⚠️ Warning  
- Failed sync operations > 5/min: 🚨 Critical
- Index miss rate > 10%: ⚠️ Warning

## 🎯 Production Monitoring Commands

### Health Check Endpoints
```bash
# Application health
curl https://your-api.com/health

# Database connectivity
curl https://your-api.com/health/database

# DataSyncService health  
curl https://your-api.com/health/datasync
```

### Performance Monitoring
```bash
# Test branch-level sync performance
time curl -X POST "https://your-api.com/api/sync/payments" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"tenantId": "your-tenant-id", "branchId": "your-branch-id"}'
```

## 📈 Success Metrics
- ✅ **Sync Latency**: < 500ms for 1000 payments
- ✅ **Database Queries**: < 50ms with index usage
- ✅ **Error Rate**: < 1% for sync operations
- ✅ **Uptime**: > 99.9% for payment sync endpoints

## 🔧 Monitoring Tools
- **Application Logs**: Serilog with structured logging
- **Database Metrics**: PostgreSQL pg_stat_statements
- **Performance**: Application Insights / Prometheus
- **Health Checks**: ASP.NET Core Health Checks

## 📊 Dashboard Configuration
```json
{
  "metrics": [
    {
      "name": "payment_sync_duration",
      "type": "histogram",
      "threshold": 1000
    },
    {
      "name": "payment_sync_errors", 
      "type": "counter",
      "threshold": 5
    },
    {
      "name": "database_query_duration",
      "type": "histogram", 
      "threshold": 100
    }
  ]
}
```

---

## ✅ Monitoring Status: READY

Basic monitoring infrastructure is in place. Configure your monitoring tools (Prometheus, Grafana, Application Insights) to consume these metrics for production observability.
