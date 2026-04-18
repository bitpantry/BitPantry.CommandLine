# GitHub App Identity Setup

This directory contains identity configuration for the two GitHub App identities
used by this project: **implementer** and **reviewer**.

## What Is Here

```
identity/
├── implementer/
│   ├── app.config.json    ← Committed — appId and installationId
│   └── private-key.pem   ← NOT committed — must be placed manually
├── reviewer/
│   ├── app.config.json    ← Committed — appId and installationId
│   └── private-key.pem   ← NOT committed — must be placed manually
├── .gitignore             ← Excludes *.pem from version control
└── README.md              ← This file
```

## First-Time Setup

### 1. Obtain the Private Key Files

Get the `.pem` private key for each GitHub App from the person who manages them,
or download from the GitHub App settings page:

> GitHub → Settings → Developer settings → GitHub Apps → [App] → Private keys

### 2. Place the Key Files

Copy each key and rename it to `private-key.pem` in the correct directory:

```
.github/skills/github-ops/identity/implementer/private-key.pem
.github/skills/github-ops/identity/reviewer/private-key.pem
```

The `.gitignore` in this directory prevents `*.pem` files from being committed.

### 3. Verify app.config.json

Each identity directory has an `app.config.json`:

```json
{
  "appId": "123456",
  "installationId": "78901234"
}
```

- **appId**: The numeric App ID shown at the top of the GitHub App's settings page
- **installationId**: The numeric ID in the installation URL, e.g.:
  `https://github.com/settings/installations/78901234`

### 4. Test the Setup

From the repo root (PowerShell 7+):

```powershell
# Should print a token, not an error
& .github/skills/github-ops/scripts/New-GitHubAppToken.ps1 -Identity implementer
& .github/skills/github-ops/scripts/New-GitHubAppToken.ps1 -Identity reviewer

# Set up session and verify
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
gh auth status
```

## Key File Format

GitHub generates private keys in **PKCS#1** format:

```
-----BEGIN RSA PRIVATE KEY-----
<base64 key material>
-----END RSA PRIVATE KEY-----
```

The token script also accepts **PKCS#8** format:

```
-----BEGIN PRIVATE KEY-----
<base64 key material>
-----END PRIVATE KEY-----
```

## Security Notes

- Never commit private key files. The `.gitignore` prevents this — verify with `git status`.
- Store keys in a password manager or secrets vault and distribute out-of-band.
- Revoke and regenerate keys from the GitHub App settings page if a key is compromised.
