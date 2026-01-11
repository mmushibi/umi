# CI/CD Token Setup

## Required GitHub Personal Access Token (CR_PAT)

To enable Docker image pushing to GitHub Container Registry, create a Personal Access Token with:

### Required Scopes:
- write:packages - For pushing to GHCR
- epo - For repository access

### Setup Steps:
1. Go to https://github.com/settings/tokens/new
2. Token name: UMI Health CI/CD
3. Expiration: 90 days
4. Select scopes: write:packages, repo
5. Copy token
6. Add to repo secrets: Settings → Secrets → Actions → New secret
7. Name: CR_PAT
8. Paste token and save

### Verification:
After adding CR_PAT, the CI/CD pipeline should successfully:
- Build Docker images
- Push to GitHub Container Registry (ghcr.io)
- Complete deployment pipeline
