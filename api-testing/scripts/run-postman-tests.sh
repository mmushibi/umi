#!/bin/bash

# Postman API Smoke Test Runner
# This script runs the Postman collection against a target environment
# Usage: ./run-postman-tests.sh <environment> <base_url> <access_token>

set -e

ENVIRONMENT=$1
BASE_URL=$2
ACCESS_TOKEN=$3
COLLECTION_PATH=${4:-"api-testing/postman/collections/UmiHealth_API_Collection.postman_collection.json"}
ENVIRONMENT_FILE=${5:-"api-testing/postman/environments/${ENVIRONMENT}.postman_environment.json"}

if [ -z "$ENVIRONMENT" ] || [ -z "$BASE_URL" ] || [ -z "$ACCESS_TOKEN" ]; then
    echo "Usage: ./run-postman-tests.sh <environment> <base_url> <access_token> [collection_path] [environment_file]"
    echo ""
    echo "Example:"
    echo "  ./run-postman-tests.sh Development https://localhost:7123 'eyJhbGc...'"
    echo ""
    echo "Supported environments: Development, Staging, Production"
    exit 1
fi

# Check if newman is installed
if ! command -v newman &> /dev/null; then
    echo "Newman is not installed. Installing globally..."
    npm install -g newman
fi

echo "🚀 Running Postman tests against: $BASE_URL"
echo "📦 Collection: $COLLECTION_PATH"
echo "🌍 Environment: $ENVIRONMENT_FILE"
echo ""

# Create a temporary environment file with the provided base_url and access_token
TEMP_ENV="/tmp/postman_temp_env.json"
cat > "$TEMP_ENV" <<EOF
{
  "id": "temp-env",
  "name": "Temporary Test Environment",
  "values": [
    {
      "key": "base_url",
      "value": "$BASE_URL",
      "type": "default",
      "enabled": true
    },
    {
      "key": "access_token",
      "value": "$ACCESS_TOKEN",
      "type": "secret",
      "enabled": true
    },
    {
      "key": "api_version",
      "value": "v1",
      "type": "default",
      "enabled": true
    }
  ]
}
EOF

# Run Newman with the collection and environment
newman run "$COLLECTION_PATH" \
  --environment "$TEMP_ENV" \
  --reporters cli,json \
  --reporter-json-export "/tmp/postman-results.json" \
  --timeout-request 30000 \
  --timeout-script 5000 \
  --bail

# Capture exit code
RESULT=$?

# Display summary
echo ""
echo "📊 Test Results Summary:"
if [ -f "/tmp/postman-results.json" ]; then
    # Extract stats from the JSON result
    TOTAL=$(jq '.run.stats.tests.total' /tmp/postman-results.json)
    PASSED=$(jq '.run.stats.tests.passed' /tmp/postman-results.json)
    FAILED=$(jq '.run.stats.tests.failed' /tmp/postman-results.json)
    
    echo "  Total Tests: $TOTAL"
    echo "  Passed: $PASSED"
    echo "  Failed: $FAILED"
fi

# Cleanup
rm -f "$TEMP_ENV"

if [ $RESULT -eq 0 ]; then
    echo ""
    echo "✅ All tests passed!"
    exit 0
else
    echo ""
    echo "❌ Some tests failed. Review the output above."
    exit 1
fi
